using Unity.VisualScripting;
using UnityEngine;

public class DeleteObjects : MonoBehaviour
{
    public void deleteAllSpawned()
    {
        GameObject[] spawnedObjects = GameObject.FindGameObjectsWithTag("ConstraintObject");

        foreach (GameObject obj in spawnedObjects)
        {
            Debug.LogError("" + obj.name);
            if (obj.name != "Board")
            {
                Destroy(obj);
            }
        }

        GameObject[] spawnedPins = GameObject.FindGameObjectsWithTag("ConstraintPins");

        foreach (GameObject obj in spawnedPins)
        {
            Destroy(obj);
        }
    }

    public void switchOffPins()
    {
        GameObject[] spawnedPins = GameObject.FindGameObjectsWithTag("ConstraintPins");

        foreach (GameObject obj in spawnedPins)
        {
            obj.SetActive(false);
        }
    }
    
    public void switchOnPins()
    {
        GameObject[] spawnedPins = GameObject.FindGameObjectsWithTag("ConstraintPins");

        foreach (GameObject obj in spawnedPins)
        {
            obj.SetActive(true); 
        }
    }
}
