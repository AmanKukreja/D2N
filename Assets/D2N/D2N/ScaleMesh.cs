using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ScaleMesh : MonoBehaviour
{
    public GameObject childSphere;
    public GameObject targetObject;
    public enum Axis { X, Y, Z }
    public Axis deformationAxis = Axis.X;
    public float range = 0.3f;

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] modifiedVertices;
    private int[] verticesCollided;
    private int k = 0;
    private bool updateShapeFlag = false;
    private Vector3 sphereCenter;
    private Vector3 projectedDeltaWorld;

    // --- direction smoothing buffer ---
    private Vector3[] motionBuffer;
    private int motionIndex = 0;
    private int motionFilled = 0;
    public int smoothFrameCount = 100; // number of frames to average direction

    private Dictionary<int, List<int>> weldedVertices = new Dictionary<int, List<int>>();
    private SyncPosition targetScript;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        mesh.MarkDynamic(); // hint to Unity that we'll modify this mesh often
        verticesCollided = new int[10000];

        if (childSphere != null)
            sphereCenter = childSphere.transform.position;

        motionBuffer = new Vector3[smoothFrameCount];
        
        ChangeShape();

        BuildWeldedVertices(); // 🔗 precompute welded groups
    }

    void Update()
    {
        if (!updateShapeFlag || childSphere == null)
            return;

        Vector3 sphereCenterNew = childSphere.transform.position;
        Vector3 distance = sphereCenterNew - sphereCenter;
        //Vector3 axisDir = GetAxisDirection();

        // store delta in circular buffer
        motionBuffer[motionIndex] = distance;
        motionIndex = (motionIndex + 1) % smoothFrameCount;
        if (motionFilled < smoothFrameCount) motionFilled++;

        // compute average direction of recent motion
        Vector3 avgDir = Vector3.zero;
        for (int i = 0; i < motionFilled; i++)
            avgDir += motionBuffer[i];
        if (motionFilled > 0)
            avgDir /= motionFilled;

        if (avgDir.sqrMagnitude < 1e-6f)
            return; // sphere not moving

        Vector3 axisDir = avgDir.normalized;

        float moveAmount = Vector3.Dot(distance, axisDir);
        projectedDeltaWorld = axisDir * moveAmount;
        Vector3 projectedDeltaLocal = transform.InverseTransformVector(projectedDeltaWorld);

        for (int i = 0; i < k; i++)
        {
            int vIndex = verticesCollided[i];
            Vector3 newPos = originalVertices[vIndex] + projectedDeltaLocal;

            // Move welded vertices together
            if (weldedVertices.TryGetValue(vIndex, out var group))
            {
                foreach (int dup in group)
                    modifiedVertices[dup] = newPos;
            }
            else
            {
                modifiedVertices[vIndex] = newPos;
            }
        }

        mesh.vertices = modifiedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    public void ChangeShape()
    {
        if (targetScript != null)
            targetScript.enabled = true;

        originalVertices = mesh.vertices;
        sphereCenter = childSphere.transform.position;

        float sphereRadius = range;
        k = 0;
        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 vertexWorldPos = transform.TransformPoint(originalVertices[i]);
            float distance = Vector3.Distance(vertexWorldPos, sphereCenter);
            if (distance < sphereRadius && k < verticesCollided.Length)
                verticesCollided[k++] = i;
        }

        Debug.Log($"Vertices selected: {k}");
        modifiedVertices = (Vector3[])originalVertices.Clone();
        updateShapeFlag = (k > 0);
    }

    public void StopChanging()
    {
        updateShapeFlag = false;
        StartCoroutine(FinalizeSpherePosition());
    }

    private IEnumerator FinalizeSpherePosition()
    {
        // Wait until mesh updates finish for this frame
        yield return new WaitForEndOfFrame();

        // Apply final child sphere position
        if (childSphere != null)
            childSphere.transform.position = sphereCenter + projectedDeltaWorld;

        if (targetScript != null)
            targetScript.enabled = false;

        // ✅ Update mesh collider safely
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;   // clear first to force refresh
            yield return null;                // wait one more frame for safety
            meshCollider.sharedMesh = mesh;   // reassign updated mesh
            Debug.Log("MeshCollider updated after deformation.");
        }

        // Clean up
        k = 0;
        for (int i = 0; i < verticesCollided.Length; i++)
            verticesCollided[i] = 0;
    }


    private Vector3 GetAxisDirection()
    {
        switch (deformationAxis)
        {
            case Axis.Y: return childSphere.transform.up;
            case Axis.Z: return childSphere.transform.forward;
            default: return childSphere.transform.right;
        }
    }

    /// <summary>
    /// Groups together vertices that occupy the same position (welded)
    /// </summary>
    private void BuildWeldedVertices()
    {
        Vector3[] verts = mesh.vertices;
        weldedVertices.Clear();

        // Use quantized positions to merge close vertices (avoid float errors)
        Dictionary<Vector3, List<int>> groups = new Dictionary<Vector3, List<int>>();
        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 key = new Vector3(
                Mathf.Round(verts[i].x * 10000f) / 10000f,
                Mathf.Round(verts[i].y * 10000f) / 10000f,
                Mathf.Round(verts[i].z * 10000f) / 10000f
            );

            if (!groups.TryGetValue(key, out var list))
            {
                list = new List<int>();
                groups[key] = list;
            }
            list.Add(i);
        }

        foreach (var kvp in groups)
        {
            List<int> list = kvp.Value;
            foreach (int idx in list)
                weldedVertices[idx] = list;
        }

        Debug.Log($"Welded vertex groups built: {groups.Count}");
    }
}
