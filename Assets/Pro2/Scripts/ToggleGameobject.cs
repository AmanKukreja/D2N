using UnityEngine;

public class ToggleGameObject : MonoBehaviour
{
    // Reference to the GameObject you want to toggle
    public GameObject targetObject;

    // Function to toggle the object
    public void Toggle()
    {
        if (targetObject != null)
        {
            // Invert the active state
            targetObject.SetActive(!targetObject.activeSelf);
        }
    }

    public void ToggleProperties()
    {
        Rigidbody rb = targetObject.GetComponent<Rigidbody>();
        rb.isKinematic = !rb.isKinematic;
    }

    public void ToggleMeshRenderer()
    {
        if (targetObject != null)
        {
            MeshRenderer mr = targetObject.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.enabled = !mr.enabled;
                Debug.Log("Reached inside toggle");
            }
        }
    }

}
