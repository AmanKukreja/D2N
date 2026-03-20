using UnityEngine;
using System.Collections.Generic;

public class SmoothMeshDeformer : MonoBehaviour
{
    [Header("Setup")]
    public MeshFilter targetMeshFilter;
    public GameObject handlePrefab;
    
    [Header("Settings")]
    public float influenceRadius = 0.5f;
    public int handleCount = 5;

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] modifiedVertices;
    private Dictionary<int, List<int>> weldedVertices = new Dictionary<int, List<int>>();

    // Handle Data
    private List<Transform> handles = new List<Transform>();
    private Vector3[] handleStartPositions;
    private List<int> affectedVertexIndices = new List<int>();
    private Dictionary<int, float[]> vertexWeights = new Dictionary<int, float[]>();

    public PlaneMeshIntersectionCurve intersectionProvider;

    [Header("Directional Falloff")]
    public float verticalInfluence = 0.15f;   // max Y distance a vertex can be influenced

    public float normalOffset = 0.2f;
    public Vector3 referenceUp = Vector3.up; // choose what makes sense

    void Start()
    {
        if (targetMeshFilter == null) return;
        
        // Initialize Mesh
        mesh = targetMeshFilter.mesh;
        mesh.MarkDynamic();
        originalVertices = mesh.vertices;
        modifiedVertices = (Vector3[])originalVertices.Clone();

        BuildWeldedVertices();
        SetupIntersectionHandles();
    }

    public void SetupIntersectionHandles()
    {
        GameObject[] controlPoints = GameObject.FindGameObjectsWithTag("ControlPoint");

        foreach (GameObject cp in controlPoints)
        {
            Destroy(cp);
        }
        
        if (intersectionProvider == null)
            return;

        Transform meshT = targetMeshFilter.transform;

        // 1. Get exact intersection curve (WORLD SPACE)
        List<Vector3> curve = intersectionProvider.GetIntersectionCurve();
        if (curve == null || curve.Count < 2 || curve.Count < handleCount)
            return;

        // 2. Collect affected vertices (distance to curve instead of plane)
        affectedVertexIndices.Clear();

        float vertexCurveThreshold = influenceRadius * 0.75f;

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 vWorld = meshT.TransformPoint(originalVertices[i]);

            float minDist = float.MaxValue;
            for (int c = 0; c < curve.Count; c++)
                minDist = Mathf.Min(minDist, Vector3.Distance(vWorld, curve[c]));

            if (minDist < vertexCurveThreshold)
                affectedVertexIndices.Add(i);
        }

        // 3. Build cumulative arc-length table
        List<float> cumulative = new List<float>();
        //cumulative.Add(0f);

        float totalLength = 0f;
        for (int i = 1; i < curve.Count; i++)
        {
            totalLength += Vector3.Distance(curve[i - 1], curve[i]);
            cumulative.Add(totalLength);
        }

        // 4. Spawn evenly spaced handles ON THE CURVE
        handleStartPositions = new Vector3[handleCount];
        handles.Clear();

        for (int h = 0; h < handleCount-1; h++)
        {
            float targetDist = (h / (float)(handleCount - 1)) * totalLength;

            int idx = cumulative.FindIndex(d => d >= targetDist);
            if (idx <= 0) idx = 1;

            float t = Mathf.InverseLerp(
                cumulative[idx - 1],
                cumulative[idx],
                targetDist
            );

            Vector3 a = curve[idx - 1];
            Vector3 b = curve[idx];

            Vector3 pos = Vector3.Lerp(a, b, t);

            // SUPER CHEAP normal (no cross product)
            Vector3 dir = b - a;
            Vector3 normal = new Vector3(0f, dir.x, -dir.y);   // perpendicular in XZ

            Vector3 spawnPos = pos + normal.normalized * normalOffset;

            GameObject handle = Instantiate(handlePrefab, spawnPos, Quaternion.identity);
            handles.Add(handle.transform);
            handleStartPositions[h] = spawnPos;


            // GameObject handle = Instantiate(handlePrefab, pos, Quaternion.identity);
            // handles.Add(handle.transform);
            // handleStartPositions[h] = pos;
        }

        // 5. Precompute vertex weights (unchanged logic)
        vertexWeights.Clear();

        foreach (int vIdx in affectedVertexIndices)
        {
            float[] weights = new float[handleCount];
            Vector3 vWorldPos = meshT.TransformPoint(originalVertices[vIdx]);

            for (int h = 0; h < handleCount; h++)
            {
                // float dist = Vector3.Distance(vWorldPos, handleStartPositions[h]);
                // float t = Mathf.Clamp01(dist / influenceRadius);

                // if (Mathf.Sign(vWorldPos.y - handleStartPositions[h].y) != Mathf.Sign(handleStartPositions[h].y))
                // {
                //     weights[h] = 0f;
                //     continue;
                // }

                // weights[h] = 1.0f - (t * t * (3 - 2 * t)); // smoothstep
                Vector3 handlePos = handleStartPositions[h];

                // Horizontal / spatial distance (XZ + along curve)
                float spatialDist = Vector3.Distance(vWorldPos, handlePos);
                float spatialT = Mathf.Clamp01(spatialDist / influenceRadius);
                float spatialWeight = 1.0f - (spatialT * spatialT * (3 - 2 * spatialT));

                // Vertical distance check
                float yDist = Mathf.Abs(vWorldPos.y - handlePos.y);
                float yT = Mathf.Clamp01(yDist / verticalInfluence);
                float verticalWeight = 1.0f - (yT * yT * (3 - 2 * yT));

                // Final combined weight
                weights[h] = spatialWeight * verticalWeight;

            }

            vertexWeights[vIdx] = weights;
        }
    }



    void Update()
    {
        if (handles.Count == 0) return;

        // Reset to original before applying new displacements
        System.Array.Copy(originalVertices, modifiedVertices, originalVertices.Length);
        Matrix4x4 worldToLocal = targetMeshFilter.transform.worldToLocalMatrix;

        foreach (var entry in vertexWeights)
        {
            int vIdx = entry.Key;
            float[] weights = entry.Value;
            Vector3 totalWorldOffset = Vector3.zero;

            for (int i = 0; i < handleCount-1; i++)
            {
                // Calculate how far the handle has moved from its start
                Vector3 delta = handles[i].position - handleStartPositions[i];
                
                // Multiply movement by the smooth weight
                totalWorldOffset += delta * weights[i];
            }

            // Convert movement to local space and apply to the vertex + its welded twins
            Vector3 localOffset = worldToLocal.MultiplyVector(totalWorldOffset);
            
            if (weldedVertices.TryGetValue(vIdx, out var group))
            {
                foreach (int childIdx in group)
                    modifiedVertices[childIdx] = originalVertices[childIdx] + localOffset;
            }
            else
            {
                modifiedVertices[vIdx] = originalVertices[vIdx] + localOffset;
            }
        }

        mesh.vertices = modifiedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void BuildWeldedVertices()
    {
        weldedVertices.Clear();
        Dictionary<Vector3, List<int>> groups = new Dictionary<Vector3, List<int>>();
        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 v = originalVertices[i];
            // Keying by rounded coordinates to catch "same" points
            Vector3 key = new Vector3(Mathf.Round(v.x*1000f)/1000f, Mathf.Round(v.y*1000f)/1000f, Mathf.Round(v.z*1000f)/1000f);
            if (!groups.ContainsKey(key)) groups[key] = new List<int>();
            groups[key].Add(i);
        }
        foreach (var g in groups.Values)
            foreach (int idx in g) weldedVertices[idx] = g;
    }
}