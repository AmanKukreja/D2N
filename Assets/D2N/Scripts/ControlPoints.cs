using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class ControlPoints : MonoBehaviour
{
    public GameObject childSphere;
    public Transform pivot;
    public int numVertices = 5000;
    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] modifiedVertices;

    private List<int> affectedVertices = new List<int>();
    private Dictionary<int, float> vertexWeights = new Dictionary<int, float>();

    private Dictionary<int, List<int>> weldedVertices = new Dictionary<int, List<int>>();

    private bool updateshapeflag = false;
    private Vector3 sphereCenter;
    private Vector3 projectedDeltaWorld;

    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();

        // Create a unique runtime copy of the mesh
        mesh = Instantiate(mf.sharedMesh);
        mf.mesh = mesh;   // assign the instance

        sphereCenter = childSphere.transform.position;

        // Precompute welded groups
        BuildWeldedVertices();

        changeshape();

    }


    void Update()
    {
        if (updateshapeflag)
        {
            Vector3 sphereCenterNew = childSphere.transform.position;
            Vector3 distance = sphereCenterNew - sphereCenter;

            float moveAmount = Vector3.Dot(distance, childSphere.transform.up);
            projectedDeltaWorld = childSphere.transform.up * moveAmount;

            Vector3 projectedDeltaLocal = transform.InverseTransformVector(projectedDeltaWorld);

            foreach (int vIndex in affectedVertices)
            {
                float weight = vertexWeights[vIndex];
                Vector3 newPos = originalVertices[vIndex] + projectedDeltaLocal * weight;

                // Move all welded duplicates together
                foreach (int dup in weldedVertices[vIndex])
                {
                    modifiedVertices[dup] = newPos;
                }
            }

            mesh.vertices = modifiedVertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }

    public void changeshape()
    {
        originalVertices = mesh.vertices;
        sphereCenter = childSphere.transform.position;

        // Calculate distances for all vertices
        var distances = new List<(int index, float distance)>();
        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 vertexWorldPos = transform.TransformPoint(originalVertices[i]);
            float dist = Vector3.Distance(vertexWorldPos, sphereCenter);
            distances.Add((i, dist));
        }

        // Sort by distance
        var sorted = distances.OrderBy(d => d.distance).ToList();

        affectedVertices.Clear();
        vertexWeights.Clear();

        // Assign weights (2 with 1.0, next 4 with 0.9, next 4 with 0.8, etc.)
        int count = 0;
        float weight = 0.2f;
        int batchSize = 1000;

        while (weight > 0 && count < sorted.Count && count < numVertices)
        {
            for (int j = 0; j < batchSize && count < sorted.Count && count < numVertices; j++)
            {
                int vIndex = sorted[count].index;
                affectedVertices.Add(vIndex);
                vertexWeights[vIndex] = weight;
                count++;
            }

            weight -= 0.05f;
            batchSize = 2000; // after first 2, use groups of 4
        }

        //Debug.LogError("" + affectedVertices.Count + "" + vertexWeights.Count);

        modifiedVertices = (Vector3[])originalVertices.Clone();
        updateshapeflag = true;
    }

    public void StopChanging()
    {
        StartCoroutine(FinalizeSpherePosition());
    }

    private IEnumerator FinalizeSpherePosition()
    {
        yield return new WaitForEndOfFrame();

        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
            yield break;

        // Copy mesh data so collider update can happen off main thread
        Mesh meshCopy = Instantiate(mesh);

        // Run collider preparation asynchronously
        yield return StartCoroutine(UpdateColliderAsync(meshCollider, meshCopy));

        Debug.Log("MeshCollider updated after deformation (async).");
    }

    private IEnumerator UpdateColliderAsync(MeshCollider collider, Mesh meshCopy)
    {
        // Run the heavy work in another thread
        bool finished = false;

        System.Threading.Tasks.Task.Run(() =>
        {
            // Prepare collision mesh (if needed, could recalc bounds, etc.)
            meshCopy.RecalculateBounds();
            finished = true;
        });

        // Wait for background thread to finish
        while (!finished)
            yield return null;

        // Now safely update collider on main thread
        collider.sharedMesh = null;
        yield return null; // allow Unity to process
        collider.sharedMesh = meshCopy;
    }

    /// <summary>
    /// Builds groups of welded vertices (same position)
    /// </summary>
    private void BuildWeldedVertices()
    {
        originalVertices = mesh.vertices;
        weldedVertices.Clear();

        var groups = new Dictionary<Vector3, List<int>>();

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 pos = originalVertices[i];

            // Use quantized position to avoid float precision issues
            Vector3 key = new Vector3(
                Mathf.Round(pos.x * 10000f) / 10000f,
                Mathf.Round(pos.y * 10000f) / 10000f,
                Mathf.Round(pos.z * 10000f) / 10000f
            );

            if (!groups.ContainsKey(key))
                groups[key] = new List<int>();

            groups[key].Add(i);
        }

        // Map each vertex index to its group
        foreach (var kvp in groups)
        {
            foreach (int idx in kvp.Value)
                weldedVertices[idx] = kvp.Value;
        }
    }
}
