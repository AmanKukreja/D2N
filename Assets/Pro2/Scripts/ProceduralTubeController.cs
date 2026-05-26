using UnityEngine;
using System.Collections.Generic;

public class ProceduralTubeController : MonoBehaviour
{
    [Header("Object References")]
    [Tooltip("The object that will display the generated mesh.")]
    [SerializeField]
    private MeshFilter targetMeshFilter;

    [Tooltip("The collider that will be updated. This is a best-fit capsule collider.")]
    [SerializeField]
    private CapsuleCollider targetCapsuleCollider;

    [Header("Handles")]
    public Transform topOuterHandle;
    public Transform topInnerHandle;
    public Transform bottomOuterHandle;
    public Transform bottomInnerHandle;
    
    [Tooltip("The angular separation between inner and outer handles.")]
    [Range(0f, 90f)] public float handleAngleOffset = 30f;
    
    [Header("Mesh Settings")]
    [Range(3, 64)] public int radialSegments = 24;

    [Header("Interaction Constraints")]
    public float minWallThickness = 0.02f;

    [Header("Initial State")]
    public float initialLength = 2f;
    public float initialOuterRadius = 0.5f;
    public float initialInnerRadius = 0.4f;

    private float _currentTopY;
    private float _currentBottomY;
    private float _currentOuterRadius;
    private float _currentInnerRadius;

    private Mesh mesh;
    private Transform _lastMovedHandle = null;

    private void Awake()
    {
        if (targetMeshFilter == null || targetCapsuleCollider == null ||
            topOuterHandle == null || topInnerHandle == null ||
            bottomOuterHandle == null || bottomInnerHandle == null)
        {
            Debug.LogError("A required reference on the ProceduralTubeController is not assigned. Please check all references in the Inspector.", this);
            this.enabled = false;
            return;
        }

        mesh = new Mesh { name = "ProceduralTube" };
        targetMeshFilter.mesh = mesh;
        targetCapsuleCollider.direction = 1; // 1 = Y-Axis
    }

    private void Start()
    {
        Initialize();
    }

    void Update()
    {
        Transform activeHandleThisFrame = null;
        
        if (topOuterHandle.hasChanged) activeHandleThisFrame = topOuterHandle;
        else if (topInnerHandle.hasChanged) activeHandleThisFrame = topInnerHandle;
        else if (bottomOuterHandle.hasChanged) activeHandleThisFrame = bottomOuterHandle;
        else if (bottomInnerHandle.hasChanged) activeHandleThisFrame = bottomInnerHandle;

        if (activeHandleThisFrame == null && _lastMovedHandle != null)
        {
            // A handle was just released. Do a full sync to snap the released handle back.
            SyncAllHandlePositions();
        }

        if (activeHandleThisFrame != null)
        {
            Vector3 handlePos = activeHandleThisFrame.localPosition;
            float potentialRadius = new Vector2(handlePos.x, handlePos.z).magnitude;

            if (activeHandleThisFrame == topOuterHandle || activeHandleThisFrame == bottomOuterHandle)
            {
                if (potentialRadius >= _currentInnerRadius + minWallThickness) { _currentOuterRadius = potentialRadius; }
            }
            else
            {
                if (potentialRadius <= _currentOuterRadius - minWallThickness && potentialRadius >= 0.01f) { _currentInnerRadius = potentialRadius; }
            }
            
            if (activeHandleThisFrame == topOuterHandle || activeHandleThisFrame == topInnerHandle) { _currentTopY = handlePos.y; }
            else { _currentBottomY = handlePos.y; }

            // --- NEW: Sync the passive handles during the drag. ---
            SyncPassiveHandles(activeHandleThisFrame);
            GenerateMesh();
        }

        _lastMovedHandle = activeHandleThisFrame;
        if(activeHandleThisFrame != null)
        {
            topOuterHandle.hasChanged = false;
            topInnerHandle.hasChanged = false;
            bottomOuterHandle.hasChanged = false;
            bottomInnerHandle.hasChanged = false;
        }
    }
    
    public void Initialize()
    {
        _currentTopY = initialLength / 2f;
        _currentBottomY = -initialLength / 2f;
        _currentOuterRadius = Mathf.Max(initialOuterRadius, initialInnerRadius + minWallThickness);
        _currentInnerRadius = Mathf.Max(0.01f, Mathf.Min(initialInnerRadius, _currentOuterRadius - minWallThickness));

        SyncAllHandlePositions();
        GenerateMesh();
    }

    private void SyncAllHandlePositions()
    {
        // This is a "full sync" used for initialization and snapping back on release.
        // It's safe to call because we know no handle is being controlled by the SDK.
        Vector3 outerDirection = Vector3.right;
        Quaternion offset = Quaternion.Euler(0, handleAngleOffset, 0);
        Vector3 innerDirection = offset * Vector3.right;
        
        topOuterHandle.localPosition = outerDirection * _currentOuterRadius + Vector3.up * _currentTopY;
        topInnerHandle.localPosition = innerDirection * _currentInnerRadius + Vector3.up * _currentTopY;
        bottomOuterHandle.localPosition = outerDirection * _currentOuterRadius + Vector3.up * _currentBottomY;
        bottomInnerHandle.localPosition = innerDirection * _currentInnerRadius + Vector3.up * _currentBottomY;
    }
    
    // --- NEW: This method updates only the handles that are NOT being actively dragged. ---
    private void SyncPassiveHandles(Transform activeHandle)
    {
        Vector3 outerDirection = Vector3.right;
        Quaternion offset = Quaternion.Euler(0, handleAngleOffset, 0);
        Vector3 innerDirection = offset * Vector3.right;

        // Calculate the target positions based on the true internal state.
        Vector3 topOuterTarget = outerDirection * _currentOuterRadius + Vector3.up * _currentTopY;
        Vector3 topInnerTarget = innerDirection * _currentInnerRadius + Vector3.up * _currentTopY;
        Vector3 bottomOuterTarget = outerDirection * _currentOuterRadius + Vector3.up * _currentBottomY;
        Vector3 bottomInnerTarget = innerDirection * _currentInnerRadius + Vector3.up * _currentBottomY;
        
        // Only set the position if the handle is not the one being controlled by the SDK.
        if (topOuterHandle != activeHandle) topOuterHandle.localPosition = topOuterTarget;
        if (topInnerHandle != activeHandle) topInnerHandle.localPosition = topInnerTarget;
        if (bottomOuterHandle != activeHandle) bottomOuterHandle.localPosition = bottomOuterTarget;
        if (bottomInnerHandle != activeHandle) bottomInnerHandle.localPosition = bottomInnerTarget;
    }

    private void GenerateMesh()
    {
        if (mesh == null) return;
        mesh.Clear();
        
        float actualTopY = Mathf.Max(_currentTopY, _currentBottomY);
        float actualBottomY = Mathf.Min(_currentTopY, _currentBottomY);
        float outerRadius = _currentOuterRadius;
        float innerRadius = _currentInnerRadius;

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        for (int i = 0; i <= radialSegments; i++)
        {
            float t = (float)i / radialSegments;
            float angle = t * Mathf.PI * 2f;
            var unitCirclePos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            vertices.Add(unitCirclePos * outerRadius + Vector3.up * actualBottomY);
            vertices.Add(unitCirclePos * outerRadius + Vector3.up * actualTopY);
            vertices.Add(unitCirclePos * innerRadius + Vector3.up * actualBottomY);
            vertices.Add(unitCirclePos * innerRadius + Vector3.up * actualTopY);
            normals.Add(unitCirclePos); normals.Add(unitCirclePos);
            normals.Add(-unitCirclePos); normals.Add(-unitCirclePos);
            uvs.Add(new Vector2(t, 0)); uvs.Add(new Vector2(t, 1));
            uvs.Add(new Vector2(t, 0)); uvs.Add(new Vector2(t, 1));
        }
        for (int i = 0; i < radialSegments; i++)
        {
            int b = i * 4;
            triangles.Add(b); triangles.Add(b + 1); triangles.Add(b + 5);
            triangles.Add(b); triangles.Add(b + 5); triangles.Add(b + 4);
            triangles.Add(b + 2); triangles.Add(b + 7); triangles.Add(b + 3);
            triangles.Add(b + 2); triangles.Add(b + 6); triangles.Add(b + 7);
        }
        int capStartIndex = vertices.Count;
        for (int i = 0; i <= radialSegments; i++)
        {
            float t = (float)i / radialSegments;
            float angle = t * Mathf.PI * 2f;
            var unitCirclePos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            vertices.Add(unitCirclePos * outerRadius + Vector3.up * actualTopY);
            vertices.Add(unitCirclePos * innerRadius + Vector3.up * actualTopY);
            vertices.Add(unitCirclePos * outerRadius + Vector3.up * actualBottomY);
            vertices.Add(unitCirclePos * innerRadius + Vector3.up * actualBottomY);
            normals.Add(Vector3.up); normals.Add(Vector3.up);
            normals.Add(Vector3.down); normals.Add(Vector3.down);
            uvs.Add(unitCirclePos.xy()); uvs.Add(unitCirclePos.xy() * (innerRadius / outerRadius));
            uvs.Add(unitCirclePos.xy()); uvs.Add(unitCirclePos.xy() * (innerRadius / outerRadius));
        }
        for (int i = 0; i < radialSegments; i++)
        {
            int b = capStartIndex + i * 4;
            triangles.Add(b); triangles.Add(b + 1); triangles.Add(b + 5);
            triangles.Add(b); triangles.Add(b + 5); triangles.Add(b + 4);
            triangles.Add(b + 2); triangles.Add(b + 7); triangles.Add(b + 3);
            triangles.Add(b + 2); triangles.Add(b + 6); triangles.Add(b + 7);
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        
        if (targetCapsuleCollider != null)
        {
            float height = actualTopY - actualBottomY;
            targetCapsuleCollider.radius = outerRadius;
            targetCapsuleCollider.height = Mathf.Max(height, outerRadius * 2);
            targetCapsuleCollider.center = new Vector3(0, (actualTopY + actualBottomY) / 2.0f, 0);
        }
    }
}

public static class Vector3Extensions
{
    public static Vector2 xy(this Vector3 v)
    {
        return new Vector2(v.x, v.y);
    }
}