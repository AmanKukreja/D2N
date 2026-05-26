using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class MoveObject : MonoBehaviour
{
    public Vector3 moveDirection = Vector3.right; // Direction to move
    public float moveSpeed = 5f;                     // Speed of movement
    public float centerThresholdX = 0.2f;           // Max X distance from center to count as "correct" hit

    private Color originalColor;
    public Transform resetPosition;
    private Renderer rend;

    private bool flag = false;

    void Start()
    {
        rend = GetComponent<Renderer>();

        originalColor = rend.material.color;

        // Ensure the collider is a trigger
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
            col.isTrigger = true;

        StartCoroutine(Resetting());
    }

    IEnumerator Resetting()
    {
        yield return new WaitForSeconds(0.1f);
        ResetCube();
    }

    void Update()
    {
        if (flag == true)
        {
            transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("hammer"))
        {
            flag = true;
            // Get hammer center in local space of this object
            Vector3 hammerLocalCenter = transform.InverseTransformPoint(other.transform.position);

            Debug.LogError("hit at " + hammerLocalCenter.x);

            // Check only X-axis distance from center
            if (Mathf.Abs(hammerLocalCenter.x) <= centerThresholdX)
            {
                // Correct hit (green)
                StartCoroutine(FlashColor(Color.green));
            }
            else
            {
                // Wrong hit (red)
                StartCoroutine(FlashColor(Color.red));
            }
        }
    }

    IEnumerator FlashColor(Color newColor)
    {
        rend.material.color = newColor;
        yield return new WaitForSeconds(1f);
        rend.material.color = originalColor;
    }

    public void ResetCube()
    {
        transform.position = resetPosition.position;
        flag = false;
    } 
}
