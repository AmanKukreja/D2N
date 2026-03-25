using UnityEngine;

public class TrashZone : MonoBehaviour
{
    // private Renderer rend;
    // private Color originalColor;
    // public Color toggleColor = Color.green;

    // void Start()
    // {
    //     rend = GetComponent<Renderer>();
    //     originalColor = rend.material.color;
    // }
    // void ResetColor()
    // {
    //     rend.material.color = originalColor;
    // }
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.tag == "ConstraintObject" || collision.tag == "ConstraintPins")
        {
            if (collision.name!="Board")
            {
                Destroy(collision.gameObject);
            } 
        }
        // if (collision.tag == "Player")
        // {
        //     rend.material.color = toggleColor;

        //     Invoke(nameof(ResetColor), 0.2f);
        // }
        // else
        // {
        //     Destroy(collision.gameObject);
        // }
    }
}
