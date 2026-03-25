using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;


public class PinForRotTran : MonoBehaviour
{
    private Vector3 snappedAxis;
    private List<GameObject> collidedObjects = new List<GameObject>();

    [SerializeField] private List<GameObject> AssemblySequence = new List<GameObject>();
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Material startMaterial;
    [SerializeField] private Material endMaterial;
    private GameObject pivot;

    public Vector3 planeNormal = Vector3.up;

    private void OnTriggerEnter(Collider other)
    {

        Rigidbody targetRb = other.attachedRigidbody;
        if (targetRb == null || !other.CompareTag("ConstraintObject"))
            return;

        snappedAxis = transform.up;

        // snappedAxis = SnapToMeshNormalAtContactPoint(other);

        //Debug.LogError("Snapped to Other's Axis: " + snappedAxis);
        //Vector3 contactPoint1 = other.ClosestPoint(transform.position);

        // Add to list if not already added
        if (!collidedObjects.Contains(other.gameObject))
            collidedObjects.Add(other.gameObject);

        // Wait until two objects have collided
        if (collidedObjects.Count < 2)
            return;
        
        Vector3 contactPoint1 = transform.position;


        Vector3 contactPoint2 = other.ClosestPoint(transform.position);

        GameObject objA = collidedObjects[0];
        GameObject objB = collidedObjects[1];

        Renderer rendA = objA.GetComponent<Renderer>();
        Renderer rendB = objB.GetComponent<Renderer>();

        // Default assignments
        GameObject first = objA;
        Vector3 contactPoint = contactPoint1;
        GameObject second = objB;

        // Make sure first always has startMaterial
        if (rendB != null && rendB.sharedMaterial == startMaterial)
        {
            first = objB;
            contactPoint = contactPoint2;
            second = objA;
        }

        // Add and configure hinge
        Transform existingPivotTransform = first.transform.Find("RotationPivot");

        if (existingPivotTransform != null)
        {
            pivot = existingPivotTransform.gameObject;
        }
        else
        {
            pivot = new GameObject("RotationPivot");
            pivot.transform.SetParent(second.transform, worldPositionStays: true);
        }
        pivot.transform.position = contactPoint;
        Vector3 mainForward = GetPerpendicularVector(snappedAxis).normalized;
        pivot.transform.rotation = Quaternion.LookRotation(mainForward, snappedAxis);

        var rotateTransformer = first.gameObject.GetOrAddComponent<OneGrabRotateTransformer>();
        rotateTransformer.InjectOptionalPivotTransform(pivot.transform);
        
        //targetRb.angularDamping = 10f; 

        if (first.TryGetComponent<Grabbable>(out var grabbable))
        {
            rotateTransformer.Initialize(grabbable);
            grabbable.InjectOptionalOneGrabTransformer(rotateTransformer);
        }
        //////////////////////////////////////////////////////////////////////////
        //Debug.LogError("Reached here");

        ChangeMaterialsAndSpawn(first);

        first.transform.SetParent(second.transform);

        transform.gameObject.SetActive(false);

        Destroy(first.GetComponent<GrabFreeTransformer>());

        collidedObjects.Clear();

    }

    void ChangeMaterialsAndSpawn(GameObject second)
    {
        for (int i = 0; i < AssemblySequence.Count; i++)
        {
            //Debug.LogError("Matched " + AssemblySequence[i] + "Second is " + second);
            if (AssemblySequence[i] != null && AssemblySequence[i] == second)
            {
                // Change "first" object's material to endMaterial
                Renderer rend = AssemblySequence[i].GetComponent<Renderer>();
                //Debug.LogError(rend);

                if (rend != null && endMaterial != null)
                {
                    rend.material = endMaterial;
                }

                // Change next object's material to startMaterial
                int nextIndex = i + 1;
                if (nextIndex < AssemblySequence.Count && AssemblySequence[nextIndex] != null)
                {
                    Renderer nextRend = AssemblySequence[nextIndex].GetComponent<Renderer>();
                    if (nextRend != null && startMaterial != null)
                    {
                        nextRend.material = startMaterial;
                        AssemblySequence[nextIndex].transform.position = spawnPoint.position;
                    }
                }
                break; // Stop after first match
            }
        }
    }

    private Vector3 SnapToMeshNormalAtContactPoint(Collider other)
    {
        RaycastHit hit;

        // Cast a ray from this object's position towards the other object
        Vector3 direction = (other.transform.position - transform.position).normalized;

        if (Physics.Raycast(transform.position, direction, out hit, 2f)) // You can tweak distance
        {
            if (hit.collider == other)
            {
                Debug.Log("Hit normal: " + hit.normal);
                return hit.normal.normalized;
            }
        }

        Debug.LogWarning("Raycast did not hit expected collider. Falling back to object's up vector.");
        return other.transform.up;
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

    IEnumerator AlignAfterDelay(Transform childTransform, Vector3 targetWorldUp)
    {
        yield return new WaitForEndOfFrame(); // Or a small delay like WaitForSeconds(0.05f)
        AlignChildPreserveParent(childTransform, targetWorldUp);
    }

    private void AlignChildPreserveParent(Transform childTransform, Vector3 targetWorldUp)
    {
        // Preserve original scale
        Vector3 originalScale = transform.localScale;

        // Get a reliable forward vector
        Vector3 forward = GetPerpendicularVector(targetWorldUp).normalized;

        // Apply rotation
        //Debug.LogError("Original rotation:" + transform.rotation);
        transform.rotation = Quaternion.LookRotation(forward, targetWorldUp);
        //Debug.LogError("New rotation:" + transform.rotation);

        // Restore scale
        transform.localScale = originalScale;
    }
    
    private Vector3 GetPerpendicularVector(Vector3 normal)
    {
        // Return a vector that's guaranteed to not be colinear with `normal`
        if (Mathf.Abs(Vector3.Dot(normal, Vector3.up)) < 0.99f)
            return Vector3.Cross(normal, Vector3.up);
        else
            return Vector3.Cross(normal, Vector3.right);
    }
}