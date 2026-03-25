using UnityEngine;

public class TeleportNewLocation : MonoBehaviour
{

    public Transform playerRig;

    public Transform[] teleportLocations;

    // Teleports the player to a random location from the list.
    public void TeleportPlayer()
    {
        if (teleportLocations.Length > 0)
        {
            // Pick a random location from the array.
            int randomIndex = Random.Range(0, teleportLocations.Length);
            Transform targetLocation = teleportLocations[randomIndex];
            
            // Set the new position for the entire player rig.
            if (playerRig != null)
            {
                playerRig.position = targetLocation.position;
                Debug.Log("Teleporting player to new location!");
            }
            else
            {
                Debug.LogWarning("Player Rig is not assigned. Please assign the OVRCameraRig in the Inspector.");
            }
        }
        else
        {
            Debug.LogWarning("Teleport locations array is empty. Please assign locations in the Inspector.");
        }
    }
}
