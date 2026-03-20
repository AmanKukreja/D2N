using UnityEngine;

public class ControlParticleOverWing : MonoBehaviour
{
    [Header("Motion")]
    public float speed = 6f;
    public Vector3 initialDirection = Vector3.forward;

    [Header("Surface Attachment")]
    public float surfaceProbeDistance = 0.2f;
    public float surfaceOffset = 0.01f;
    public LayerMask wingLayer;

    [Header("Stability")]
    [Range(0f, 1f)]
    public float reattachStrength = 0.9f;

    private Vector3 velocity;
    private Vector3 surfaceNormal;
    private bool attached;

    void Start()
    {
        velocity = initialDirection.normalized * speed;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        RaycastHit hit;

        // Always probe TOWARD the surface
        if (Physics.Raycast(
            transform.position,
            -transform.up,
            out hit,
            surfaceProbeDistance,
            wingLayer,
            QueryTriggerInteraction.Ignore))
        {
            AttachToSurface(hit);
        }
        else
        {
            attached = false;
        }

        if (attached)
        {
            MoveAlongSurface(dt);
        }
        else
        {
            transform.position += velocity * dt;
        }
    }

    void AttachToSurface(RaycastHit hit)
    {
        Debug.LogError("Attached");
        attached = true;
        surfaceNormal = hit.normal;

        // Lock position slightly above surface
        transform.position = hit.point + surfaceNormal * surfaceOffset;

        // Force velocity to be tangential
        velocity -= Vector3.Dot(velocity, surfaceNormal) * surfaceNormal;
        velocity = velocity.normalized * speed;
    }

    void MoveAlongSurface(float dt)
    {
        // Project velocity onto surface tangent every frame
        Vector3 tangential =
            velocity - Vector3.Dot(velocity, surfaceNormal) * surfaceNormal;

        velocity = Vector3.Lerp(
            velocity,
            tangential.normalized * speed,
            reattachStrength);

        transform.position += velocity * dt;
    }
}



// using UnityEngine;
// using System.Collections.Generic;

// [RequireComponent(typeof(ParticleSystem))]
// public class ControlParticleOverWing : MonoBehaviour
// {
//     private ParticleSystem ps;
//     private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
//     private ParticleSystem.Particle[] particles;

//     void Awake()
//     {
//         ps = GetComponent<ParticleSystem>();
//     }

//     void OnParticleCollision(GameObject other)
//     {
//         // 1. Get collision events
//         int numCollisionEvents = ps.GetCollisionEvents(other, collisionEvents);

//         // 2. Prepare particle array
//         if (particles == null || particles.Length < ps.main.maxParticles)
//             particles = new ParticleSystem.Particle[ps.main.maxParticles];

//         int numParticlesAlive = ps.GetParticles(particles);

//         for (int i = 0; i < numCollisionEvents; i++)
//         {
//             // For every collision, find the particle responsible
//             // Note: This is simplified. For high-precision, 
//             // you'd match the particle's position to the collision event intersection point.
            
//             Vector3 incomingVelo = collisionEvents[i].velocity;
//             Vector3 normal = collisionEvents[i].normal;

//             // Calculate tangential velocity (Vector projection)
//             Vector3 tangential = incomingVelo - Vector3.Project(incomingVelo, normal);
            
//             // We apply this logic to the system's particles
//             // In a simple setup, the collision events often correspond to the 
//             // particles in the order they hit. 
//         }

//         // IMPORTANT: To make particles flow perfectly over a wing, 
//         // it is often better to use the "Force over Lifetime" or "External Forces" 
//         // because manual velocity overrides in OnParticleCollision can be jittery.
//     }
// }