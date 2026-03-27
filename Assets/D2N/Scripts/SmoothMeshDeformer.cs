using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    private Vector3 normal;
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

        // Delete all control points and generate new at new plane location
        foreach (GameObject cp in controlPoints)
        {
            Destroy(cp);
        }
        
        if (intersectionProvider == null)
            return;

        Transform meshT = targetMeshFilter.transform;

        //Get exact intersection curve (WORLD SPACE)
        //Debug.LogError("Entered smooth mesh");
        List<Vector3> curve = intersectionProvider.GetIntersectionCurve();
        // Debug.LogError("Curve points" + curve.Count);

        // Find centroid in y direction so as to find out points on to the top curve and bottom curve and then populate control points
        float centroid=0;

        for (int i =0; i < curve.Count; i++)
        {
            //GameObject handle = Instantiate(handlePrefab, curve[i], Quaternion.identity);
            centroid=centroid+curve[i].y;
        }
        centroid=centroid/curve.Count;

        // Find span in x direction and divide it equally to populate control points throughout the span
        float minX = curve.Min(v => v.x);
        float maxX = curve.Max(v => v.x);

        float step = (maxX - minX) / 4f;

        List<int> idxs = new List<int>(10);

        // for each target X, find closest ABOVE and BELOW centroidY
        for (int i = 0; i < handleCount; i++)
        {
            float targetX = minX + step * i;

            int closestAbove = -1;
            int closestBelow = -1;

            float bestAboveXDist = float.MaxValue;
            float bestBelowXDist = float.MaxValue;

            for (int j = 0; j < curve.Count; j++)
            {
                float xDist = Mathf.Abs(curve[j].x - targetX);
                float y = curve[j].y;

                if (y > centroid && xDist < bestAboveXDist)
                {
                    bestAboveXDist = xDist;
                    closestAbove = j;
                }
                else if (y < centroid && xDist < bestBelowXDist)
                {
                    bestBelowXDist = xDist;
                    closestBelow = j;
                }
            }
            idxs.Add(closestAbove);
            idxs.Add(closestBelow);
        }

        if (curve == null || curve.Count < 2 || curve.Count < handleCount)
            return;

        // Collect affected vertices (distance to curve instead of plane) to change shape of surface by moving control points
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

        // Spawn evenly spaced handles ON THE CURVE
        handleStartPositions = new Vector3[handleCount];
        handles.Clear();

        for (int h = 0; h < handleCount-1; h++)
        {
            Vector3 pos=curve[idxs[h]];
            Vector3 spawnPos=pos;

            if (pos.y < centroid)
            {
                spawnPos=new Vector3 (pos.x, pos.y - normalOffset, pos.z);
            }
            else
            {
                spawnPos=new Vector3 (pos.x, pos.y + normalOffset, pos.z);                
            }
            
            GameObject handle = Instantiate(handlePrefab, spawnPos, Quaternion.identity);
            handles.Add(handle.transform);
            handleStartPositions[h] = spawnPos;
            // Debug.LogError("" + h);
        }

        // Precompute vertex weights (unchanged logic)
        vertexWeights.Clear();

        foreach (int vIdx in affectedVertexIndices)
        {
            float[] weights = new float[handleCount];
            Vector3 vWorldPos = meshT.TransformPoint(originalVertices[vIdx]);

            for (int h = 0; h < handleCount; h++)
            {
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