using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class BezierSurface : MonoBehaviour
{
    [Header("Surface Resolution")]
    [Range(2, 150)] public int resolutionU = 40;
    [Range(2, 50)]  public int resolutionV = 20;

    [Header("V Direction")]
    public int controlPointsV = 4;
    public float vSpacing = 0.1f;

    [Header("Control Point Settings")]
    public GameObject controlPointPrefab;
    public Material dottedLineMaterial;

    [Header("Inputs")]
    public Transform rightController;
    public bool generateSurface = true;

    private List<Transform[]> controlPointsU = new List<Transform[]>();
    private Transform[] dummyColumn;
    private Mesh mesh;
    
    // Lines joining points in U and V directions
    private List<LineRenderer> uLines = new List<LineRenderer>();
    private List<LineRenderer> vLines = new List<LineRenderer>();

    void Awake()
    {
        mesh = new Mesh();
        mesh.name = "Dynamic Bezier Surface";
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void Update()
    {
        if (generateSurface)
        {
            UpdateDummyColumn();
            UpdateGridLines();
        }

        // Right Trigger: If finalized, start new. If active, commit column.
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            if (!generateSurface) 
            {
                ResetSurface(); // Start fresh
            }
            else 
            {
                CommitDummyColumn();
            }
        }

        // Left Trigger: Finalize the mesh (stop previewing)
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
        {
            FinalizeSurface();
        }

        if (controlPointsU.Count >= 1 || (generateSurface && dummyColumn != null))
        {
            UpdateSurface();
        }
    }

    void ResetSurface()
    {
        // Destroy old control points
        foreach (var col in controlPointsU)
            foreach (var cp in col) if(cp) Destroy(cp.gameObject);
        
        controlPointsU.Clear();
        mesh.Clear();
        generateSurface = true;
    }

    void FinalizeSurface()
    {
        generateSurface = false;
        // Hide preview lines when finished
        // if (dummyColumn != null)
        // {
        //     foreach (var t in dummyColumn) t.gameObject.SetActive(false);
        // }
        // foreach (var lr in uLines) lr.positionCount = 0;
        // foreach (var lr in vLines) lr.positionCount = 0;
    }

    void UpdateDummyColumn()
    {
        Vector3 basePosition = rightController.position;
        // Calculate direction based on controller's forward (Z-axis)
        Vector3 forwardDir = rightController.forward;

        if (dummyColumn == null)
        {
            dummyColumn = new Transform[controlPointsV];
            for (int v = 0; v < controlPointsV; v++)
            {
                GameObject cp = controlPointPrefab ? Instantiate(controlPointPrefab) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
                if (!controlPointPrefab) cp.transform.localScale = Vector3.one * 0.05f;
                cp.name = $"CP_Dummy_V{v}";
                cp.transform.SetParent(transform, true);
                dummyColumn[v] = cp.transform;
            }
        }

        for (int v = 0; v < controlPointsV; v++)
        {
            dummyColumn[v].position = basePosition + (forwardDir * v * vSpacing);
        }
    }

    void CommitDummyColumn()
    {
        Transform[] newColumn = new Transform[controlPointsV];
        for (int v = 0; v < controlPointsV; v++)
        {
            GameObject cp = Instantiate(controlPointPrefab ? controlPointPrefab : GameObject.CreatePrimitive(PrimitiveType.Sphere));
            cp.transform.position = dummyColumn[v].position;
            cp.transform.SetParent(transform, true);
            newColumn[v] = cp.transform;
        }
        controlPointsU.Add(newColumn);
    }

    // Handles drawing lines for both U and V directions
    void UpdateGridLines()
    {
        // 1. U-Direction Lines (connecting points across columns)
        for (int v = 0; v < controlPointsV; v++)
        {
            if (v >= uLines.Count) uLines.Add(CreateLine($"ULine_{v}"));
            
            LineRenderer lr = uLines[v];
            int count = controlPointsU.Count + (generateSurface ? 1 : 0);
            lr.positionCount = count;

            for (int i = 0; i < controlPointsU.Count; i++)
                lr.SetPosition(i, controlPointsU[i][v].position);
            
            if (generateSurface)
                lr.SetPosition(count - 1, dummyColumn[v].position);
        }

        // 2. V-Direction Lines (connecting points within a single column)
        int totalColsNeeded = controlPointsU.Count + (generateSurface ? 1 : 0);
        while (vLines.Count < totalColsNeeded) vLines.Add(CreateLine($"VLine_{vLines.Count}"));

        for (int i = 0; i < totalColsNeeded; i++)
        {
            LineRenderer lr = vLines[i];
            lr.positionCount = controlPointsV;
            Transform[] col = (i < controlPointsU.Count) ? controlPointsU[i] : dummyColumn;
            
            for (int v = 0; v < controlPointsV; v++)
                lr.SetPosition(v, col[v].position);
        }
    }

    LineRenderer CreateLine(string name)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(transform, false);
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = dottedLineMaterial;
        lr.widthMultiplier = 0.005f;
        lr.useWorldSpace = true;
        return lr;
    }

    void UpdateSurface()
    {
        int cols = resolutionU + 1; // Vertices along U
        int rows = resolutionV + 1; // Vertices along V

        int baseVertexCount = cols * rows;

        // Add 2 extra vertices for the center points of the V=0 and V=max caps
        Vector3[] vertices = new Vector3[baseVertexCount + 2];

        // Triangles: Main surface + (Triangles per cap * 2 caps)
        int surfaceTriCount = resolutionU * resolutionV * 6;
        int capTriCount = resolutionU * 3 * 2; 
        int[] triangles = new int[surfaceTriCount + capTriCount];

        int vtx = 0;

        // 1. GENERATE SURFACE VERTICES
        for (int y = 0; y <= resolutionV; y++) // V loop
        {
            for (int x = 0; x <= resolutionU; x++) // U loop
            {
                float u = x / (float)resolutionU;
                float v = y / (float)resolutionV;

                Vector3 p = EvaluateSurfaceWithPreview(u, v);
                vertices[vtx++] = transform.InverseTransformPoint(p);
            }
        }

        // 2. CAP CENTER VERTICES
        int capStartIdx = baseVertexCount;      // Center for V = 0
        int capEndIdx = baseVertexCount + 1;    // Center for V = resolutionV

        Vector3 startCenterSum = Vector3.zero;
        Vector3 endCenterSum = Vector3.zero;

        // Calculate averages for center points
        for (int x = 0; x <= resolutionU; x++)
        {
            startCenterSum += vertices[x]; // First row
            endCenterSum += vertices[(resolutionV * cols) + x]; // Last row
        }

        vertices[capStartIdx] = startCenterSum / (resolutionU + 1);
        vertices[capEndIdx] = endCenterSum / (resolutionU + 1);

        // 3. MAIN SURFACE TRIANGLES (Corrected Winding for Normals)
        int t = 0;
        for (int y = 0; y < resolutionV; y++)
        {
            for (int x = 0; x < resolutionU; x++)
            {
                int i = y * cols + x;

                // Face 1
                triangles[t++] = i;
                triangles[t++] = i + cols;
                triangles[t++] = i + 1;

                // Face 2
                triangles[t++] = i + 1;
                triangles[t++] = i + cols;
                triangles[t++] = i + cols + 1;
            }
        }

        // 4. START CAP (V = 0 edge)
        for (int x = 0; x < resolutionU; x++)
        {
            triangles[t++] = capStartIdx;
            triangles[t++] = x;
            triangles[t++] = x + 1;
        }

        // 5. END CAP (V = resolutionV edge)
        int lastRowStart = resolutionV * cols;
        for (int x = 0; x < resolutionU; x++)
        {
            triangles[t++] = capEndIdx;
            triangles[t++] = lastRowStart + x + 1;
            triangles[t++] = lastRowStart + x;
        }

        // 6. APPLY TO MESH
        mesh.Clear();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    Vector3 EvaluateSurfaceWithPreview(float u, float v)
    {
        List<Transform[]> allColumns = new List<Transform[]>(controlPointsU);
        if (generateSurface && dummyColumn != null) allColumns.Add(dummyColumn);

        if (allColumns.Count == 0) return Vector3.zero;

        Vector3 point = Vector3.zero;
        int n = allColumns.Count - 1;
        int m = controlPointsV - 1;

        for (int i = 0; i <= n; i++)
        {
            float bu = Bernstein(n, i, u);
            for (int j = 0; j <= m; j++)
            {
                float bv = Bernstein(m, j, v);
                point += allColumns[i][j].position * bu * bv;
            }
        }
        return point;
    }

    float Bernstein(int n, int i, float t) => Binomial(n, i) * Mathf.Pow(t, i) * Mathf.Pow(1f - t, n - i);

    float Binomial(int n, int k)
    {
        float result = 1f;
        for (int i = 1; i <= k; i++) { result *= (n - (k - i)); result /= i; }
        return result;
    }
}