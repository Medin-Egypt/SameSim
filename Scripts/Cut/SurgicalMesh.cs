using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class SurgicalMesh : MonoBehaviour
{
    [Header("Configuration")]
    public float influenceRadius = 0.15f;
    public float maxDeformationDepth = 0.1f;
    public float cutThreshold = 0.08f; 
    public float falloffPower = 2f;

    [Header("Materials")]
    [Tooltip("The material to use for the inside/damaged parts")]
    public Material fleshMaterial;

    // -- Internal Data --
    private Mesh workingMesh;
    private Vector3[] originalVertices;
    private Vector3[] currentVertices;
    
    // Adjacency: vertexIndex -> list of triangleIndices that use this vertex
    private List<int>[] vertexToTriangles; 
    
    // Master list of all triangles in groups of 3
    private int[] allTriangles; 
    
    // The state of every triangle. 0=Original(Skin), 1=Flesh, 2=Hole
    private byte[] triangleStates; 

    private bool meshIsDirty = false;
    private MeshCollider meshCollider;
    private MeshRenderer meshRenderer;

    void Start()
    {
        InitializeMesh();
    }

    void InitializeMesh()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        Mesh sourceMesh = mf.sharedMesh;

        if (fleshMaterial == null)
        {
            Debug.LogError("Please assign a Flesh Material to the Surgical Mesh script!");
            this.enabled = false;
            return;
        }

        // 1. Grab the Original Material
        // We take whatever is currently on the object.
        Material originalSkinMat = meshRenderer.sharedMaterial;
        
        // 2. Setup the Renderer with BOTH materials
        // Index 0 = Original, Index 1 = Flesh
        meshRenderer.sharedMaterials = new Material[] { originalSkinMat, fleshMaterial };

        // 3. Clone Mesh
        workingMesh = Instantiate(sourceMesh);
        workingMesh.name = gameObject.name + "_ColorSwap";
        
        originalVertices = sourceMesh.vertices;
        currentVertices = sourceMesh.vertices;
        allTriangles = sourceMesh.triangles;

        // 4. Setup Adjacency Map
        int triCount = allTriangles.Length / 3;
        vertexToTriangles = new List<int>[originalVertices.Length];
        triangleStates = new byte[triCount]; // Defaults to 0 (Original Skin)

        for (int i = 0; i < vertexToTriangles.Length; i++)
            vertexToTriangles[i] = new List<int>();

        for (int t = 0; t < triCount; t++)
        {
            int v1 = allTriangles[t * 3 + 0];
            int v2 = allTriangles[t * 3 + 1];
            int v3 = allTriangles[t * 3 + 2];

            vertexToTriangles[v1].Add(t);
            vertexToTriangles[v2].Add(t);
            vertexToTriangles[v3].Add(t);
        }

        // 5. Finalize Mesh Setup
        workingMesh.subMeshCount = 2;
        mf.mesh = workingMesh;
        
        UpdateSubmeshes(); // Puts all triangles into Submesh 0 initially

        if (meshCollider != null)
        {
            meshCollider.sharedMesh = workingMesh;
            meshCollider.convex = false;
        }
    }

    void LateUpdate()
    {
        if (meshIsDirty)
        {
            workingMesh.vertices = currentVertices;
            workingMesh.RecalculateNormals();
            
            UpdateSubmeshes();
            meshIsDirty = false;
        }
    }

    void UpdateSubmeshes()
    {
        List<int> skinTris = new List<int>();
        List<int> fleshTris = new List<int>();

        for (int i = 0; i < triangleStates.Length; i++)
        {
            if (triangleStates[i] == 2) continue; // Hole - skip completely

            int baseIndex = i * 3;
            
            if (triangleStates[i] == 0) // Skin
            {
                skinTris.Add(allTriangles[baseIndex]);
                skinTris.Add(allTriangles[baseIndex+1]);
                skinTris.Add(allTriangles[baseIndex+2]);
            }
            else if (triangleStates[i] == 1) // Flesh
            {
                fleshTris.Add(allTriangles[baseIndex]);
                fleshTris.Add(allTriangles[baseIndex+1]);
                fleshTris.Add(allTriangles[baseIndex+2]);
            }
        }

        // Apply lists to the respective Submeshes
        workingMesh.SetTriangles(skinTris, 0);
        workingMesh.SetTriangles(fleshTris, 1);
    }

    public void Deform(Vector3 pointOfContact, float pushStrength)
    {
        Vector3 localPoint = transform.InverseTransformPoint(pointOfContact);
        bool localDirty = false;

        for (int i = 0; i < currentVertices.Length; i++)
        {
            float dist = Vector3.Distance(localPoint, currentVertices[i]);

            if (dist < influenceRadius)
            {
                // -- Deformation --
                float normalizedDist = dist / influenceRadius;
                float influence = 1f - Mathf.Pow(normalizedDist, falloffPower);
                
                Vector3 pushDir = (currentVertices[i] - localPoint).normalized;
                Vector3 displacement = pushDir * (pushStrength * influence * Time.deltaTime);
                Vector3 proposedPos = currentVertices[i] + displacement;

                float totalDeformation = Vector3.Distance(originalVertices[i], proposedPos);

                if (totalDeformation < maxDeformationDepth)
                {
                    currentVertices[i] = proposedPos;
                    localDirty = true;

                    // -- Turn to Flesh --
                    foreach (int triIndex in vertexToTriangles[i])
                    {
                        if (triangleStates[triIndex] == 0) 
                            triangleStates[triIndex] = 1; 
                    }
                }
                
                // -- Create Hole --
                if (totalDeformation >= cutThreshold)
                {
                    foreach (int triIndex in vertexToTriangles[i])
                    {
                        triangleStates[triIndex] = 2; 
                    }
                    localDirty = true;
                }
            }
        }

        if (localDirty) meshIsDirty = true;
    }
}