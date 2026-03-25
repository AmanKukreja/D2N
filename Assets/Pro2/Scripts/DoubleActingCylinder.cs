using UnityEngine;
using System.Collections;

public class DoubleActingCylinder : MonoBehaviour
{
    public float moveSpeed = 0.05f;   // Movement speed per second
    public float stopDelay = 4.0f;    // Grace time before stopping when hits stop

    [SerializeField] private float minLocalY = 0f;
    [SerializeField] private float maxLocalY = 0.14f;

    private Coroutine moveRoutine;
    private int moveDirection = 0;    // -1 = down, 1 = up, 0 = stop
    private float lastHitTime;        // last time a valid hit was received

    void OnParticleCollision(GameObject other)
    {
        ParticleSystem ps = other.GetComponent<ParticleSystem>();
        if (ps == null) return;

        ParticleCollisionEvent[] collisionEvents = new ParticleCollisionEvent[16];
        int count = ps.GetCollisionEvents(gameObject, collisionEvents);

        bool hitTop = false;
        bool hitBottom = false;

        for (int i = 0; i < count; i++)
        {
            Vector3 contactPoint = collisionEvents[i].intersection;
            float contactY = transform.parent.InverseTransformPoint(contactPoint).y;
            float selfY = transform.localPosition.y;

            if (contactY > selfY)
                hitTop = true;
            else if (contactY < selfY)
                hitBottom = true;
        }

        int newDirection = 0;
        if (hitTop && hitBottom)
        {
            newDirection = 0; // cancel out
        }
        else if (hitTop)
        {
            newDirection = -1; // move down
        }
        else if (hitBottom)
        {
            newDirection = 1; // move up
        }

        if (newDirection != 0)
        {
            // ✅ immediately switch if opposite hit arrives
            moveDirection = newDirection;
            lastHitTime = Time.time;

            if (moveRoutine == null)
                moveRoutine = StartCoroutine(MoveCylinder());
        }
    }

    private IEnumerator MoveCylinder()
    {
        while (true)
        {
            // stop if no hits for "stopDelay"
            if (Time.time - lastHitTime > stopDelay)
                break;

            if (moveDirection != 0)
            {
                Vector3 localPos = transform.localPosition;
                localPos.y += moveSpeed * moveDirection * Time.deltaTime;
                localPos.y = Mathf.Clamp(localPos.y, minLocalY, maxLocalY);
                transform.localPosition = localPos;
            }

            yield return null;
        }

        moveRoutine = null;
        moveDirection = 0; // fully stop
    }
}



// using UnityEngine;

// public class DoubleActingCylinder : MonoBehaviour
// {
//     public float moveSpeed = 0.05f; // Movement speed per second

//     private int moveDirection = 0; // -1 = down, 1 = up, 0 = stop

//     private bool hitFromTopThisFrame = false;
//     private bool hitFromBottomThisFrame = false;
//     private int hitcountbottom=10;
//     private int hitcounttop=10;

//     [SerializeField] private float minLocalY = 0f;
//     [SerializeField] private float maxLocalY = 0.14f;

//     void OnParticleCollision(GameObject other)
//     {
//         //Debug.LogError("COLLISION DETECTED with " + other.name);
//         ParticleSystem ps = other.GetComponent<ParticleSystem>();
//         if (ps == null) return;

//         ParticleCollisionEvent[] collisionEvents = new ParticleCollisionEvent[16];
//         int count = ps.GetCollisionEvents(gameObject, collisionEvents);

//         //Debug.LogError(count);

//         for (int i = 0; i < count; i++)
//         {
//             Vector3 contactPoint = collisionEvents[i].intersection;
//             float contactY = transform.parent.InverseTransformPoint(contactPoint).y;
//             float selfY = transform.localPosition.y;

//             if (contactY > selfY)
//             {
//                 hitFromTopThisFrame = true;
//                 hitcounttop=10;
//                 hitcountbottom=hitcountbottom-1;
//                 if (hitcountbottom<0)
//                 {
//                     hitFromBottomThisFrame = false;    
//                 }
//             }
//             else if (contactY < selfY)
//             {
//                 hitFromBottomThisFrame = true;
//                 hitcountbottom=10;
//                 hitcounttop=hitcounttop-1;
//                 if (hitcounttop<0)
//                 {
//                     hitFromTopThisFrame = false;    
//                 }

//             }
//         }
//     }

//     void Update()
//     {
//         // Update move direction based on this frame's collisions
//         if (hitFromTopThisFrame && hitFromBottomThisFrame)
//         {
//             moveDirection = 0; // Cancel movement if both sides hit
//         }
//         else if (hitFromTopThisFrame)
//         {
//             moveDirection = -1; // Move down
//         }
//         else if (hitFromBottomThisFrame)
//         {
//             moveDirection = 1; // Move up
//         }

//         //Debug.LogError(moveDirection);

//         // Apply movement
//         if (moveDirection != 0)
//         {
//             Vector3 localPos = transform.localPosition;
//             localPos.y += moveSpeed * moveDirection * Time.deltaTime;
//             localPos.y = Mathf.Clamp(localPos.y, minLocalY, maxLocalY);
//             transform.localPosition = localPos;
//         }

//         // Reset hit flags for the next frame
//         // hitFromTopThisFrame = false;
//         // hitFromBottomThisFrame = false;
//     }
// }
