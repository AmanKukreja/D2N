using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using System.Linq;
using Unity.XR.CoreUtils;

public class RotConstraintCreator : MonoBehaviour
{
    private Vector3 snappedAxis;
    private GameObject pivot;
    private GameObject secondpivot;
    private HingeJoint hinge;
    private List<GameObject> collidedObjects = new List<GameObject>();
    
    private float lastKinematicToggleTime;
    private float ignoreDuration = 0.05f; 
    private Renderer rend;
    private Color originalColor;
    // public Color toggleColor = Color.green;

    // void ResetColor()
    // {
    //     rend.material.color = originalColor;
    // }
    
    public void ClearCollisions()
    {
        collidedObjects.Clear();
    }
    
    private void switchOffHandGrab(GameObject other, Vector3 contactPoint)
    { 
        //Debug.LogError("Reached here" + other.GetComponent<OneGrabRotateTransformer>());
        //// Code to detect if a two pins have entered then disable hand grabbable
        if (other.GetComponent<OneGrabRotateTransformer>() != null)
        {
            //Debug.LogError("I don not know why this is happening");
            if (other.TryGetComponent<HandGrabInteractable>(out var handgrabbable))
            {
                handgrabbable.enabled = false;
                Transform secondExistingPivotTransform = other.transform.Find("SecondRotationPivot");

                if (secondExistingPivotTransform != null)
                {
                    secondpivot = secondExistingPivotTransform.gameObject;
                }
                else
                {
                    secondpivot = new GameObject("SecondRotationPivot");
                    secondpivot.transform.SetParent(other.transform, worldPositionStays: true);
                }
                secondpivot.transform.position = contactPoint;
                Vector3 forward = GetPerpendicularVector(snappedAxis).normalized;
                secondpivot.transform.rotation = Quaternion.LookRotation(forward, snappedAxis);
            }
            return;
        }
        /////////////////////////////////
    }

    private void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("ConstraintObject") || !other.TryGetComponent<Rigidbody>(out var targetRb) || other.GetComponent<OneGrabTranslateTransformer>() != null)
            return;

        // // Enable Grabbable on the current object
        // Grabbable currentGrabbable = other.GetComponent<Grabbable>();
        // if (currentGrabbable != null)
        //     currentGrabbable.enabled = true;

        // targetRb.linearDamping=1000; // this is to stop hinge from acting

        snappedAxis = SnapToNearestAxisOfOtherObject(other.transform, transform.up);
        Vector3 contactPoint = transform.position;// other.ClosestPoint(transform.position);

        if (!collidedObjects.Contains(other.gameObject))
            collidedObjects.Add(other.gameObject);

        if (collidedObjects.Count == 2)
        {
            if (Time.time - lastKinematicToggleTime < ignoreDuration)
            {
                return;
            }
            Debug.LogError("Reached inside second object");

            GameObject first = collidedObjects[0];
            GameObject second = collidedObjects[1];

            Debug.LogError($"{first.name}: {second.name}");

            if (collidedObjects[0].GetComponent<HingeJoint>() != null)
            {
                if (collidedObjects[1].name != "Board")
                {
                    second = collidedObjects[0];
                    first = collidedObjects[1];
                }
            }

            HingeJoint hinge = first.AddComponent<HingeJoint>();
            hinge.anchor = first.transform.InverseTransformPoint(contactPoint); //localAnchor;
            hinge.axis = first.transform.InverseTransformDirection(snappedAxis);
            hinge.connectedBody = second.GetComponent<Rigidbody>();
            Rigidbody rbody1 = first.GetComponent<Rigidbody>();
            Rigidbody rbody2 = second.GetComponent<Rigidbody>();

            if (second.name != "Board")
            {
                if (rbody2.isKinematic == true)
                {
                    rbody2.isKinematic = false;
                    lastKinematicToggleTime = Time.time;
                }
                // transform.parent.gameObject.SetActive(false);
            }
            if (first.name != "Board")
            {
                if (rbody1.isKinematic == true)
                {
                    rbody1.isKinematic = false;
                    lastKinematicToggleTime = Time.time;
                }
            }

            // If connected body for hinge is board, free up the rotations so that the object can rotate with gravity
            if (first.name == "Board")
            {
                rbody2.freezeRotation = false;
                rbody2.angularDamping = 20f;
                switchOffHandGrab(second.gameObject, contactPoint);
            }

            if (second.name == "Board")
            {
                rbody1.freezeRotation = false;
                rbody1.angularDamping = 20f;
                switchOffHandGrab(first.gameObject, contactPoint);
            }

            //////////////////////////////////////////////////////////////////////////
            Transform existingPivotTransform = first.transform.Find("RotationPivot");

            if (existingPivotTransform != null)
            {
                pivot = existingPivotTransform.gameObject;
            }
            else
            {
                pivot = new GameObject("RotationPivot");
                pivot.transform.SetParent(first.transform, worldPositionStays: true);
            }
            pivot.transform.position = contactPoint;
            Vector3 mainForward = GetPerpendicularVector(snappedAxis).normalized;
            pivot.transform.rotation = Quaternion.LookRotation(mainForward, snappedAxis);

            var rotateTransformer = first.gameObject.GetOrAddComponent<OneGrabRotateTransformer>();
            rotateTransformer.InjectOptionalPivotTransform(pivot.transform);


            if (first.TryGetComponent<Grabbable>(out var grabbable))
            {
                rotateTransformer.Initialize(grabbable);
                grabbable.InjectOptionalOneGrabTransformer(rotateTransformer);
            }
            //////////////////////////////////////////////////////////////////////////
            transform.gameObject.SetActive(false);
            // Debug.LogError(rotateTransformer);
        }
        else if (collidedObjects.Count == 3)
        {
            if (Time.time - lastKinematicToggleTime < ignoreDuration)
            {
                return;
            }
            Debug.LogError("I dont know how i got here");
            //GameObject first = collidedObjects[0];
            if (collidedObjects[2].name == "Board")
            {
                // Play sound
            }
            else
            {
                GameObject second = collidedObjects[2];
                GameObject first = collidedObjects[0];
                HingeJoint[] hinges = first.GetComponents<HingeJoint>();
                Debug.LogError(hinges.Count());
                hinge = hinges[hinges.Count()];
                hinge.connectedBody = second.GetComponent<Rigidbody>();
                Debug.LogError("Connected body added to " + second);
                Rigidbody rbody2 = second.GetComponent<Rigidbody>();
                if (rbody2.isKinematic == true)
                {
                    rbody2.isKinematic = false;
                    lastKinematicToggleTime = Time.time;
                }
                transform.parent.gameObject.SetActive(false);
                // rbody1.isKinematic = false;
                // lastKinematicToggleTime = Time.time;
            }
        }
        else
        {
            return;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (Time.time - lastKinematicToggleTime < ignoreDuration)
        {
            return;
        }
        if (!other.CompareTag("ConstraintObject") || !other.TryGetComponent<Rigidbody>(out var targetRb))
            return;
        // If handgrab interactable is present and is not enabled, which will be the case when second pin has entered
        // then run the following and set hinge anchor to whichever pin is still there (closest)
        if (other.TryGetComponent<HandGrabInteractable>(out var handgrabbable) && !handgrabbable.enabled)
        {
            handgrabbable.enabled = true;
            Transform rotationPivot = other.transform.Find("RotationPivot");
            Transform secondRotationPivot = other.transform.Find("SecondRotationPivot");

            if (rotationPivot == null || secondRotationPivot == null)
            {
                Debug.LogWarning("One or both pivots are missing.");
                return;
            }

            Vector3 myPos = transform.position;
            float distToRotationPivot = Vector3.Distance(myPos, rotationPivot.position);
            float distToSecondPivot = Vector3.Distance(myPos, secondRotationPivot.position);

            Transform closerPivot = distToRotationPivot < distToSecondPivot ? rotationPivot : secondRotationPivot;

            Debug.LogError("Deleting all collided objects");

            collidedObjects = null;

            if (other.TryGetComponent<OneGrabRotateTransformer>(out var rotateTransformer))
            {
                if (closerPivot.name == "RotationPivot")
                {
                    rotateTransformer.InjectOptionalPivotTransform(secondRotationPivot);

                    // If second pin is removed add hinge anchor as second pivot
                    hinge = other.gameObject.GetComponent<HingeJoint>();
                    hinge.anchor = other.transform.InverseTransformPoint(secondRotationPivot.position); //localAnchor;        
                }
                else
                {
                    rotateTransformer.InjectOptionalPivotTransform(rotationPivot);
                    // If second pin is removed add hinge anchor as second pivot
                    hinge = other.gameObject.GetComponent<HingeJoint>();
                    hinge.anchor = other.transform.InverseTransformPoint(rotationPivot.position); //localAnchor; 
                }
            }
            return;
        }

        // if only one pin exists and it is removed then destroy hinge and one grab rotate transformer, and set grab free transformer
        if (other.TryGetComponent<OneGrabRotateTransformer>(out var rotatehinge))
        {
            Debug.LogError("Removing Rotate");
            Destroy(rotatehinge);
            Destroy(other.gameObject.GetComponent<HingeJoint>());
            if (other.TryGetComponent<Grabbable>(out var grabbable) && other.TryGetComponent<GrabFreeTransformer>(out var grabFreeTransformer))
            {
                grabFreeTransformer.Initialize(grabbable);
                grabbable.InjectOptionalOneGrabTransformer(grabFreeTransformer);
            }

            if (other.TryGetComponent<Rigidbody>(out var HingeRb))
            {
                HingeRb.isKinematic = true;
                lastKinematicToggleTime = Time.time;
            }
        }
    }

    IEnumerator AlignAfterDelay(Transform childTransform, Vector3 targetWorldUp)
    {
        yield return new WaitForEndOfFrame();
        AlignParentUpToTargetAxis(childTransform, targetWorldUp);
    }

    public void whenleft()
    {
        StartCoroutine(AlignAfterDelay(transform, snappedAxis));
    }

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
        if (Mathf.Abs(Vector3.Dot(normal, Vector3.up)) < 0.99f)
            return Vector3.Cross(normal, Vector3.up);
        else
            return Vector3.Cross(normal, Vector3.right);
    }

    private void AlignParentUpToTargetAxis(Transform childTransform, Vector3 targetWorldUp)
    {
        Transform parent = childTransform.parent;
        if (parent == null)
        {
            Debug.LogWarning("Trigger has no parent — cannot align parent rotation.");
            return;
        }

        Vector3 originalScale = parent.localScale;
        Vector3 forward = GetPerpendicularVector(targetWorldUp).normalized;
        parent.rotation = Quaternion.LookRotation(forward, targetWorldUp);
        parent.localScale = originalScale;
    }
}

public static class ComponentExtensions
{
    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
    {
        if (!gameObject.TryGetComponent<T>(out var component))
        {
            component = gameObject.AddComponent<T>();
        }
        return component;
    }
}
