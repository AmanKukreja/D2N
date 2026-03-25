using UnityEngine;

public class spotlightetector : MonoBehaviour
{
    public float range = 10f;

    
    private Light spotlight;

    void Start()
    {
        spotlight = GetComponent<Light>();
        if (spotlight.type != LightType.Spot)
        {
            Debug.LogWarning("This script is intended for a Spot Light!");
        }
    }


    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, range))
        {
            spotlight.color = Color.green;
            if (hit.collider.CompareTag("Target"))
            {
                TargetManager.Instance.HandleHit(hit.collider.gameObject, Time.deltaTime);
            }
        }
        else
        {
            Physics.Raycast(ray, out hit);
            float distance = hit.distance/2; // Normalize distance (0 = near, 1 = far)
            //Debug.LogError("" + distance + "");
            spotlight.color = Color.Lerp(Color.yellow, Color.red, distance); 
            //spotlight.color = Color.red;
        }
    }
}
