using UnityEngine;
using System.Collections.Generic;

// MODIFIED: Renamed the class to reflect its controller role.
// MODIFIED: Removed [RequireComponent] attributes.
public class ProceduralCylinderController : MonoBehaviour
{
    [Header("Object References")]
    [Tooltip("The object that will display the generated mesh and have the collider.")]
    [SerializeField]
    private MeshFilter targetMeshFilter; // ADDED: Explicit reference

    [Tooltip("The collider that will be updated to match the mesh shape.")]
    [SerializeField]
    private CapsuleCollider targetCapsuleCollider; // ADDED: Explicit reference

    [Tooltip("The handle for the top end of the cylinder.")]
    public Transform topHandle;

    [Tooltip("The handle for the base of the cylinder.")]
    public Transform baseHandle;

    [Header("Mesh Settings")]
    [Tooltip("How many segments to use to build the cylinder. More segments = smoother circle.")]
    [Range(3, 64)] public int radialSegments = 24;

    [Header("Initial State")]
    [Tooltip("The default height of the cylinder if not initialized by a script.")]
    public float initialHeight = 1f;
    [Tooltip("The default radius of the cylinder if not initialized by a script.")]
    public float initialRadius = 0.5f;

    private Mesh mesh;
    
    /// <summary>
    /// The public entry point for updating the cylinder's shape based on handle positions.
    /// </summary>
    public void UpdateFromHandles()
    {
        if (topHandle == null || baseHandle == null) return;

        // MODIFIED: Simplified logic. We read all state from the handles first.
        // We can use either handle to determine the radius; by convention, we'll use the top one.
        Vector3 topLocalPos = topHandle.localPosition;
        Vector3 baseLocalPos = baseHandle.localPosition;

        // The radius is the handle's distance from the central Y-axis.
        float currentRadius = new Vector2(topLocalPos.x, topLocalPos.z).magnitude;

        // The height of each end is simply its Y position.
        float currentTopY = topLocalPos.y;
        float currentBaseY = baseLocalPos.y;

        // Generate the mesh with these new parameters.
        GenerateMesh(currentRadius, currentTopY, currentBaseY);

        // Snap both handles to be consistent with the new radius, maintaining their heights.
        // This ensures if one handle is pulled out, the other follows.
        UpdateHandlePositions(currentRadius, currentTopY, currentBaseY);
    }

    private void Awake()
    {
        // ADDED: Null checks for our new target references.
        if (targetMeshFilter == null || targetCapsuleCollider == null)
        {
            Debug.LogError("Target MeshFilter or CapsuleCollider is not assigned. Please assign them in the Inspector.", this);
            this.enabled = false;
            return;
        }

        // MODIFIED: Create the mesh and assign it to our target.
        mesh = new Mesh { name = "ProceduralCylinder" };
        targetMeshFilter.mesh = mesh;

        // MODIFIED: Ensure the target collider is set to the correct orientation.
        targetCapsuleCollider.direction = 1; // 1 = Y-Axis
    }

    // --- MODIFIED: Start() now calls the default Initialize() method ---
    // This ensures the object will create a default cylinder on its own
    // if not configured by an external script.
    private void Start()
    {
        Initialize();
    }

    // MODIFIED: Simplified Update loop.
    void Update()
    {
        if (topHandle == null || baseHandle == null) return;
        
        if (topHandle.hasChanged || baseHandle.hasChanged)
        {
            UpdateFromHandles();
            topHandle.hasChanged = false;
            baseHandle.hasChanged = false;
        }
    }
    
    // --- NEW: Public method to initialize with a specific size and rotation ---
    /// <summary>
    /// Initializes the cylinder with a specific length, radius, and rotation.
    /// To be called from an external script after instantiating the prefab.
    /// </summary>
    /// <param name="length">The desired total height of the cylinder.</param>
    /// <param name="radius">The desired radius of the cylinder.</param>
    /// <param name="worldRotation">The desired world-space orientation.</param>
    public void Initialize(float length, float radius, Quaternion worldRotation)
    {
        if (topHandle == null || baseHandle == null)
        {
            Debug.LogError("Handles are not assigned, cannot set initial state.", this);
            return;
        }

        // Set the parent controller's rotation
        transform.rotation = worldRotation;

        // Set handle positions based on length and radius
        float topY = length / 2f;
        float baseY = -length / 2f;
        UpdateHandlePositions(radius, topY, baseY);

        // Generate the mesh and collider to match the new state
        UpdateFromHandles();
    }

    // --- NEW: Public method to initialize with default inspector values ---
    /// <summary>
    /// Initializes the cylinder with the default values set in the Inspector.
    /// This is called automatically by Start() as a fallback.
    /// </summary>
    public void Initialize()
    {
        if (topHandle == null || baseHandle == null)
        {
            Debug.LogError("Handles are not assigned, cannot set initial state.", this);
            return;
        }
        
        // Set handle positions using the initial values
        float topY = initialHeight / 2f;
        float baseY = -initialHeight / 2f;
        UpdateHandlePositions(initialRadius, topY, baseY);
        
        // Generate the initial mesh and collider
        UpdateFromHandles();
    }

    // --- REMOVED: The old 'InitializeCylinderState()' method is no longer needed. ---

    // MODIFIED: This method now takes the state as parameters for clarity.
    private void UpdateHandlePositions(float radius, float topY, float baseY)
    {
        if (topHandle != null)
        {
            // Keep the handle's current rotation from the center, but enforce the radius.
            Vector3 topDir = new Vector3(topHandle.localPosition.x, 0, topHandle.localPosition.z).normalized;
            if (topDir == Vector3.zero) topDir = Vector3.right; // Handle default case if at center
            topHandle.localPosition = topDir * radius + Vector3.up * topY;
        }
        if (baseHandle != null)
        {
            Vector3 baseDir = new Vector3(baseHandle.localPosition.x, 0, baseHandle.localPosition.z).normalized;
            if (baseDir == Vector3.zero) baseDir = Vector3.right;
            baseHandle.localPosition = baseDir * radius + Vector3.up * baseY;
        }
    }
    
    // MODIFIED: This method now takes the state as parameters.
    private void GenerateMesh(float radius, float topY, float baseY)
    {
        if (mesh == null) return;
        mesh.Clear();

        float cylinderRadius = Mathf.Max(0.01f, radius); // Ensure radius is not zero.

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        float actualTopY = Mathf.Max(topY, baseY);
        float actualBottomY = Mathf.Min(topY, baseY);

        // --- All mesh generation logic below is identical to your original script ---
        // --- except it uses the parameters passed into this method. ---

        // Wall
        int wallVertexStartIndex = vertices.Count;
        for (int i = 0; i <= radialSegments; i++)
        {
            float t = (float)i / radialSegments;
            float angle = t * Mathf.PI * 2f;
            var unitPos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            vertices.Add(unitPos * cylinderRadius + Vector3.up * actualBottomY);
            vertices.Add(unitPos * cylinderRadius + Vector3.up * actualTopY);
            normals.Add(unitPos); normals.Add(unitPos);
            uvs.Add(new Vector2(t, 0)); uvs.Add(new Vector2(t, 1));
        }
        for (int i = 0; i < radialSegments; i++)
        {
            int base_i = wallVertexStartIndex + i * 2;
            triangles.Add(base_i); triangles.Add(base_i + 1); triangles.Add(base_i + 3);
            triangles.Add(base_i); triangles.Add(base_i + 3); triangles.Add(base_i + 2);
        }

        // Top Cap
        int topCapStartIndex = vertices.Count;
        vertices.Add(new Vector3(0, actualTopY, 0));
        normals.Add(Vector3.up); uvs.Add(new Vector2(0.5f, 0.5f));
        for (int i = 0; i <= radialSegments; i++)
        {
            float t = (float)i / radialSegments;
            float angle = t * Mathf.PI * 2f;
            var unitPos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            vertices.Add(unitPos * cylinderRadius + Vector3.up * actualTopY);
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(unitPos.x * 0.5f + 0.5f, unitPos.z * 0.5f + 0.5f));
        }
        for (int i = 0; i < radialSegments; i++)
        {
            triangles.Add(topCapStartIndex);
            triangles.Add(topCapStartIndex + i + 2);
            triangles.Add(topCapStartIndex + i + 1);
        }

        // Bottom Cap
        int bottomCapStartIndex = vertices.Count;
        vertices.Add(new Vector3(0, actualBottomY, 0));
        normals.Add(Vector3.down); uvs.Add(new Vector2(0.5f, 0.5f));
        for (int i = 0; i <= radialSegments; i++)
        {
            float t = (float)i / radialSegments;
            float angle = t * Mathf.PI * 2f;
            var unitPos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            vertices.Add(unitPos * cylinderRadius + Vector3.up * actualBottomY);
            normals.Add(Vector3.down);
            uvs.Add(new Vector2(unitPos.x * 0.5f + 0.5f, unitPos.z * 0.5f + 0.5f));
        }
        for (int i = 0; i < radialSegments; i++)
        {
            triangles.Add(bottomCapStartIndex);
            triangles.Add(bottomCapStartIndex + i + 1);
            triangles.Add(bottomCapStartIndex + i + 2);
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        
        // MODIFIED: Update the target collider reference.
        targetCapsuleCollider.radius = cylinderRadius;
        targetCapsuleCollider.height = Mathf.Max(0.01f, actualTopY - actualBottomY); // Ensure non-zero height
        targetCapsuleCollider.center = new Vector3(0, (actualTopY + actualBottomY) / 2.0f, 0);
    }
}