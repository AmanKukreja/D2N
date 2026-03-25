using UnityEngine;

public class FallingSphere : MonoBehaviour
{
    public float resetThreshold = 1f;      // Y value below which the object resets

    public Vector3 resetDirection = Vector3.right;
    private Vector3 originalPosition;        // Stores the starting position

    void Start()
    {
        // Save the starting position of the sphere
        originalPosition = transform.position;
    }

    void Update()
    {
        if (resetDirection == Vector3.down)
        {
            if (transform.localPosition.y < resetThreshold)
            {
                //Debug.LogError(transform.position.y + "and" + resetYThreshold);
                ResetPosition();
            }
        }
        else if (resetDirection == Vector3.right)
        {
            if (transform.localPosition.x > resetThreshold)
            {
                //Debug.LogError(transform.position.y + "and" + resetYThreshold);
                ResetPosition();
            }
        }
    }

    void ResetPosition()
    {
        // Reset position and velocity
        transform.position = originalPosition;

        // Reset velocity if using Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
