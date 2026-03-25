using UnityEngine;

public class StretchParentMeshOneWayFixed : MonoBehaviour
{
    public MeshFilter parentMeshFilter; // Assign parent's MeshFilter in inspector
    public float stretchMultiplier = 1f;

    private Mesh mesh;
    private Vector3[] modifiedVertices;
    private Vector3 lastPosition;
    private Vector3 meshCenter;
    private bool[] isForwardVertex;

    private bool stretchActive =false;

    void Start()
    {
        if (parentMeshFilter == null)
        {
            Debug.LogError("Parent MeshFilter not assigned!");
            enabled = false;
            return;
        }

        mesh = Instantiate(parentMeshFilter.sharedMesh);
        parentMeshFilter.mesh = mesh;

        modifiedVertices = mesh.vertices;
        meshCenter = mesh.bounds.center;

        // Determine forward-side vertices once (based on local X)
        isForwardVertex = new bool[modifiedVertices.Length];
        for (int i = 0; i < modifiedVertices.Length; i++)
        {
            if (modifiedVertices[i].x > meshCenter.x)
                isForwardVertex[i] = true;
            else
                isForwardVertex[i] = false;
        }

        lastPosition = transform.position;
    }

    public void WhenGrabbedUpdate()
    {
        stretchActive = true;
    }

    public void WhenReleased()
    {
        stretchActive = false;
    }
    void Update()
    {
        if (stretchActive == true)
        { 
            Vector3 movement = transform.position - lastPosition;

            if (movement.sqrMagnitude > 0f)
            {
                // Convert movement to parent's local space
                Vector3 localMovement = parentMeshFilter.transform.InverseTransformDirection(movement) * stretchMultiplier;

                // Move only forward-side vertices
                for (int i = 0; i < modifiedVertices.Length; i++)
                {
                    if (isForwardVertex[i])
                    {
                        modifiedVertices[i] += localMovement;
                    }
                }

                mesh.vertices = modifiedVertices;
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
            }

            lastPosition = transform.position;
        }
    }
}
