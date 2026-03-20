using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PlaneMeshIntersectionCurveWorkingBase : MonoBehaviour
{
    [Header("Target")]
    public MeshFilter targetMesh;

    // [Header("Rendering")]
    // public LineRenderer lineRenderer;
    // public float lineWidth = 0.001f;

    [Header("Settings")]
    public float weldTolerance = 0.001f;

    private Plane slicingPlane;

    // Cached state
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private List<Vector3> cachedCurve = new List<Vector3>();
    private bool curveDirty = true;

    // void Awake()
    // {
    //     // Cache transform state
    //     lastPosition = transform.position;
    //     lastRotation = transform.rotation;

    //     // LineRenderer setup
    //     if (lineRenderer == null)
    //         lineRenderer = GetComponent<LineRenderer>();

    //     lineRenderer.loop = true;
    //     lineRenderer.useWorldSpace = true;
    //     lineRenderer.widthMultiplier = lineWidth;
    //     lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    // }

    // void UpdateOnClick()
    // {
    //     if (!Application.isPlaying)
    //         return;

    //     if (targetMesh == null || targetMesh.sharedMesh == null)
    //         return;

    //     // Detect plane movement or rotation
    //     if (transform.position != lastPosition ||
    //         transform.rotation != lastRotation)
    //     {
    //         curveDirty = true;
    //         lastPosition = transform.position;
    //         lastRotation = transform.rotation;
    //     }

    //     if (curveDirty)
    //         RebuildCurve();
    // }

    void RebuildCurve()
    {
        slicingPlane = new Plane(transform.up, transform.position);
        Debug.LogError("Rebuilding curve");

        List<Segment> segments = ComputeIntersectionSegments();
        cachedCurve = BuildContinuousCurve(segments);

        // RenderCurve(cachedCurve);
        curveDirty = true;
    }

    // External access (used by PolygonObstacle)
    public List<Vector3> GetIntersectionCurve()
    {
        if (curveDirty)
            RebuildCurve();

        return cachedCurve;
    }

    #region Intersection Logic

    List<Segment> ComputeIntersectionSegments()
    {
        Mesh mesh = targetMesh.sharedMesh;
        Transform t = targetMesh.transform;

        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;

        List<Segment> segments = new List<Segment>();

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 v0 = t.TransformPoint(verts[tris[i]]);
            Vector3 v1 = t.TransformPoint(verts[tris[i + 1]]);
            Vector3 v2 = t.TransformPoint(verts[tris[i + 2]]);

            TryTriangleIntersection(v0, v1, v2, segments);
        }

        return segments;
    }

    void TryTriangleIntersection(
        Vector3 v0,
        Vector3 v1,
        Vector3 v2,
        List<Segment> segments)
    {
        float d0 = slicingPlane.GetDistanceToPoint(v0);
        float d1 = slicingPlane.GetDistanceToPoint(v1);
        float d2 = slicingPlane.GetDistanceToPoint(v2);

        List<Vector3> hits = new List<Vector3>();

        if (EdgeIntersect(v0, v1, d0, d1, out Vector3 p01)) hits.Add(p01);
        if (EdgeIntersect(v1, v2, d1, d2, out Vector3 p12)) hits.Add(p12);
        if (EdgeIntersect(v2, v0, d2, d0, out Vector3 p20)) hits.Add(p20);

        if (hits.Count == 2)
            segments.Add(new Segment(hits[0], hits[1]));
    }

    bool EdgeIntersect(
        Vector3 a,
        Vector3 b,
        float da,
        float db,
        out Vector3 hit)
    {
        const float EPS = 1e-5f;
        hit = Vector3.zero;

        if (Mathf.Abs(da) < EPS && Mathf.Abs(db) < EPS)
            return false;

        if (da > EPS && db > EPS) return false;
        if (da < -EPS && db < -EPS) return false;

        float t = da / (da - db);

        if (t <= EPS || t >= 1f - EPS)
            return false;

        hit = a + t * (b - a);
        return true;
    }

    #endregion

    #region Curve Building

    List<Vector3> BuildContinuousCurve(List<Segment> segments)
    {
        if (segments.Count == 0)
            return new List<Vector3>();

        List<Vector3> curve = new List<Vector3>();
        Segment current = segments[0];
        segments.RemoveAt(0);

        curve.Add(current.a);
        curve.Add(current.b);

        while (segments.Count > 0)
        {
            Vector3 end = curve[curve.Count - 1];
            bool found = false;

            for (int i = 0; i < segments.Count; i++)
            {
                if (Vector3.Distance(end, segments[i].a) < weldTolerance)
                {
                    curve.Add(segments[i].b);
                    segments.RemoveAt(i);
                    found = true;
                    break;
                }
                if (Vector3.Distance(end, segments[i].b) < weldTolerance)
                {
                    curve.Add(segments[i].a);
                    segments.RemoveAt(i);
                    found = true;
                    break;
                }
            }

            if (!found)
                break;
        }

        return curve;
    }

    #endregion

    // #region Rendering

    // void RenderCurve(List<Vector3> curve)
    // {
    //     if (curve == null || curve.Count < 2)
    //     {
    //         lineRenderer.positionCount = 0;
    //         return;
    //     }

    //     lineRenderer.positionCount = curve.Count;
    //     lineRenderer.SetPositions(curve.ToArray());
    // }

    // #endregion

    #region Helper Struct

    struct Segment
    {
        public Vector3 a;
        public Vector3 b;

        public Segment(Vector3 a, Vector3 b)
        {
            this.a = a;
            this.b = b;
        }
    }

    #endregion
}
