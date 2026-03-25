using UnityEngine;
using System.Collections;

public class Tube : MonoBehaviour
{
    public float snapDelay = 0.05f;

    private Transform bestHole;
    private Transform bestAttachPoint;


    // Use a trigger collider on the tube to detect valves
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "PneumaticComp")
        {
            bestHole = other.transform;
            bestAttachPoint = transform;
            //SnapTubeToValve();
            // Debug.LogError("Best hole and attach point found");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        bestHole = null;
        bestAttachPoint = null;
    }

    // Call this from the tube's XR OnSelectExited event
    public void SnapTubeToValve()
    {
        // Debug.LogError("Best hole is" + bestHole);
        if (bestHole != null)
        {
            StartCoroutine(DelayedSnapToValve(bestAttachPoint, bestHole));
        }
    }

    private IEnumerator DelayedSnapToValve(Transform bestAttachPoint, Transform bestHole)
    {
        yield return new WaitForSeconds(snapDelay);
        TrySnapToValve(bestAttachPoint, bestHole);
    }
    private void TrySnapToValve(Transform bestAttachPoint, Transform bestHole)
    {
        if (bestHole == null || bestAttachPoint == null) return;

        // --- Snap with ROTATE FIRST, then MOVE (matches your Snap() logic) ---
        // 1) Match up directions
        Quaternion rotationOffset = Quaternion.FromToRotation(bestAttachPoint.up, bestHole.up);
        transform.parent.rotation = rotationOffset * transform.parent.rotation; 

        // 2) Recompute offset after rotation and align positions
        Vector3 positionOffset = bestHole.position - bestAttachPoint.position;
        transform.parent.position += positionOffset;
    }
}
