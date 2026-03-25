using UnityEngine;
using System.Collections;

public class SingleActingCylinder : MonoBehaviour
{
    public float moveSpeed = 0.05f;       // Extend speed per second
    public float retractMultiplier = 50f; // How much faster retracting is
    public float retractionDelay = 2f;    // Seconds to wait before retracting

    [SerializeField] private float minLocalY = 0f;
    [SerializeField] private float maxLocalY = 0.14f;

    private Coroutine moveRoutine;
    private float lastHitTime;

    void OnParticleCollision(GameObject other)
    {
        ParticleSystem ps = other.GetComponent<ParticleSystem>();
        if (ps == null) return;

        ParticleCollisionEvent[] collisionEvents = new ParticleCollisionEvent[16];
        int count = ps.GetCollisionEvents(gameObject, collisionEvents);

        for (int i = 0; i < count; i++)
        {
            Vector3 contactPoint = collisionEvents[i].intersection;
            float contactY = transform.parent.InverseTransformPoint(contactPoint).y;
            float selfY = transform.localPosition.y;

            if (contactY < selfY)
            {
                lastHitTime = Time.time; // refresh "still colliding" timestamp

                if (moveRoutine == null)
                {
                    // Start movement coroutine if not already running
                    moveRoutine = StartCoroutine(MoveCylinder());
                }
                break;
            }
        }
    }

    private IEnumerator MoveCylinder()
    {
        // Extend until fully extended
        while (transform.localPosition.y < maxLocalY)
        {
            Vector3 localPos = transform.localPosition;
            localPos.y += moveSpeed * Time.deltaTime;
            localPos.y = Mathf.Min(localPos.y, maxLocalY);
            transform.localPosition = localPos;
            yield return null;
        }

        // Wait until no hits for retractionDelay
        while (Time.time - lastHitTime < retractionDelay)
        {
            yield return null;
        }

        // Retract
        while (transform.localPosition.y > minLocalY)
        {
            Vector3 localPos = transform.localPosition;
            localPos.y -= moveSpeed * retractMultiplier * Time.deltaTime;
            localPos.y = Mathf.Max(localPos.y, minLocalY);
            transform.localPosition = localPos;
            yield return null;
        }

        moveRoutine = null; // Finished, ready for next cycle
    }
}

// using UnityEngine;

// public class SingleActingCylinder : MonoBehaviour
// {
//     public float moveSpeed = 0.05f; // Movement speed per second
//     private float movespeednew;
//     public float retractionDelay = 2f; // Seconds to wait before retracting

//     private float retractionTimer = 0f;
//     private bool hitFromBottomThisFrame = false;

//     [SerializeField] private float minLocalY = 0f;
//     [SerializeField] private float maxLocalY = 0.14f;

//     void OnParticleCollision(GameObject other)
//     {
//         ParticleSystem ps = other.GetComponent<ParticleSystem>();
//         if (ps == null) return;

//         ParticleCollisionEvent[] collisionEvents = new ParticleCollisionEvent[16];
//         int count = ps.GetCollisionEvents(gameObject, collisionEvents);

//         // Debug.LogError(count);

//         for (int i = 0; i < count; i++)
//         {
//             Vector3 contactPoint = collisionEvents[i].intersection;
//             float contactY = transform.parent.InverseTransformPoint(contactPoint).y;
//             float selfY = transform.localPosition.y;

//             if (contactY < selfY)
//             {
//                 hitFromBottomThisFrame = true;
//                 retractionTimer = retractionDelay; // Reset the timer
//                 break;
//             }
//         }
//     }

//     void Update()
//     {
//         if (!hitFromBottomThisFrame && transform.localPosition.y==maxLocalY)
//         {
//             retractionTimer -= Time.deltaTime;
//         }

//         // Decide move direction
//         int moveDirection = 0;

//         if (retractionTimer > 0f)
//         {
//             moveDirection = 1; // Move up (extend)
//             movespeednew = moveSpeed;
//         }
//         else
//         {
//             moveDirection = -1; // Move down (retract)
//             movespeednew = moveSpeed * 50;
//         }

//         // Apply movement
//         if (moveDirection != 0)
//         {
//             Vector3 localPos = transform.localPosition;
//             localPos.y += movespeednew * moveDirection * Time.deltaTime;
//             localPos.y = Mathf.Clamp(localPos.y, minLocalY, maxLocalY);
//             transform.localPosition = localPos;
//         }

//         // Reset flag for next frame
//         hitFromBottomThisFrame = false;
//     }
// }
