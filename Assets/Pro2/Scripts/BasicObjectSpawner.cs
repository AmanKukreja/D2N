using UnityEngine;

public class BasicObjectSpawner : MonoBehaviour
{
    [Tooltip("Drag the prefab you want to spawn here")]
    public GameObject prefabToSpawn;

    [Tooltip("Optional: set this in Inspector for custom spawn location and rotation")]
    public Transform spawnPoint;

    public void Spawn()
    {
        if (prefabToSpawn != null)
        {
            // Use spawnPoint position & rotation if assigned, otherwise use this object's transform
            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
            Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

            GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);

            spawnedObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("No prefab assigned to spawn!");
        }
    }
}
