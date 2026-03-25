using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Valve : MonoBehaviour
{
    public float snapDistance = 0.5f;
    public float snapDelay = 0.05f;
    public float minDist = 0.3f;        // NEW: search radius for nearby tubes
    public float holeSnapThreshold = 0.05f; // NEW: threshold for multi-snap

    public Transform[] Holes { get; private set; }

    private Transform primaryHole;       // NEW: the first snapped hole
    private Transform primaryAttach;     // NEW: the first snapped attach point

    void Awake()
    {
        Holes = new Transform[]
        {
            transform.Find("Hole1"),
            transform.Find("Hole2"),
            transform.Find("Hole3")
        };
    }

    public Transform GetClosestHole(Vector3 position)
    {
        Transform closest = null;
        float min = Mathf.Infinity;
        foreach (Transform h in Holes)
        {
            float d = Vector3.Distance(position, h.position);
            if (d < min)
            {
                min = d;
                closest = h;
            }
        }
        return closest;
    }

    public void SnapValveToTube()
    {
        StartCoroutine(DelayedSnapToTube());
    }

    private IEnumerator DelayedSnapToTube()
    {
        yield return new WaitForSeconds(snapDelay);

        Tube[] tubes = FindObjectsOfType<Tube>();
        foreach (Tube tube in tubes)
        {
            if (Vector3.Distance(tube.transform.position, transform.position) <= snapDistance)
            {
                if (TrySnapToTube(tube))
                {
                    // NEW: Start search for multi-snaps once first snap succeeded
                    StartCoroutine(CheckForAdditionalSnaps());
                }
                break;
            }
        }
    }

    private bool TrySnapToTube(Tube tube)
    {
        Transform ap1 = tube.transform.Find("AttachPoint1");
        Transform ap2 = tube.transform.Find("AttachPoint2");

        if (ap1 == null && ap2 == null) return false;

        float minDist = Mathf.Infinity;
        Transform bestHole = null;
        Transform bestAttachPoint = null;

        if (ap1 != null)
        {
            Transform h = GetClosestHole(ap1.position);
            float d = Vector3.Distance(ap1.position, h.position);
            if (d < minDist) { minDist = d; bestHole = h; bestAttachPoint = ap1; }
        }
        if (ap2 != null)
        {
            Transform h = GetClosestHole(ap2.position);
            float d = Vector3.Distance(ap2.position, h.position);
            if (d < minDist) { minDist = d; bestHole = h; bestAttachPoint = ap2; }
        }

        if (bestHole == null || bestAttachPoint == null) return false;

        // Rotate first, then move
        Quaternion rotationOffset = Quaternion.FromToRotation(bestHole.up, bestAttachPoint.up);
        transform.rotation = rotationOffset * transform.rotation;
        Vector3 positionOffset = bestAttachPoint.position - bestHole.position;
        transform.position += positionOffset;

        primaryHole = bestHole;        // NEW: remember first snapped pair
        primaryAttach = bestAttachPoint;
        return true;
    }

    // NEW: checks for other tubes to snap while keeping first snap
    private IEnumerator CheckForAdditionalSnaps()
    {
        while (true)
        {
            Tube[] tubes = FindObjectsOfType<Tube>();
            foreach (Tube tube in tubes)
            {
                if (tube == null) continue;
                if (tube.transform == primaryAttach?.parent) continue; // skip primary tube

                // Gather attach points
                List<Transform> attachPoints = new List<Transform>();
                Transform ap1 = tube.transform.Find("AttachPoint1");
                Transform ap2 = tube.transform.Find("AttachPoint2");
                if (ap1 != null) attachPoints.Add(ap1);
                if (ap2 != null) attachPoints.Add(ap2);

                foreach (Transform ap in attachPoints)
                {
                    foreach (Transform h in Holes)
                    {
                        if (h == primaryHole) continue; // skip the already used hole

                        // --- Compute angle around primary axis to align this hole with this attach point ---
                        Vector3 pivot = primaryAttach.position;
                        Vector3 axis = primaryAttach.up;

                        // Vector from pivot to hole/attach (projected onto plane perpendicular to axis)
                        Vector3 vHole = h.position - pivot;
                        Vector3 vAttach = ap.position - pivot;

                        Vector3 vHoleProj = Vector3.ProjectOnPlane(vHole, axis);
                        Vector3 vAttachProj = Vector3.ProjectOnPlane(vAttach, axis);

                        // If either is degenerate, skip
                        if (vHoleProj.sqrMagnitude < 1e-6f || vAttachProj.sqrMagnitude < 1e-6f)
                            continue;

                        // Find signed angle between projections
                        float angle = Vector3.SignedAngle(vHoleProj, vAttachProj, axis);

                        // --- Apply rotation about the pivot/axis ---
                        transform.RotateAround(pivot, axis, angle);

                        // --- Re-lock the primary hole to its attach point ---
                        Vector3 lockOffset = primaryAttach.position - primaryHole.position;
                        transform.position += lockOffset;

                        // --- Final check ---
                        float dist = Vector3.Distance(h.position, ap.position);
                        if (dist < holeSnapThreshold)
                        {
                            // Perfect, snapped!
                            yield break;
                        }
                    }
                }
            }
            yield return null;
        }
    }
}

// using UnityEngine;
// using System.Collections;

// public class Valve : MonoBehaviour
// {
//     public float snapDistance = 0.5f;
//     public float snapDelay = 0.05f;

//     public Transform[] Holes { get; private set; }

//     void Awake()
//     {
//         Holes = new Transform[]
//         {
//             transform.Find("Hole1"),
//             transform.Find("Hole2"),
//             transform.Find("Hole3")
//         };
//         SnapValveToTube();
//     }

//     public Transform GetClosestHole(Vector3 position)
//     {
//         Transform closest = null;
//         float min = Mathf.Infinity;
//         foreach (Transform h in Holes)
//         {
//             float d = Vector3.Distance(position, h.position);
//             if (d < min)
//             {
//                 min = d;
//                 closest = h;
//             }
//         }
//         return closest;
//     }

//     // Call this from the valve's XR OnSelectExited event
//     public void SnapValveToTube()
//     {
//         StartCoroutine(DelayedSnapToTube());
//     }

//     private IEnumerator DelayedSnapToTube()
//     {
//         yield return new WaitForSeconds(snapDelay);

//         Tube[] tubes = FindObjectsOfType<Tube>();
//         foreach (Tube tube in tubes)
//         {
//             if (Vector3.Distance(tube.transform.position, transform.position) <= snapDistance)
//             {
//                 TrySnapToTube(tube);
//                 break;
//             }
//         }
//     }

//     private void TrySnapToTube(Tube tube)
//     {
//         // Get the tube's attach points locally (search within that gameobject)
//         Transform ap1 = tube.transform.Find("AttachPoint1");
//         Transform ap2 = tube.transform.Find("AttachPoint2");

//         if (ap1 == null && ap2 == null) return;

//         float minDist = Mathf.Infinity;
//         Transform bestHole = null;
//         Transform bestAttachPoint = null;

//         // Consider whichever attach points exist
//         if (ap1 != null)
//         {
//             Transform h = GetClosestHole(ap1.position);
//             float d = Vector3.Distance(ap1.position, h.position);
//             if (d < minDist) { minDist = d; bestHole = h; bestAttachPoint = ap1; }
//         }
//         if (ap2 != null)
//         {
//             Transform h = GetClosestHole(ap2.position);
//             float d = Vector3.Distance(ap2.position, h.position);
//             if (d < minDist) { minDist = d; bestHole = h; bestAttachPoint = ap2; }
//         }

//         if (bestHole == null || bestAttachPoint == null) return;

//         // --- Snap with ROTATE FIRST, then MOVE (mirrors tube logic) ---
//         // 1) Match up directions (rotate the VALVE so hole.up matches attachPoint.up)
//         Quaternion rotationOffset = Quaternion.FromToRotation(bestHole.up, bestAttachPoint.up);
//         transform.rotation = rotationOffset * transform.rotation;

//         // 2) Recompute offset after rotation and align positions
//         Vector3 positionOffset = bestAttachPoint.position - bestHole.position;
//         transform.position += positionOffset;
//     }
// }
