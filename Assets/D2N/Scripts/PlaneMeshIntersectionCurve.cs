using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PlaneMeshIntersectionCurve : MonoBehaviour
{
    [Header("Target")]
    public MeshFilter targetMesh;

    [Header("Settings")]
    public float weldTolerance = 0.001f;
    public bool flipWinding = false; // Toggle this if you need clockwise vs counter-clockwise

    private Plane slicingPlane;

    // Cached state
    private List<Vector3> cachedCurve = new List<Vector3>();
    private bool curveDirty = true;

    void RebuildCurve()
    {
        if (targetMesh == null || targetMesh.sharedMesh == null) return;
        //Debug.LogError("Entered intersection curve");

        slicingPlane = new Plane(transform.up, transform.position);

        List<Segment> segments = ComputeIntersectionSegments();
        //Debug.LogError("Segment count is : " + segments.Count);
        if (segments.Count <50)
        {
            //Debug.LogError("Entered less count");
            for (int i = 0;i<10;i++)
            {
                slicingPlane = new Plane(transform.up, transform.position+transform.up * (0.001f * i));
                segments = ComputeIntersectionSegments();
                if (segments.Count<50)
                {
                    break;
                }
            }  
        }
        cachedCurve = BuildContinuousCurve(segments);

        curveDirty = true;
    }

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

    void TryTriangleIntersection(Vector3 v0, Vector3 v1, Vector3 v2, List<Segment> segments)
    {
        float d0 = slicingPlane.GetDistanceToPoint(v0);
        float d1 = slicingPlane.GetDistanceToPoint(v1);
        float d2 = slicingPlane.GetDistanceToPoint(v2);

        List<Vector3> hits = new List<Vector3>();

        if (EdgeIntersect(v0, v1, d0, d1, out Vector3 p01)) hits.Add(p01);
        if (EdgeIntersect(v1, v2, d1, d2, out Vector3 p12)) hits.Add(p12);
        if (EdgeIntersect(v2, v0, d2, d0, out Vector3 p20)) hits.Add(p20);

        // Standard intersection will produce exactly 2 points per triangle
        if (hits.Count == 2)
            segments.Add(new Segment(hits[0], hits[1]));
    }

    bool EdgeIntersect(Vector3 a, Vector3 b, float da, float db, out Vector3 hit)
    {
        const float EPS = 1e-5f;
        hit = Vector3.zero;

        if (Mathf.Abs(da) < EPS && Mathf.Abs(db) < EPS) return false;
        if (da > EPS && db > EPS) return false;
        if (da < -EPS && db < -EPS) return false;

        float t = da / (da - db);
        if (t <= EPS || t >= 1f - EPS) return false;

        hit = a + t * (b - a);
        return true;
    }

    #endregion

    #region Curve Building

    List<Vector3> BuildContinuousCurve(List<Segment> segments)
    {
        if (segments.Count == 0) return new List<Vector3>();

        List<Vector3> curve = new List<Vector3>();
        
        // Pick the first segment to start the chain
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
                // Link end to start
                if (Vector3.Distance(end, segments[i].a) < weldTolerance)
                {
                    curve.Add(segments[i].b);
                    segments.RemoveAt(i);
                    found = true;
                    break;
                }
                // Link end to end (reverse segment)
                if (Vector3.Distance(end, segments[i].b) < weldTolerance)
                {
                    curve.Add(segments[i].a);
                    segments.RemoveAt(i);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                // Start a new chain from remaining segments
                Segment next = segments[0];
                segments.RemoveAt(0);

                curve.Add(next.a);
                curve.Add(next.b);
            }
        }

        // --- ENFORCE WINDING DIRECTION ---
        if (curve.Count >= 3)
        {
            // 1. Calculate the center of the contour
            Vector3 center = Vector3.zero;
            for (int i = 0; i < curve.Count; i++) center += curve[i];
            center /= curve.Count;

            // 2. Use the cross product of two vectors from the center to check winding
            Vector3 dir1 = curve[0] - center;
            Vector3 dir2 = curve[1] - center;
            Vector3 normal = Vector3.Cross(dir1, dir2);

            // 3. Compare with Plane Normal (transform.up)
            float dot = Vector3.Dot(normal, transform.up);

            // If dot is negative, the points are winding the "wrong" way relative to the plane
            bool shouldReverse = flipWinding ? dot > 0 : dot < 0;

            if (shouldReverse)
            {
                curve.Reverse();
            }
        }

        return curve;
    }

    #endregion

    #region Helper Struct

    struct Segment
    {
        public Vector3 a;
        public Vector3 b;
        public Segment(Vector3 a, Vector3 b) { this.a = a; this.b = b; }
    }

    #endregion
}