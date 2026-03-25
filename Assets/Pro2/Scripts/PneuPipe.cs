using UnityEngine;
using System.Collections.Generic;

public class PneuPipe : MonoBehaviour
{
    [Header("Object References")]
    [Tooltip("The object that will display the generated mesh.")]
    [SerializeField] private MeshFilter targetMeshFilter;

    [Tooltip("The collider that will be updated. Uses MeshCollider.")]
    [SerializeField] private MeshCollider targetMeshCollider;

    [Header("Handles (only outer handles now)")]
    public Transform topOuterHandle;
    public Transform bottomOuterHandle;

    [Header("Mesh Settings")]
    [Range(3, 64)] public int radialSegments = 24;

    [Header("Initial State")]
    public float initialLength = 2f;
    public float initialOuterRadius = 0.5f;
    public float initialInnerRadius = 0.4f;

    private float _currentTopY;
    private float _currentTopX;
    private float _currentBottomY;
    private float _currentBottomX;
    private float _currentOuterRadius;
    private float _currentInnerRadius;

    private Mesh mesh;
    private Transform _lastMovedHandle = null;

    private void Awake()
    {
        if (targetMeshFilter == null || targetMeshCollider == null ||
            topOuterHandle == null || bottomOuterHandle == null)
        {
            Debug.LogError("A required reference on PneuPipe is not assigned. Please check all references in the Inspector.", this);
            this.enabled = false;
            return;
        }

        mesh = new Mesh { name = "ProceduralTube" };
        targetMeshFilter.mesh = mesh;

        targetMeshCollider.sharedMesh = null;
        targetMeshCollider.convex = false; // required if Rigidbody is attached
    }

    private void Start()
    {
        Initialize();
    }

    void Update()
    {
        Transform activeHandleThisFrame = null;

        if (topOuterHandle.hasChanged) activeHandleThisFrame = topOuterHandle;
        else if (bottomOuterHandle.hasChanged) activeHandleThisFrame = bottomOuterHandle;

        if (activeHandleThisFrame == null && _lastMovedHandle != null)
        {
            SyncHandlePositions();
        }

        if (activeHandleThisFrame != null)
        {
            Vector3 handlePos = activeHandleThisFrame.localPosition;
            float potentialRadius = new Vector2(handlePos.x, handlePos.z).magnitude;

            if (activeHandleThisFrame == topOuterHandle)
            {
                _currentTopY = handlePos.y;
                _currentTopX = handlePos.x;
                _currentOuterRadius = potentialRadius;
            }
            else // bottom
            {
                _currentBottomY = handlePos.y;
                _currentBottomX = handlePos.x;
                _currentOuterRadius = potentialRadius;
            }

            SyncHandlePositions();
            GenerateMesh();
        }

        _lastMovedHandle = activeHandleThisFrame;
        if (activeHandleThisFrame != null)
        {
            topOuterHandle.hasChanged = false;
            bottomOuterHandle.hasChanged = false;
        }
    }

    public void Initialize()
    {
        _currentTopY = initialLength / 2f;
        _currentBottomY = -initialLength / 2f;
        _currentTopX = 0f;
        _currentBottomX = 0f;
        _currentOuterRadius = initialOuterRadius;
        _currentInnerRadius = initialInnerRadius;

        SyncHandlePositions();
        GenerateMesh();
    }

    private void SyncHandlePositions()
    {
        topOuterHandle.localPosition = Vector3.right * _currentTopX + Vector3.forward * 0.08f + Vector3.up * _currentTopY;
        bottomOuterHandle.localPosition = Vector3.right * _currentBottomX + Vector3.forward * 0.08f + Vector3.up * _currentBottomY;
    }

    private void GenerateMesh()
    {
        if (mesh == null) return;
        mesh.Clear();

        float actualTopY = Mathf.Max(_currentTopY, _currentBottomY);
        float actualTopX = _currentTopX;
        float actualBottomY = Mathf.Min(_currentTopY, _currentBottomY);
        float actualBottomX = _currentBottomX;

        float outerRadius = initialOuterRadius; //_currentOuterRadius;
        float innerRadius = initialInnerRadius;  //_currentInnerRadius;

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        // Tube walls
        for (int i = 0; i <= radialSegments; i++)
        {
            float t = (float)i / radialSegments;
            float angle = t * Mathf.PI * 2f;
            var unitCirclePos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));

            // Bottom ring
            vertices.Add(Vector3.right * actualBottomX + unitCirclePos * outerRadius + Vector3.up * actualBottomY);
            // Top ring
            vertices.Add(Vector3.right * actualTopX + unitCirclePos * outerRadius + Vector3.up * actualTopY);

            // Inner bottom ring
            vertices.Add(Vector3.right * actualBottomX + unitCirclePos * innerRadius + Vector3.up * actualBottomY);
            // Inner top ring
            vertices.Add(Vector3.right * actualTopX + unitCirclePos * innerRadius + Vector3.up * actualTopY);

            normals.Add(unitCirclePos); normals.Add(unitCirclePos);
            normals.Add(-unitCirclePos); normals.Add(-unitCirclePos);
            uvs.Add(new Vector2(t, 0)); uvs.Add(new Vector2(t, 1));
            uvs.Add(new Vector2(t, 0)); uvs.Add(new Vector2(t, 1));
        }

        for (int i = 0; i < radialSegments; i++)
        {
            int b = i * 4;
            // outer wall
            triangles.Add(b); triangles.Add(b + 1); triangles.Add(b + 5);
            triangles.Add(b); triangles.Add(b + 5); triangles.Add(b + 4);
            // inner wall
            triangles.Add(b + 2); triangles.Add(b + 7); triangles.Add(b + 3);
            triangles.Add(b + 2); triangles.Add(b + 6); triangles.Add(b + 7);
        }

        // Caps
        int capStartIndex = vertices.Count;
        for (int i = 0; i <= radialSegments; i++)
        {
            float t = (float)i / radialSegments;
            float angle = t * Mathf.PI * 2f;
            var unitCirclePos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));

            // Top cap
            vertices.Add(Vector3.right * actualTopX + unitCirclePos * outerRadius + Vector3.up * actualTopY);
            vertices.Add(Vector3.right * actualTopX + unitCirclePos * innerRadius + Vector3.up * actualTopY);

            // Bottom cap
            vertices.Add(Vector3.right * actualBottomX + unitCirclePos * outerRadius + Vector3.up * actualBottomY);
            vertices.Add(Vector3.right * actualBottomX + unitCirclePos * innerRadius + Vector3.up * actualBottomY);

            normals.Add(Vector3.up); normals.Add(Vector3.up);
            normals.Add(Vector3.down); normals.Add(Vector3.down);

            uvs.Add(unitCirclePos.xyc()); uvs.Add(unitCirclePos.xyc() * (innerRadius / outerRadius));
            uvs.Add(unitCirclePos.xyc()); uvs.Add(unitCirclePos.xyc() * (innerRadius / outerRadius));
        }

        for (int i = 0; i < radialSegments; i++)
        {
            int b = capStartIndex + i * 4;
            // top cap
            triangles.Add(b); triangles.Add(b + 1); triangles.Add(b + 5);
            triangles.Add(b); triangles.Add(b + 5); triangles.Add(b + 4);
            // bottom cap
            triangles.Add(b + 2); triangles.Add(b + 7); triangles.Add(b + 3);
            triangles.Add(b + 2); triangles.Add(b + 6); triangles.Add(b + 7);
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();

        if (targetMeshCollider != null)
        {
            targetMeshCollider.sharedMesh = null; // force update
            targetMeshCollider.sharedMesh = mesh;
        }
    }
}

public static class Vector3ExtensionsNew
{
    public static Vector2 xyc(this Vector3 v)
    {
        return new Vector2(v.x, v.y);
    }
}



// using UnityEngine;
// using System.Collections.Generic;

// public class PneuPipe : MonoBehaviour
// {
//     [Header("Object References")]
//     [Tooltip("The object that will display the generated mesh.")]
//     [SerializeField]
//     private MeshFilter targetMeshFilter;

//     [Tooltip("The collider that will be updated. Now uses a MeshCollider.")]
//     [SerializeField]
//     private MeshCollider targetMeshCollider;

//     [Header("Handles")]
//     public Transform topOuterHandle;
//     public Transform topInnerHandle;
//     public Transform bottomOuterHandle;
//     public Transform bottomInnerHandle;
    
//     [Tooltip("The angular separation between inner and outer handles.")]
//     [Range(0f, 90f)] public float handleAngleOffset = 30f;
    
//     [Header("Mesh Settings")]
//     [Range(3, 64)] public int radialSegments = 24;

//     [Header("Interaction Constraints")]
//     public float minWallThickness = 0.02f;

//     [Header("Initial State")]
//     public float initialLength = 2f;
//     public float initialOuterRadius = 0.5f;
//     public float initialInnerRadius = 0.4f;

//     private float _currentTopY;
//     private float _currentTopX;
//     private float _currentBottomY;
//     private float _currentBottomX;
//     private float _currentOuterRadius;
//     private float _currentInnerRadius;

//     private Mesh mesh;
//     private Transform _lastMovedHandle = null;

//     private void Awake()
//     {
//         if (targetMeshFilter == null || targetMeshCollider == null ||
//             topOuterHandle == null || topInnerHandle == null ||
//             bottomOuterHandle == null || bottomInnerHandle == null)
//         {
//             Debug.LogError("A required reference on PneuPipe is not assigned. Please check all references in the Inspector.", this);
//             this.enabled = false;
//             return;
//         }

//         mesh = new Mesh { name = "ProceduralTube" };
//         targetMeshFilter.mesh = mesh;

//         // Prepare mesh collider
//         targetMeshCollider.sharedMesh = null;
//         targetMeshCollider.convex = false; // required if used with a Rigidbody
//     }

//     private void Start()
//     {
//         Initialize();
//     }

//     void Update()
//     {
//         Transform activeHandleThisFrame = null;
        
//         if (topOuterHandle.hasChanged) activeHandleThisFrame = topOuterHandle;
//         else if (topInnerHandle.hasChanged) activeHandleThisFrame = topInnerHandle;
//         else if (bottomOuterHandle.hasChanged) activeHandleThisFrame = bottomOuterHandle;
//         else if (bottomInnerHandle.hasChanged) activeHandleThisFrame = bottomInnerHandle;

//         if (activeHandleThisFrame == null && _lastMovedHandle != null)
//         {
//             SyncAllHandlePositions();
//         }

//         if (activeHandleThisFrame != null)
//         {
//             Vector3 handlePos = activeHandleThisFrame.localPosition;
//             float potentialRadius = new Vector2(handlePos.x, handlePos.z).magnitude;

//             if (activeHandleThisFrame == topOuterHandle || activeHandleThisFrame == bottomOuterHandle)
//             {
//                 if (potentialRadius >= _currentInnerRadius + minWallThickness) 
//                     _currentOuterRadius = potentialRadius;
//             }
//             else
//             {
//                 if (potentialRadius <= _currentOuterRadius - minWallThickness && potentialRadius >= 0.01f) 
//                     _currentInnerRadius = potentialRadius;
//             }
            
//             if (activeHandleThisFrame == topOuterHandle || activeHandleThisFrame == topInnerHandle) 
//             { 
//                 _currentTopY = handlePos.y; 
//                 _currentTopX = handlePos.x; 
//             }
//             else 
//             { 
//                 _currentBottomY = handlePos.y; 
//                 _currentBottomX = handlePos.x; 
//             }

//             SyncPassiveHandles(activeHandleThisFrame);
//             GenerateMesh();
//         }

//         _lastMovedHandle = activeHandleThisFrame;
//         if(activeHandleThisFrame != null)
//         {
//             topOuterHandle.hasChanged = false;
//             topInnerHandle.hasChanged = false;
//             bottomOuterHandle.hasChanged = false;
//             bottomInnerHandle.hasChanged = false;
//         }
//     }
    
//     public void Initialize()
//     {
//         _currentTopY = initialLength / 2f;
//         _currentBottomY = -initialLength / 2f;
//         _currentOuterRadius = Mathf.Max(initialOuterRadius, initialInnerRadius + minWallThickness);
//         _currentInnerRadius = Mathf.Max(0.01f, Mathf.Min(initialInnerRadius, _currentOuterRadius - minWallThickness));

//         SyncAllHandlePositions();
//         GenerateMesh();
//     }

//     private void SyncAllHandlePositions()
//     {
//         Vector3 outerDirection = Vector3.right;
//         Quaternion offset = Quaternion.Euler(0, handleAngleOffset, 0);
//         Vector3 innerDirection = offset * Vector3.right;
        
//         topOuterHandle.localPosition = outerDirection * _currentOuterRadius + Vector3.up * _currentTopY;
//         topInnerHandle.localPosition = innerDirection * _currentInnerRadius + Vector3.up * _currentTopY;
//         bottomOuterHandle.localPosition = outerDirection * _currentOuterRadius + Vector3.up * _currentBottomY;
//         bottomInnerHandle.localPosition = innerDirection * _currentInnerRadius + Vector3.up * _currentBottomY;
//     }
    
//     private void SyncPassiveHandles(Transform activeHandle)
//     {
//         Vector3 outerDirection = Vector3.right;
//         Quaternion offset = Quaternion.Euler(0, handleAngleOffset, 0);
//         Vector3 innerDirection = offset * Vector3.right;

//         Vector3 topOuterTarget = outerDirection * _currentOuterRadius + Vector3.up * _currentTopY;
//         Vector3 topInnerTarget = innerDirection * _currentInnerRadius + Vector3.up * _currentTopY;
//         Vector3 bottomOuterTarget = outerDirection * _currentOuterRadius + Vector3.up * _currentBottomY;
//         Vector3 bottomInnerTarget = innerDirection * _currentInnerRadius + Vector3.up * _currentBottomY;
        
//         if (topOuterHandle != activeHandle) topOuterHandle.localPosition = topOuterTarget;
//         if (topInnerHandle != activeHandle) topInnerHandle.localPosition = topInnerTarget;
//         if (bottomOuterHandle != activeHandle) bottomOuterHandle.localPosition = bottomOuterTarget;
//         if (bottomInnerHandle != activeHandle) bottomInnerHandle.localPosition = bottomInnerTarget;
//     }

//     private void GenerateMesh()
//     {
//         if (mesh == null) return;
//         mesh.Clear();
        
//         float actualTopY = Mathf.Max(_currentTopY, _currentBottomY);
//         float actualTopX = Mathf.Max(_currentTopX, _currentBottomX);
//         float actualBottomY = Mathf.Min(_currentTopY, _currentBottomY);
//         float actualBottomX = Mathf.Min(_currentTopX, _currentBottomX);
//         float outerRadius = 0.025f;
//         float innerRadius = 0.01f;

//         var vertices = new List<Vector3>();
//         var normals = new List<Vector3>();
//         var uvs = new List<Vector2>();
//         var triangles = new List<int>();

//         for (int i = 0; i <= radialSegments; i++)
//         {
//             float t = (float)i / radialSegments;
//             float angle = t * Mathf.PI * 2f;
//             var unitCirclePos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));

//             vertices.Add(Vector3.right*actualBottomX + unitCirclePos* outerRadius + Vector3.up * actualBottomY);
//             vertices.Add(Vector3.right*actualTopX + unitCirclePos* outerRadius + Vector3.up * actualTopY);
//             vertices.Add(Vector3.right*actualBottomX + unitCirclePos* innerRadius + Vector3.up * actualBottomY);
//             vertices.Add(Vector3.right*actualTopX + unitCirclePos* innerRadius + Vector3.up * actualTopY);

//             normals.Add(unitCirclePos); normals.Add(unitCirclePos);
//             normals.Add(-unitCirclePos); normals.Add(-unitCirclePos);
//             uvs.Add(new Vector2(t, 0)); uvs.Add(new Vector2(t, 1));
//             uvs.Add(new Vector2(t, 0)); uvs.Add(new Vector2(t, 1));
//         }
//         for (int i = 0; i < radialSegments; i++)
//         {
//             int b = i * 4;
//             triangles.Add(b); triangles.Add(b + 1); triangles.Add(b + 5);
//             triangles.Add(b); triangles.Add(b + 5); triangles.Add(b + 4);
//             triangles.Add(b + 2); triangles.Add(b + 7); triangles.Add(b + 3);
//             triangles.Add(b + 2); triangles.Add(b + 6); triangles.Add(b + 7);
//         }

//         int capStartIndex = vertices.Count;
//         for (int i = 0; i <= radialSegments; i++)
//         {
//             float t = (float)i / radialSegments;
//             float angle = t * Mathf.PI * 2f;
//             var unitCirclePos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));

//             vertices.Add(Vector3.right*actualTopX + unitCirclePos * outerRadius + Vector3.up * actualTopY);
//             vertices.Add(Vector3.right*actualTopX + unitCirclePos * innerRadius + Vector3.up * actualTopY);
//             vertices.Add(Vector3.right*actualBottomX + unitCirclePos * outerRadius + Vector3.up * actualBottomY);
//             vertices.Add(Vector3.right*actualBottomX + unitCirclePos * innerRadius + Vector3.up * actualBottomY);

//             normals.Add(Vector3.up); normals.Add(Vector3.up);
//             normals.Add(Vector3.down); normals.Add(Vector3.down);
//             uvs.Add(unitCirclePos.xyc()); uvs.Add(unitCirclePos.xyc() * (innerRadius / outerRadius));
//             uvs.Add(unitCirclePos.xyc()); uvs.Add(unitCirclePos.xyc() * (innerRadius / outerRadius));
//         }
//         for (int i = 0; i < radialSegments; i++)
//         {
//             int b = capStartIndex + i * 4;
//             triangles.Add(b); triangles.Add(b + 1); triangles.Add(b + 5);
//             triangles.Add(b); triangles.Add(b + 5); triangles.Add(b + 4);
//             triangles.Add(b + 2); triangles.Add(b + 7); triangles.Add(b + 3);
//             triangles.Add(b + 2); triangles.Add(b + 6); triangles.Add(b + 7);
//         }

//         mesh.SetVertices(vertices);
//         mesh.SetNormals(normals);
//         mesh.SetUVs(0, uvs);
//         mesh.SetTriangles(triangles, 0);
//         mesh.RecalculateBounds();

//         // --- MeshCollider update ---
//         if (targetMeshCollider != null)
//         {
//             targetMeshCollider.sharedMesh = null; // force update
//             targetMeshCollider.sharedMesh = mesh;
//         }
//     }
// }

// public static class Vector3ExtensionsNew
// {
//     public static Vector2 xyc(this Vector3 v)
//     {
//         return new Vector2(v.x, v.y);
//     }
// }
