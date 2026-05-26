// MODIFIED: Renamed the class to better reflect its new role.
using UnityEngine;

// MODIFIED: Removed the [RequireComponent] attributes as this script
// now lives on a parent controller, not the mesh object itself.
public class ProceduralCubeController : MonoBehaviour
{
    [Header("Object References")]
    [Tooltip("The object that will display the generated mesh and have the collider.")]
    [SerializeField] // ADDED: SerializedField to make it assignable in the Inspector
    private MeshFilter targetMeshFilter; // ADDED: Explicit reference to the MeshFilter

    [Tooltip("The collider that will be updated to match the mesh shape.")]
    [SerializeField] // ADDED: SerializedField to make it assignable in the Inspector
    private BoxCollider targetBoxCollider; // ADDED: Explicit reference to the BoxCollider

    [Tooltip("The first corner handle of the cube.")]
    public Transform handleA;

    [Tooltip("The second, opposite corner handle of the cube.")]
    public Transform handleB;

    [Header("Initial State")]
    [Tooltip("The side length of the cube when it's first created, centered on the parent object.")]
    public float initialSideLength = 0.5f;
    
    // Private references
    private Mesh mesh;

    [SerializeField] GameObject label;
    [SerializeField] Vector3 labelOffset = new Vector3(0f, 0.03f, 0f);

    /// <summary>
    /// This is the public entry point for your VR interaction system.
    /// Call this every frame a handle is being moved to update the cube's shape.
    /// </summary>
    public void UpdateFromHandles()
    {
        if (handleA == null || handleB == null) return;

        // This logic remains unchanged as localPosition is relative to the parent,
        // which is now the ProceduralObject_System.
        Vector3 minCorner = Vector3.Min(handleA.localPosition, handleB.localPosition);
        Vector3 maxCorner = Vector3.Max(handleA.localPosition, handleB.localPosition);

        GenerateMesh(minCorner, maxCorner);
    }

    private void Awake()
    {
        // ADDED: Null checks to ensure the new references are assigned in the Inspector.
        if (targetMeshFilter == null || targetBoxCollider == null)
        {
            Debug.LogError("Target MeshFilter or BoxCollider is not assigned in the ProceduralCubeController. Please assign them in the Inspector.", this);
            this.enabled = false; // Disable the script to prevent further errors.
            return;
        }

        // MODIFIED: We no longer get components from this GameObject.
        // We create a new mesh and assign it to our target reference.
        mesh = new Mesh { name = "ProceduralCube_BoundingBox" };
        targetMeshFilter.mesh = mesh;
    }

    // --- MODIFIED: Start() now calls the default Initialize() method ---
    // This ensures that if the prefab is not configured by a spawner script,
    // it will still correctly create a default cube on its own.
    private void Start()
    {
        Initialize();
    }

    void Update()
    {
        if (!handleA.hasChanged && !handleB.hasChanged) return;

        UpdateFromHandles();

        handleA.hasChanged = false;
        handleB.hasChanged = false;

        if (label != null) UpdateLabelPosition();
    }
    
    // --- NEW: Public method to initialize the cube with a specific size and rotation ---
    /// <summary>
    /// Initializes the cube with a specific size and rotation in world space.
    /// This is the primary method to be called from a spawner script.
    /// </summary>
    /// <param name="worldSize">The desired dimensions of the cube.</param>
    /// <param name="worldRotation">The desired orientation of the cube.</param>
    public void Initialize(Vector3 worldSize, Quaternion worldRotation)
    {
        if (handleA == null || handleB == null)
        {
            Debug.LogError("Handles are not assigned to the ProceduralCubeController, cannot set initial state.", this);
            return;
        }

        // Set the parent controller's rotation directly.
        transform.rotation = worldRotation;

        // The size is applied in local space to the handles, as they are children of this object.
        Vector3 halfSize = worldSize / 2.0f;
        handleA.localPosition = -halfSize;
        handleB.localPosition = halfSize;

        // Generate the mesh and collider with the new dimensions.
        UpdateFromHandles();
    }

    // --- NEW: Public method to initialize the cube with default values ---
    /// <summary>
    /// Initializes the cube with a default size (using 'initialSideLength') centered on the object.
    /// This is called by Start() as a fallback.
    /// </summary>
    public void Initialize()
    {
        if (handleA == null || handleB == null)
        {
            Debug.LogError("Handles are not assigned to the ProceduralCubeController, cannot set initial state.", this);
            return;
        }

        float halfLength = initialSideLength / 2.0f;
        Vector3 minCorner = new Vector3(-halfLength, -halfLength, -halfLength);
        Vector3 maxCorner = new Vector3(halfLength, halfLength, halfLength);

        handleA.localPosition = minCorner;
        handleB.localPosition = maxCorner;
        
        // Generate the mesh and collider.
        UpdateFromHandles();
    }

    // --- REMOVED: The old 'InitializeCubeState()' method is no longer needed ---

    private void UpdateLabelPosition()
    {
        label.transform.localPosition = handleB.transform.localPosition + labelOffset;
    }

    private void GenerateMesh(Vector3 min, Vector3 max)
    {
        if (mesh == null) return;

        mesh.Clear();

        // --- All the mesh generation logic below this line is IDENTICAL to before ---

        Vector3 p0 = new Vector3(min.x, min.y, min.z);
        Vector3 p1 = new Vector3(max.x, min.y, min.z);
        Vector3 p2 = new Vector3(max.x, min.y, max.z);
        Vector3 p3 = new Vector3(min.x, min.y, max.z);

        Vector3 p4 = new Vector3(min.x, max.y, min.z);
        Vector3 p5 = new Vector3(max.x, max.y, min.z);
        Vector3 p6 = new Vector3(max.x, max.y, max.z);
        Vector3 p7 = new Vector3(min.x, max.y, max.z);

        var vertices = new Vector3[]
        {
            p0, p1, p2, p3, p7, p6, p5, p4,
            p4, p5, p1, p0, p3, p2, p6, p7,
            p0, p3, p7, p4, p1, p5, p6, p2
        };

        var normals = new Vector3[]
        {
            Vector3.down, Vector3.down, Vector3.down, Vector3.down,
            Vector3.up, Vector3.up, Vector3.up, Vector3.up,
            Vector3.back, Vector3.back, Vector3.back, Vector3.back,
            Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
            Vector3.left, Vector3.left, Vector3.left, Vector3.left,
            Vector3.right, Vector3.right, Vector3.right, Vector3.right
        };

        var uvs = new Vector2[24];
        for (int i = 0; i < 6; i++)
        {
            uvs[i * 4 + 0] = new Vector2(0, 0); uvs[i * 4 + 1] = new Vector2(1, 0);
            uvs[i * 4 + 2] = new Vector2(1, 1); uvs[i * 4 + 3] = new Vector2(0, 1);
        }

        var triangles = new int[36];
        for (int i = 0; i < 6; i++)
        {
            int baseIndex = i * 4;
            int triIndex = i * 6;
            triangles[triIndex + 0] = baseIndex + 0; triangles[triIndex + 1] = baseIndex + 1; triangles[triIndex + 2] = baseIndex + 2;
            triangles[triIndex + 3] = baseIndex + 0; triangles[triIndex + 4] = baseIndex + 2; triangles[triIndex + 5] = baseIndex + 3;
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateBounds();
        
        // MODIFIED: These lines now update the target BoxCollider reference.
        targetBoxCollider.size = max - min;// - new Vector3(0.1f, 0.1f, 0.1f);
        targetBoxCollider.size = new Vector3(
        Mathf.Max(0.1f, targetBoxCollider.size.x),
        Mathf.Max(0.1f, targetBoxCollider.size.y),
        Mathf.Max(0.1f, targetBoxCollider.size.z)
        );
        targetBoxCollider.center = (min + max) / 2.0f;
    }
}