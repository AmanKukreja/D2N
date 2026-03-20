using UnityEngine;
using UnityEngine.UIElements;

public class BezierSurfaceFlat : MonoBehaviour
{
    [Header("Surface Resolution")]
    [Range(2, 200)]
    public int resolution = 30;

    [Header("Control Point Grid Size")]
    [Range(2, 10)]
    public int controlPointsU = 4; // chordwise
    [Range(2, 10)]
    public int controlPointsV = 4; // spanwise

    [Header("Control Points")]
    public bool instantiateControlPointObjects = true;
    public GameObject controlPointPrefab;
    public float controlPointSpacing = 1f;

    private Transform[,] controlPoints;
    private Mesh mesh;

    void Awake()
    {
        mesh = new Mesh();
        mesh.name = "Bezier Surface";
        GetComponent<MeshFilter>().mesh = mesh;

        InitializeControlPoints();
        UpdateSurface();
    }

    void Update()
    {
        UpdateSurface();
    }

    // --------------------------------------------------
    // CONTROL POINT INITIALIZATION
    // --------------------------------------------------
    void InitializeControlPoints()
    {
        controlPoints = new Transform[controlPointsU, controlPointsV];

        float width  = controlPointSpacing * (controlPointsU - 1);
        float height = controlPointSpacing * (controlPointsV - 1);
        BoxCollider box = GetComponent<BoxCollider>();
        box.size = new Vector3(width, width, height/2);
        box.center = new Vector3(width/2, width/2, 0);

        for (int u = 0; u < controlPointsU; u++)
        {
            for (int v = 0; v < controlPointsV; v++)
            {
                // Linear grid coordinates
                float x = u * controlPointSpacing;
                float y = v * controlPointSpacing;
                float z = 0f; // flat surface

                Vector3 localPos = new Vector3(x, y, z);
                Vector3 worldPos = transform.TransformPoint(localPos);

                GameObject cp;
                if (instantiateControlPointObjects && controlPointPrefab != null)
                {
                    cp = Instantiate(controlPointPrefab, worldPos, Quaternion.identity);
                }
                else
                {
                    cp = new GameObject();
                    cp.transform.position = worldPos;
                }

                cp.transform.SetParent(transform, true);
                cp.name = $"CP_{u}_{v}";
                controlPoints[u, v] = cp.transform;
            }
        }
    }


    // --------------------------------------------------
    // SURFACE GENERATION
    // --------------------------------------------------
    void UpdateSurface()
    {
        int row = resolution + 1;
        int baseVertCount = row * row;

        // +2 for cap centers
        Vector3[] vertices = new Vector3[baseVertCount + 2];

        // base surface triangles
        int surfaceTriCount = resolution * resolution * 6;

        // caps: each needs resolution triangles
        int capTriCount = resolution * 3 * 2;

        int[] triangles = new int[surfaceTriCount + capTriCount];

        // --------------------------------------------------
        // VERTICES (surface)
        // --------------------------------------------------
        int vtx = 0;
        for (int y = 0; y <= resolution; y++)
        {
            float v = y / (float)resolution;

            for (int x = 0; x <= resolution; x++)
            {
                float u = x / (float)resolution;
                vertices[vtx++] = EvaluateBezierSurface(u, v);
            }
        }

        // --------------------------------------------------
        // ADD CAP CENTER VERTICES
        // --------------------------------------------------
        int capCenterMin = baseVertCount;
        int capCenterMax = baseVertCount + 1;

        Vector3 centerMin = Vector3.zero;
        Vector3 centerMax = Vector3.zero;

        for (int x = 0; x <= resolution; x++)
        {
            centerMin += vertices[x];                         // y = 0 row
            centerMax += vertices[resolution * row + x];     // y = max row
        }

        vertices[capCenterMin] = centerMin / (resolution + 1);
        vertices[capCenterMax] = centerMax / (resolution + 1);

        // --------------------------------------------------
        // SURFACE TRIANGLES (your original)
        // --------------------------------------------------
        int t = 0;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = y * row + x;

                triangles[t++] = i;
                triangles[t++] = i + 1;
                triangles[t++] = i + row;

                triangles[t++] = i + 1;
                triangles[t++] = i + row + 1;
                triangles[t++] = i + row;
            }
        }

        // --------------------------------------------------
        // END CAP AT V = 0
        // --------------------------------------------------
        for (int x = 0; x < resolution; x++)
        {
            triangles[t++] = capCenterMin;
            triangles[t++] = x + 1;
            triangles[t++] = x;
        }

        // --------------------------------------------------
        // END CAP AT V = MAX
        // --------------------------------------------------
        int topRow = resolution * row;

        for (int x = 0; x < resolution; x++)
        {
            triangles[t++] = capCenterMax;
            triangles[t++] = topRow + x;
            triangles[t++] = topRow + x + 1;
        }

        // --------------------------------------------------
        // APPLY MESH
        // --------------------------------------------------
        mesh.Clear();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
    }


    // --------------------------------------------------
    // GENERAL BEZIER SURFACE EVALUATION
    // --------------------------------------------------
    Vector3 EvaluateBezierSurface(float u, float v)
    {
        Vector3 point = Vector3.zero;

        int n = controlPointsU - 1;
        int m = controlPointsV - 1;

        for (int i = 0; i <= n; i++)
        {
            float bu = Bernstein(n, i, u);

            for (int j = 0; j <= m; j++)
            {
                float bv = Bernstein(m, j, v);

                Vector3 localCP =
                    transform.InverseTransformPoint(controlPoints[i, j].position);

                point += localCP * bu * bv;
            }
        }

        return point;
    }

    // --------------------------------------------------
    // BERNSTEIN POLYNOMIAL
    // --------------------------------------------------
    float Bernstein(int n, int i, float t)
    {
        return Binomial(n, i) * Mathf.Pow(t, i) * Mathf.Pow(1f - t, n - i);
    }

    float Binomial(int n, int k)
    {
        float res = 1f;
        for (int i = 1; i <= k; i++)
        {
            res *= (n - (k - i));
            res /= i;
        }
        return res;
    }
}
