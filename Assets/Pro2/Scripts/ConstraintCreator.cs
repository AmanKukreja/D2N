using UnityEngine;
using System.Collections;
using Oculus.Interaction;

public class ConstraintCreator : MonoBehaviour
{
    private Vector3 snappedAxis;
    private void OnTriggerEnter(Collider other)
    {
        //Debug.LogError("Triggered with: " + other.name);
        Rigidbody targetRb = other.attachedRigidbody;
        if (targetRb == null || !other.CompareTag("ConstraintObject") || other.GetComponent<OneGrabTranslateTransformer>() != null || other.GetComponent<OneGrabRotateTransformer>() != null)
            return;

        if (other.name == "Board")
        {
            return;
        }

        GameObject newParent = new GameObject("New Parent of " + other.name);

        newParent.transform.position = other.transform.position;
        newParent.transform.rotation = other.transform.rotation;
        newParent.transform.localScale = other.transform.localScale;

        other.transform.SetParent(newParent.transform);

        // Step 1: Snap this trigger's up vector to the nearest axis of the other object's transform
        snappedAxis = SnapToNearestAxisOfOtherObject(other.transform, transform.up);
        // Debug.LogError("Snapped to Other's Axis: " + snappedAxis);
        // whenleft();

        // Step 2: Align object to that axis
        Vector3 forward = GetPerpendicularVector(snappedAxis).normalized;
        other.transform.rotation = Quaternion.LookRotation(forward, snappedAxis);

        // Step 3: Get OneGrabTranslateTransformer
        var translateTransformer = other.GetComponent<OneGrabTranslateTransformer>();
        if (translateTransformer == null)
        {
            translateTransformer = other.gameObject.AddComponent<OneGrabTranslateTransformer>();
        }

        // Step 4: Convert snapped axis to local space
        Vector3 localSnappedAxis = other.transform.InverseTransformDirection(snappedAxis).normalized;

        // Step 5: Compute length of object along snapped axis
        float length = transform.localScale.z*transform.parent.localScale.z;//GetObjectLengthAlongAxis(other.transform, snappedAxis);
        //Debug.LogError("child length " + transform.localScale.z + "Parent length " + transform.parent.localScale.z + "Total Length" + length);
        // Step 6: Apply constraints directly
        var constraints = translateTransformer.Constraints;
        constraints.ConstraintsAreRelative = true;

        constraints.MinX.Constrain = true;
        constraints.MaxX.Constrain = true;
        constraints.MinY.Constrain = true;
        constraints.MaxY.Constrain = true;
        constraints.MinZ.Constrain = true;
        constraints.MaxZ.Constrain = true;

        constraints.MinX.Value = 0; //other.transform.position.x;
        constraints.MaxX.Value = 0; //other.transform.position.x;
        constraints.MinY.Value = other.transform.localPosition.y - length/2;
        constraints.MaxY.Value = other.transform.localPosition.y + length/2;
        constraints.MinZ.Value = 0; //other.transform.position.z;
        constraints.MaxZ.Value = 0; //other.transform.position.z;   
      
        var grabbable = other.GetComponent<Grabbable>();
        if (grabbable != null)
        {
            translateTransformer.Initialize(grabbable);
            grabbable.InjectOptionalOneGrabTransformer(translateTransformer);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.name == "Board")
        {
            return;
        }

        //Debug.LogError("On trigger of linear constraint fired for " + other.gameObject.name);
        if (other.TryGetComponent<OneGrabTranslateTransformer>(out var transformer))
        {
            Destroy(transformer);

            Transform parent = other.transform.parent;
            if (parent != null)
            {
                // Store world transform before unparenting
                Vector3 worldPos = other.transform.position;
                Quaternion worldRot = other.transform.rotation;
                Vector3 worldScale = other.transform.lossyScale;

                // Unparent
                other.transform.SetParent(null);

                // Restore world transform
                other.transform.position = worldPos;
                other.transform.rotation = worldRot;
                other.transform.localScale = worldScale;

                //Debug.LogError("New Parent for" + other.name);
                // Destroy the parent GameObject
                if (parent.name == "New Parent for" + other.name)
                {
                    // Before destroying parent
                    transform.SetParent(null, true); // true keeps world position/rotation
                    Destroy(parent.gameObject);
                }
            }
            if (other.TryGetComponent<Grabbable>(out var grabbable) && other.TryGetComponent<GrabFreeTransformer>(out var grabFreeTransformer))
            {
                grabFreeTransformer.Initialize(grabbable);
                grabbable.InjectOptionalOneGrabTransformer(grabFreeTransformer);
            }
        }
    }

    IEnumerator AlignAfterDelay(Transform childTransform, Vector3 targetWorldUp)
    {
        yield return new WaitForEndOfFrame(); // Or a small delay like WaitForSeconds(0.05f)
        AlignParentUpToTargetAxis(childTransform, targetWorldUp);
    }

    public void whenleft()
    {
        //Debug.LogError("Entered here with snapped axis: " + snappedAxis);
        StartCoroutine(AlignAfterDelay(transform, snappedAxis));
    }


    // --- Snap a direction to the closest local axis (±X, ±Y, ±Z) of the other object
    private Vector3 SnapToNearestAxisOfOtherObject(Transform otherTransform, Vector3 direction)
    {
        direction.Normalize();
        Vector3[] axes = {
            otherTransform.right, -otherTransform.right,
            otherTransform.up, -otherTransform.up,
            otherTransform.forward, -otherTransform.forward
        };

        Vector3 bestAxis = axes[0];
        float maxDot = -1f;

        foreach (var axis in axes)
        {
            float dot = Vector3.Dot(direction, axis);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestAxis = axis;
            }
        }

        return bestAxis;
    }

    private Vector3 GetPerpendicularVector(Vector3 normal)
    {
        // Return a vector that's guaranteed to not be colinear with `normal`
        if (Mathf.Abs(Vector3.Dot(normal, Vector3.up)) < 0.99f)
            return Vector3.Cross(normal, Vector3.up);
        else
            return Vector3.Cross(normal, Vector3.right);
    }


    private void AlignParentUpToTargetAxis(Transform childTransform, Vector3 targetWorldUp)
    {
        // Get the parent Transform of the child.
        Transform parent = childTransform.parent;

        // Check if the child has a parent. If not, a warning is logged as alignment isn't possible.
        if (parent == null)
        {
            Debug.LogWarning("Child Transform '" + childTransform.name + "' has no parent — cannot align parent rotation.");
            return;
        }

        // Log the alignment attempt for debugging purposes.
        Debug.Log("Aligning parent: " + parent.name + " for child: " + childTransform.name + " to target axis: " + targetWorldUp);

        // Store the original local scale of the parent. This is important to prevent
        // scaling issues that can sometimes occur when manipulating rotations.
        Vector3 originalParentScale = parent.localScale;

        // Calculating the child's CURRENT effective "up" direction in WORLD space.
        Vector3 currentChildWorldUp = childTransform.rotation * Vector3.up;

        // Calculating the Quaternion rotation needed to rotate 'currentChildWorldUp' to 'targetWorldUp'
        Quaternion rotationDifference = Quaternion.FromToRotation(currentChildWorldUp, targetWorldUp);

        // Apply this 'rotationDifference' to the parent's current world rotation.
        parent.rotation = rotationDifference * parent.rotation;

        //Restore the parent's original local scale.
        parent.localScale = originalParentScale;
    }
}
