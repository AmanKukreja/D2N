using UnityEngine;

public class SyncPosition : MonoBehaviour
{
    // The transform of the object to follow.
    public Transform objectToFollow;

    // The calculated local offset.
    private Vector3 localPositionOffset;
    private Quaternion localRotationOffset;

    void Start()
    {
        if (objectToFollow == null)
        {
            Debug.LogError("The 'objectToFollow' Transform has not been assigned!");
            enabled = false;
            return;
        }

        // Calculate the initial local offsets.
        // This is the position and rotation of this object relative to the object it's following.
        localPositionOffset = objectToFollow.InverseTransformPoint(transform.position);
        localRotationOffset = Quaternion.Inverse(objectToFollow.rotation) * transform.rotation;
    }

    void Update()
    {
        // Apply the local offsets to synchronize position and rotation.
        transform.position = objectToFollow.TransformPoint(localPositionOffset);
        transform.rotation = objectToFollow.rotation * localRotationOffset;
    }
}