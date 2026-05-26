using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class ChangeSphereColor : MonoBehaviour
{
    private Color originalColor;
    private Renderer rend;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rend = GetComponent<Renderer>();
        originalColor = rend.material.color;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            StartCoroutine(FlashColor(Color.green));
        }
    }
    IEnumerator FlashColor(Color newcolor)
    {
        rend.material.color = newcolor;
        yield return new WaitForSeconds(0.2f);
        rend.material.color = originalColor;
    }
}
