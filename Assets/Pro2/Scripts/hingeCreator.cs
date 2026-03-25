using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;


public class hingeCreator : MonoBehaviour
{
    private Vector3 snappedAxis;
    private List<GameObject> collidedObjects = new List<GameObject>();

    public Vector3 planeNormal = Vector3.up;
    
    private float lastKinematicToggleTime;
    private float ignoreDuration = 0.1f; // seconds to ignore "fake" exits

    private void OnTriggerEnter(Collider other)
    {
        //Debug.LogError("Triggered with: " + other.name);

        Rigidbody targetRb = other.attachedRigidbody;
        if (targetRb == null || !other.CompareTag("ConstraintObject"))
            return;

        // Step 1: Snap axis
        snappedAxis = SnapToNearestAxisOfOtherObject(other.transform, transform.up);

        //snappedAxis = SnapToMeshNormalAtContactPoint(other);

        //Debug.LogError("Snapped to Other's Axis: " + snappedAxis);
        Vector3 contactPoint = transform.position;
        //Vector3 contactPoint = transform.position + new Vector3(0, 0.5f, 0);
        //Vector3 contactPoint = firstCollider.ClosestPoint(transform.position);
        //Debug.LogError("Contact Point: " + contactPoint);

        // Add to list if not already added
        if (!collidedObjects.Contains(other.gameObject))
            collidedObjects.Add(other.gameObject);

        if (collidedObjects.Count < 2)
        {
            GameObject first = collidedObjects[0];
            HingeJoint hinge = first.AddComponent<HingeJoint>();
            hinge.anchor = first.transform.InverseTransformPoint(contactPoint); //localAnchor;
            hinge.axis = first.transform.InverseTransformDirection(snappedAxis);
            //Destroy(first.GetComponent<Grabbable>());
        }
        else
        {
            Debug.LogError("Second object detected" + collidedObjects[1]);
            GameObject second = collidedObjects[1];

            GameObject first = collidedObjects[0];

            HingeJoint[] hinges = first.GetComponents<HingeJoint>();

            if (hinges.Length > 1)
            {
                HingeJoint hinge = hinges[1];
                hinge.connectedBody = second.GetComponent<Rigidbody>();
            }
            else
            {
                HingeJoint hinge = hinges[0];
                hinge.connectedBody = second.GetComponent<Rigidbody>();
            }

            Rigidbody rbody1 = first.GetComponent<Rigidbody>();
            Rigidbody rbody2 = second.GetComponent<Rigidbody>();

            if (rbody1 != null)
            {
                if (collidedObjects[1].name != "Board")
                {
                //     rbody2.isKinematic = true;
                //     rbody1.isKinematic = false;
                //     lastKinematicToggleTime = Time.time;
                // }
                // else
                // {
                    rbody2.isKinematic = false;
                    //rbody1.isKinematic = false;
                    lastKinematicToggleTime = Time.time;
                }
            }

            // Optional: Apply angular drag (correct property)
            // transform.parent.gameObject.SetActive(false);

            Destroy(second.GetComponent<GrabFreeTransformer>());

            //collidedObjects.Clear();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //Debug.LogError(Time.time + " and " + lastKinematicToggleTime);
        if (Time.time - lastKinematicToggleTime > ignoreDuration)
        {
            Rigidbody targetRb = other.attachedRigidbody;
            if (targetRb == null || !other.CompareTag("ConstraintObject"))
                return;
            Destroy(other.GetComponent<HingeJoint>());
            Debug.LogError("Reached here");
            //targetRb.isKinematic = true;
        }

    }

    IEnumerator AlignAfterDelay(Transform childTransform, Vector3 targetWorldUp)
    {
        yield return new WaitForEndOfFrame(); // Or a small delay like WaitForSeconds(0.05f)
        AlignParentUpToTargetAxis(childTransform, targetWorldUp);
        //AlignChildPreserveParent(childTransform, targetWorldUp);
    }

    public void whenleft()
    {
        // Check if snappedAxis is still the default (zero vector)
        if (snappedAxis == Vector3.zero)
        {
            Debug.LogWarning("snappedAxis has not been initialized yet. Skipping alignment.");
            return;
        }
        Debug.LogError("Entered here with snapped axis: " + snappedAxis);
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


    // --- Aligns the parent’s up vector to match the given target axis (in world space)
    private void AlignParentUpToTargetAxis(Transform childTransform, Vector3 targetWorldUp)
    {
        Transform parent = childTransform.parent;
        if (parent == null)
        {
            Debug.LogWarning("Trigger has no parent — cannot align parent rotation.");
            return;
        }

        Debug.LogError("Aligning parent: " + parent.name + " to axis: " + targetWorldUp);

        // Preserve original scale
        Vector3 originalScale = parent.localScale;

        // Get a reliable forward vector
        Vector3 forward = GetPerpendicularVector(targetWorldUp).normalized;

        // Apply rotation
        Debug.LogError("Original rotation:" + parent.rotation);
        parent.rotation = Quaternion.LookRotation(forward, targetWorldUp);
        Debug.LogError("New rotation:" + parent.rotation);

        // Restore scale
        parent.localScale = originalScale;
    }
}