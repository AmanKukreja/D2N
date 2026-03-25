using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class PolygonObstacle : MonoBehaviour
{
    [Header("References")]
    public FluidSimulation sim;
    public PlaneMeshIntersectionCurve intersectionSource;

    [Header("Rendering")]
    public float lineWidth = 0.01f;
    public Color lineColor = Color.green;

    LineRenderer line;
    List<Vector3> lastCurve = new List<Vector3>();
    bool appliedOnce = false;

    [Header("Obstacle Tuning")]
    [Range(0.1f, 5f)]
    public float obstacleScale = 1.5f;

    public float distFromCentroid;

    public Vector3 CentroidOri;


    void Start()
    {
        line = GetComponent<LineRenderer>();

        line.useWorldSpace = true;
        line.loop = true;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.material = new Material(Shader.Find("Unlit/Color"));
        line.material.color = lineColor;
        line.material.renderQueue = 3000;

        StartCoroutine(InitialUpdate());
    }

    System.Collections.IEnumerator InitialUpdate()
    {
        // Wait until dependencies are ready
        while (sim == null || !sim.IsInitialized || intersectionSource == null)
            yield return null;

        UpdateOnClick();
    }

    public void UpdateOnClick()
    {
        // if (sim == null || !sim.IsInitialized)
        //     return;

        // if (intersectionSource == null)
        //     return;

        List<Vector3> curve = intersectionSource.GetIntersectionCurve();

        if (curve == null || curve.Count < 3)
            return;
        
        // Debug.LogError("Reached Here Changed curve");
        UpdateObstacle(curve);
        appliedOnce = true;
    }


    bool CurveChanged(List<Vector3> curve)
    {
        if (curve.Count != lastCurve.Count)
            return true;

        for (int i = 0; i < curve.Count; i++)
        {
            if ((curve[i] - lastCurve[i]).sqrMagnitude > 1e-8f)
                return true;
        }

        return false;
    }

    static void ScalePolygon(List<Vector2> verts, float scale)
    {
        if (verts == null || verts.Count == 0)
            return;

        Vector2 centroid = Vector2.zero;
        for (int i = 0; i < verts.Count; i++)
            centroid += verts[i];
        centroid /= verts.Count;

        for (int i = 0; i < verts.Count; i++)
            verts[i] = centroid + (verts[i] - centroid) * scale;
    }

    static void ScalePolygon3D(List<Vector3> verts, float scale)
    {
        if (verts == null || verts.Count == 0)
            return;

        Vector3 centroid = Vector3.zero;
        for (int i = 0; i < verts.Count; i++)
            centroid += verts[i];
        centroid /= verts.Count;

        for (int i = 0; i < verts.Count; i++)
            verts[i] = centroid + (verts[i] - centroid) * scale;
    }

    void UpdateObstacle(List<Vector3> curve)
    {
        List<Vector3> newcurve = FlattenPlanarPointsToXY(curve);
        
        List<Vector2> verts = new List<Vector2>(newcurve.Count);

        lastCurve.Clear();

        for (int i = 0; i < newcurve.Count; i++)
        {
            Vector3 p = newcurve[i];
            lastCurve.Add(curve[i]);
            verts.Add(sim.WorldToSim(p));
        }

        ScalePolygon(verts, obstacleScale);
        
        sim.SetPolygonObstacle(verts);
        ScalePolygon3D(newcurve, obstacleScale);
        UpdateLineRenderer(newcurve);
    }

    List<Vector3> FlattenPlanarPointsToXY(List<Vector3> points)
    {
        if (points == null || points.Count < 3)
            throw new System.ArgumentException("At least three points are required.");

        CentroidOri = Vector3.zero;
        for (int i = 0; i < points.Count; i++)
            CentroidOri += points[i];
        CentroidOri /= points.Count;

        // float distFromCentroid = Vector3.Distance(centroid, intersectionPlane.transform.position);

        // Debug.LogError("" + CentroidOri);

        // --- Compute plane normal from first non-collinear triplet ---
        Vector3 p0 = points[0];
        Vector3 planeNormal = Vector3.zero;

        for (int i = 1; i < points.Count - 1 && planeNormal == Vector3.zero; i++)
        {
            for (int j = i + 1; j < points.Count; j++)
            {
                Vector3 v1 = points[i] - p0;
                Vector3 v2 = points[j] - p0;

                Vector3 n = Vector3.Cross(v1, v2);
                if (n.sqrMagnitude > 1e-8f)
                {
                    planeNormal = n.normalized;
                    break;
                }
            }
        }

        if (planeNormal == Vector3.zero)
            throw new System.InvalidOperationException("Points are collinear or degenerate.");

        // --- Rotate plane normal onto +Z (0,0,1) ---
        Quaternion rotation = Quaternion.FromToRotation(planeNormal, Vector3.forward);

        List<Vector3> result = new List<Vector3>(points.Count);

        // --- Apply rotation ---
        for (int i = 0; i < points.Count; i++)
            result.Add(rotation * points[i]);

        // --- Compute centroid ---
        Vector3 centroid = Vector3.zero;
        for (int i = 0; i < result.Count; i++)
            centroid += result[i];
        centroid /= result.Count;

        // --- Translate to origin ---
        for (int i = 0; i < result.Count; i++)
            result[i] -= centroid;

        return result;
    }

    void UpdateLineRenderer(List<Vector3> curve)
    {
        int count = curve.Count;
        if (count < 2)
            return;

        line.positionCount = count;

        for (int i = 0; i < count; i++)
        {
            Vector3 p = curve[i];
            p.z = 0; // align with fluid quad
            //p.y=p.y+1.4f;
            line.SetPosition(i, p);
        }
    }

}
