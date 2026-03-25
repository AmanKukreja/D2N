using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using System.Linq;
using Unity.XR.CoreUtils;

public class OnPinExit : MonoBehaviour
{

    private HingeJoint hinge;
    private float lastKinematicToggleTime;
    private float ignoreDuration = 1f;

    private RotConstraintCreator childScript;

    private void Start()
    {
        // Get reference to ChildScript in children
        childScript = GetComponentInChildren<RotConstraintCreator>();
    }

    private void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("ConstraintObject") || !other.TryGetComponent<Rigidbody>(out var targetRb))
            return;
        lastKinematicToggleTime = Time.time;
    }
    private void OnTriggerExit(Collider other)
    {
        if (Time.time - lastKinematicToggleTime < ignoreDuration)
        {
            return;
        }
        if (!other.CompareTag("ConstraintObject") || !other.TryGetComponent<Rigidbody>(out var targetRb))
            return;

        childScript.ClearCollisions();
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
            //Debug.LogError("Removing Rotate because of " + other.gameObject.name + "");
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
        StartCoroutine(ActivateAfterDelay());
    }
    private IEnumerator ActivateAfterDelay()
    {
        // wait for 2 seconds
        yield return new WaitForSeconds(2f);

        // find the child by name
        Transform hingeCreator = transform.Find("HingeCreator");
        if (hingeCreator != null)
        {
            hingeCreator.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("HingeCreator not found under " + gameObject.name);
        }
    }
}