using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;

public class ScaleMeshOld : MonoBehaviour
{
    public GameObject childSphere;   // Sphere driving the deformation
    public GameObject targetObject;  // Optional: object with SyncPosition script
    public enum Axis { X, Y, Z }
    public Axis deformationAxis = Axis.X; // Which local axis of childSphere to use
    public float range=0.3f;

    private SyncPosition targetScript;
    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] modifiedVertices;
    private int[] verticesCollided;
    private int k = 0;
    private bool updateshapeflag = false;
    private Vector3 sphereCenter;
    private Vector3 projectedDeltaWorld;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        verticesCollided = new int[2000];

        if (childSphere != null)
            sphereCenter = childSphere.transform.position;

        if (targetObject != null)
            targetScript = targetObject.GetComponent<SyncPosition>();

       if (transform.gameObject.name == "Bar1")
        {
            ChangeShape();

            childSphere.transform.localPosition = new Vector3(
                childSphere.transform.localPosition.x + 6f,
                childSphere.transform.localPosition.y,
                childSphere.transform.localPosition.z
            );

            Vector3 sphereCenterNew = childSphere.transform.position;
            Vector3 distance = sphereCenterNew - sphereCenter;

            Vector3 axisDir = GetAxisDirection();

            for (int i = 0; i < verticesCollided.Length; i++)
            {
                if (verticesCollided[i] > 0)
                {
                    float moveAmount = Vector3.Dot(distance, axisDir);
                    projectedDeltaWorld = axisDir * moveAmount;

                    Vector3 projectedDeltaLocal = transform.InverseTransformVector(projectedDeltaWorld);
                    modifiedVertices[verticesCollided[i]] =
                        originalVertices[verticesCollided[i]] + projectedDeltaLocal;
                }
            }

            mesh.vertices = modifiedVertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            //targetObject.GetComponent<SyncPosition>().enabled = false;
            StopChanging();
        }
    }

    void Update()
    {
        if (updateshapeflag && childSphere != null)
        {
            Vector3 sphereCenterNew = childSphere.transform.position;
            Vector3 distance = sphereCenterNew - sphereCenter;

            Vector3 axisDir = GetAxisDirection();

            for (int i = 0; i < verticesCollided.Length; i++)
            {
                if (verticesCollided[i] > 0)
                {
                    float moveAmount = Vector3.Dot(distance, axisDir);
                    projectedDeltaWorld = axisDir * moveAmount;

                    Vector3 projectedDeltaLocal = transform.InverseTransformVector(projectedDeltaWorld);
                    modifiedVertices[verticesCollided[i]] =
                        originalVertices[verticesCollided[i]] + projectedDeltaLocal;
                }
            }

            mesh.vertices = modifiedVertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }

    public void SaveMeshAsOBJ(string fileName = "SavedMesh.obj")
    {
        Mesh meshToSave = GetComponent<MeshFilter>().mesh;
        if (meshToSave == null)
        {
            Debug.LogError("No mesh found to export.");
            return;
        }

        // Convert mesh data to OBJ format
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("# Exported from ScaleMesh");
        sb.AppendLine("g " + gameObject.name);

        foreach (Vector3 v in meshToSave.vertices)
        {
            Vector3 wv = transform.TransformPoint(v);
            sb.AppendLine(string.Format("v {0} {1} {2}", wv.x, wv.y, wv.z));
        }

        foreach (Vector3 n in meshToSave.normals)
        {
            Vector3 wn = transform.TransformDirection(n);
            sb.AppendLine(string.Format("vn {0} {1} {2}", wn.x, wn.y, wn.z));
        }

        foreach (Vector2 uv in meshToSave.uv)
            sb.AppendLine(string.Format("vt {0} {1}", uv.x, uv.y));

        int[] triangles = meshToSave.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // OBJ format is 1-based
            int v1 = triangles[i] + 1;
            int v2 = triangles[i + 1] + 1;
            int v3 = triangles[i + 2] + 1;
            sb.AppendLine(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}", v1, v2, v3));
        }

        // Save to file
        string filePath = Path.Combine(Application.dataPath, fileName);
        File.WriteAllText(filePath, sb.ToString());
        Debug.Log("Mesh saved to: " + filePath);
    }


    public void ChangeShape()
    {
        if (targetScript != null)
            targetScript.enabled = true;

        originalVertices = mesh.vertices;
        sphereCenter = childSphere.transform.position;

        float sphereRadius = range;
        Color[] colors = new Color[originalVertices.Length];
        k = 0;

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 vertexWorldPos = transform.TransformPoint(originalVertices[i]);
            float distance = Vector3.Distance(vertexWorldPos, sphereCenter);

            if ((distance < sphereRadius) && (k < verticesCollided.Length))
            {
                verticesCollided[k] = i;
                k++;
            }
        }

        Debug.Log("Vertices selected: " + k);

        modifiedVertices = (Vector3[])originalVertices.Clone();
        mesh.colors = colors;
        updateshapeflag = (k > 0);
    }

    public void StopChanging()
    {
        updateshapeflag = false;
        StartCoroutine(FinalizeSpherePosition());
    }

    private IEnumerator FinalizeSpherePosition()
    {
        yield return new WaitForEndOfFrame();

        if (childSphere != null)
            childSphere.transform.position = sphereCenter + projectedDeltaWorld;

        yield return new WaitForEndOfFrame();

        if (targetScript != null)
            targetScript.enabled = false;

        k = 0;
        for (int i = 0; i < verticesCollided.Length; i++)
            verticesCollided[i] = 0;
    }

    /// <summary>
    /// Returns the chosen local axis of the childSphere in world space.
    /// </summary>
    private Vector3 GetAxisDirection()
    {
        switch (deformationAxis)
        {
            case Axis.Y: return childSphere.transform.up;
            case Axis.Z: return childSphere.transform.forward;
            default: return childSphere.transform.right;
        }
    }
}
