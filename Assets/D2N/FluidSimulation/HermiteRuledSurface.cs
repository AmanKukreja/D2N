using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DynamicHermiteSurface : MonoBehaviour
{
    [Header("Control Objects")]
    public Transform pointA;
    public Transform pointB;

    public OVRInput.Button button = OVRInput.Button.PrimaryIndexTrigger;
    private bool generateSurface=true;
    public OVRInput.Controller controller = OVRInput.Controller.RTouch;
    private float frequency = 0.5f;

    private float amplitude = 1.0f;

    private float duration = 0.2f;



    [Header("Settings")]
    public float vSpacing = 0.1f;
    public float tangentStrength = 2.0f;
    
    [Range(2, 40)]
    public int uResolution = 15; // Smoothness across the curve (A to B)
    
    [Range(1, 10)]
    public int vSubdivisions = 2; // Smoothness between recorded steps

    private Mesh mesh;
    private List<SliceData> recordedSlices = new List<SliceData>();
    
    private Vector3 lastRecordedPosA;
    private Vector3 lastRecordedPosB;
    

    struct SliceData
    {
        public Vector3 p0, p1; // Positions
        public Vector3 t0, t1; // X-Axis Tangents
    }

    void Start()
    {
        mesh = new Mesh();
        mesh.name = "DynamicHermiteSurface";
        GetComponent<MeshFilter>().mesh = mesh;
        // RecordSlice();
        // Debug.LogError(lastRecordedPosA);
    }

    void Update()
    {
        generateSurface = OVRInput.Get(button);

        if (generateSurface)
        {
            StartCoroutine(Vibrate());
            // 1. Check if either point has moved 0.1 units in any direction
            float distA = Vector3.Distance(pointA.position, lastRecordedPosA);
            float distB = Vector3.Distance(pointB.position, lastRecordedPosB);
           

            // 2. If moved far enough, lock a new slice into history
            if (distA >= vSpacing || distB >= vSpacing)
            {
                RecordSlice();
            }

            // 3. Always generate mesh so it updates on ROTATION even when not moving
            GenerateMesh();
        }

        if (OVRInput.GetUp(button))
        {
            StoreMesh();
            ResetSurface();
        }
    }

    void StoreMesh()
    {
        if (mesh.vertexCount == 0) return;

        GameObject surface = new GameObject("GeneratedSurface");

        MeshFilter mf = surface.AddComponent<MeshFilter>();
        MeshRenderer mr = surface.AddComponent<MeshRenderer>();

        Mesh storedMesh = new Mesh();
        storedMesh.vertices = mesh.vertices;
        storedMesh.triangles = mesh.triangles;
        storedMesh.uv = mesh.uv;
        storedMesh.normals = mesh.normals;

        mf.mesh = storedMesh;

        mr.material = GetComponent<MeshRenderer>().material;

        surface.transform.position = transform.position;
        surface.transform.rotation = transform.rotation;
    }

    void ResetSurface()
    {
        mesh.Clear();

        recordedSlices.Clear();

        lastRecordedPosA = pointA.position;
        lastRecordedPosB = pointB.position;
    }
    System.Collections.IEnumerator Vibrate()
    {
        OVRInput.SetControllerVibration(frequency, amplitude, controller);
        yield return new WaitForSeconds(duration);
        OVRInput.SetControllerVibration(0, 0, controller);
    }

    void RecordSlice()
    {
        recordedSlices.Add(GetCurrentSlice());
        lastRecordedPosA = pointA.position;
        lastRecordedPosB = pointB.position;
    }

    SliceData GetCurrentSlice()
    {
        return new SliceData
        {
            p0 = pointA.position,
            p1 = pointB.position,
            t0 = pointA.up * tangentStrength,
            t1 = pointB.up * tangentStrength *-1
        };
    }

    void GenerateMesh()
    {
        if (recordedSlices.Count < 1) return;

        // We create a temporary list of slices that includes the current "Live" position
        // This makes the surface respond to rotation/movement before the 0.1m limit is hit
        List<SliceData> displaySlices = new List<SliceData>(recordedSlices);
        displaySlices.Add(GetCurrentSlice());

        int vSegments = (displaySlices.Count - 1) * vSubdivisions;
        int uVertCount = uResolution + 1;
        int vVertCount = vSegments + 1;

        Vector3[] vertices = new Vector3[uVertCount * vVertCount];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[vSegments * uResolution * 6];

        // Generate Vertices
        for (int vIdx = 0; vIdx < displaySlices.Count - 1; vIdx++)
        {
            SliceData startSlice = displaySlices[vIdx];
            SliceData endSlice = displaySlices[vIdx + 1];

            for (int subV = 0; subV < (vIdx == displaySlices.Count - 2 ? vSubdivisions + 1 : vSubdivisions); subV++)
            {
                float tV = (float)subV / vSubdivisions;
                int globalV = vIdx * vSubdivisions + subV;

                for (int u = 0; u <= uResolution; u++)
                {
                    float tU = (float)u / uResolution;

                    // Bi-linear interpolation of the Hermite points 
                    // (Interpolating between two Hermite curves creates the surface)
                    Vector3 posStartU = CalculateHermite(startSlice.p0, startSlice.t0, startSlice.p1, startSlice.t1, tU);
                    Vector3 posEndU = CalculateHermite(endSlice.p0, endSlice.t0, endSlice.p1, endSlice.t1, tU);
                    
                    // In V direction, we use linear interpolation between the two Hermite curves
                    // For a full "Bi-Cubic", you'd need V-tangents, but for a live trail, 
                    // high subdivision + linear V is standard and more stable.
                    Vector3 finalPos = Vector3.Lerp(posStartU, posEndU, tV);

                    int vertIdx = globalV * uVertCount + u;
                    vertices[vertIdx] = transform.InverseTransformPoint(finalPos);
                    uvs[vertIdx] = new Vector2(tU, (float)globalV / vSegments);
                }
            }
        }

        // Generate Triangles
        int triIdx = 0;
        for (int v = 0; v < vSegments; v++)
        {
            for (int u = 0; u < uResolution; u++)
            {
                int row1 = v * uVertCount;
                int row2 = (v + 1) * uVertCount;

                triangles[triIdx++] = row1 + u;
                triangles[triIdx++] = row2 + u;
                triangles[triIdx++] = row1 + u + 1;

                triangles[triIdx++] = row1 + u + 1;
                triangles[triIdx++] = row2 + u;
                triangles[triIdx++] = row2 + u + 1;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
    }

    Vector3 CalculateHermite(Vector3 p0, Vector3 t0, Vector3 p1, Vector3 t1, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        float h1 = 2 * t3 - 3 * t2 + 1;
        float h2 = -2 * t3 + 3 * t2;
        float h3 = t3 - 2 * t2 + t;
        float h4 = t3 - t2;
        return h1 * p0 + h2 * p1 + h3 * t0 + h4 * t1;
    }
}