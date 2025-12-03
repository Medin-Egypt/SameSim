using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using System.Linq;

public class SurgicalCuttingSystem : MonoBehaviour
{
    public enum SurgicalMode { Pull, Cut }
    
    [Header("Mode Settings")]
    public SurgicalMode currentMode = SurgicalMode.Pull;
    
    [Header("XR Controller")]
    public Transform rightController;  // Assign your right controller transform
    public LineRenderer cuttingRay;
    
    [Header("Cutting Properties")]
    [Range(0.01f, 0.5f)]
    [Tooltip("Radius of area to remove when cutting")]
    public float cuttingRadius = 0.05f;
    
    [Range(0.01f, 1f)]
    [Tooltip("Maximum distance for cutting ray")]
    public float maxCuttingDistance = 0.3f;
    
    [Range(1, 10)]
    [Tooltip("Number of vertices to remove per cut stroke")]
    public int verticesPerCut = 5;
    
    [Header("Visual Feedback")]
    public Color pullModeColor = Color.green;
    public Color cutModeColor = Color.red;
    public Material cutLineMaterial;
    
    [Header("Debug")]
    public bool showDebugInfo = true;

    private bool isCutting = false;
    private ElasticMesh currentTargetMesh;
    private Vector3 lastCutPosition;
    private float cutMovementThreshold = 0.01f;
    
    // Track separated mesh parts
    private List<GameObject> separatedParts = new List<GameObject>();

    void Start()
    {
        SetupCuttingRay();
        SetMode(SurgicalMode.Pull); // Start in pull mode
    }

    void Update()
    {
        HandleInput();
        
        if (currentMode == SurgicalMode.Cut)
        {
            UpdateCuttingRay();
            HandleCutting();
        }
    }

    void HandleInput()
    {
        // Keybind 1: Pull Mode
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetMode(SurgicalMode.Pull);
        }
        
        // Keybind 2: Cut Mode
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetMode(SurgicalMode.Cut);
        }
    }

    void SetMode(SurgicalMode mode)
    {
        currentMode = mode;
        
        if (mode == SurgicalMode.Pull)
        {
            Debug.Log("<color=green>✓ PULL ACTIVATED</color>");
            if (cuttingRay != null) cuttingRay.enabled = false;
        }
        else if (mode == SurgicalMode.Cut)
        {
            Debug.Log("<color=red>✓ CUT ACTIVATED</color>");
            if (cuttingRay != null) cuttingRay.enabled = true;
        }
        
        UpdateVisualFeedback();
    }

    void SetupCuttingRay()
    {
        if (cuttingRay == null)
        {
            GameObject rayObj = new GameObject("CuttingRay");
            rayObj.transform.SetParent(transform);
            cuttingRay = rayObj.AddComponent<LineRenderer>();
        }
        
        cuttingRay.startWidth = 0.002f;
        cuttingRay.endWidth = 0.002f;
        cuttingRay.positionCount = 2;
        cuttingRay.enabled = false;
        
        if (cutLineMaterial != null)
        {
            cuttingRay.material = cutLineMaterial;
        }
        else
        {
            cuttingRay.material = new Material(Shader.Find("Sprites/Default"));
        }
    }

    void UpdateCuttingRay()
    {
        if (cuttingRay == null || rightController == null) return;
        
        Vector3 rayOrigin = rightController.transform.position;
        Vector3 rayDirection = rightController.transform.forward;
        
        cuttingRay.SetPosition(0, rayOrigin);
        cuttingRay.SetPosition(1, rayOrigin + rayDirection * maxCuttingDistance);
        
        cuttingRay.startColor = cutModeColor;
        cuttingRay.endColor = new Color(cutModeColor.r, cutModeColor.g, cutModeColor.b, 0.3f);
    }

    void UpdateVisualFeedback()
    {
        if (cuttingRay != null)
        {
            Color modeColor = currentMode == SurgicalMode.Cut ? cutModeColor : pullModeColor;
            cuttingRay.startColor = modeColor;
            cuttingRay.endColor = new Color(modeColor.r, modeColor.g, modeColor.b, 0.3f);
        }
    }

    void HandleCutting()
    {
        // Left mouse button or trigger to start/continue cutting
        bool cutInput = Input.GetMouseButton(0);
        
        // If using XR, you can add XR input here
        // Example: cutInput = cutInput || OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger);
        
        if (cutInput)
        {
            if (!isCutting)
            {
                isCutting = true;
                lastCutPosition = Vector3.zero;
            }
            
            PerformCut();
        }
        else
        {
            if (isCutting)
            {
                isCutting = false;
                if (showDebugInfo)
                {
                    Debug.Log("<color=yellow>Cut stroke ended</color>");
                }
            }
        }
    }

    void PerformCut()
    {
        if (rightController == null) return;
        
        Vector3 rayOrigin = rightController.transform.position;
        Vector3 rayDirection = rightController.transform.forward;
        
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, maxCuttingDistance))
        {
            ElasticMesh elasticMesh = hit.collider.GetComponent<ElasticMesh>();
            
            if (elasticMesh != null)
            {
                // Check if ray has moved enough to make a new cut
                if (Vector3.Distance(hit.point, lastCutPosition) > cutMovementThreshold)
                {
                    CutMeshAtPoint(elasticMesh, hit.point);
                    lastCutPosition = hit.point;
                    currentTargetMesh = elasticMesh;
                }
            }
        }
    }

    void CutMeshAtPoint(ElasticMesh elasticMesh, Vector3 worldPoint)
    {
        MeshFilter meshFilter = elasticMesh.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.mesh == null) return;
        
        Mesh mesh = meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        
        // Convert world point to local space
        Vector3 localPoint = elasticMesh.transform.InverseTransformPoint(worldPoint);
        
        // Find vertices within cutting radius
        List<int> verticesToRemove = new List<int>();
        
        for (int i = 0; i < vertices.Length; i++)
        {
            float distance = Vector3.Distance(localPoint, vertices[i]);
            
            if (distance < cuttingRadius)
            {
                verticesToRemove.Add(i);
                
                // Mark as permanently displaced in the elastic mesh
                elasticMesh.permanentlyDisplacedVertices.Add(i);
            }
        }
        
        if (verticesToRemove.Count > 0)
        {
            // Remove triangles that use these vertices
            List<int> newTriangles = new List<int>();
            
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];
                
                // Keep triangle only if none of its vertices are being removed
                if (!verticesToRemove.Contains(v0) && 
                    !verticesToRemove.Contains(v1) && 
                    !verticesToRemove.Contains(v2))
                {
                    newTriangles.Add(v0);
                    newTriangles.Add(v1);
                    newTriangles.Add(v2);
                }
            }
            
            // Update mesh
            mesh.triangles = newTriangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            // Update collider
            MeshCollider meshCollider = elasticMesh.GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = mesh;
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=orange>Cut removed {verticesToRemove.Count} vertices and {(triangles.Length - newTriangles.Count) / 3} triangles</color>");
            }
            
            // Check if we should separate this into a new part
            CheckForSeparation(elasticMesh, mesh);
        }
    }

    void CheckForSeparation(ElasticMesh elasticMesh, Mesh mesh)
    {
        // This is a simplified separation check
        // A more complex implementation would analyze mesh connectivity
        
        int[] triangles = mesh.triangles;
        
        // If mesh has very few triangles left, it might be separated
        if (triangles.Length < mesh.vertexCount / 2)
        {
            if (showDebugInfo)
            {
                Debug.Log("<color=cyan>Mesh significantly cut - potential separation detected</color>");
            }
            
            // Here you could create separated mesh parts
            // This would require more complex mesh analysis and splitting
        }
    }

    // Public method to create a separated mesh part
    public GameObject CreateSeparatedPart(ElasticMesh sourceMesh, List<int> vertexIndices)
    {
        GameObject newPart = new GameObject($"{sourceMesh.gameObject.name}_CutPart");
        newPart.transform.position = sourceMesh.transform.position;
        newPart.transform.rotation = sourceMesh.transform.rotation;
        newPart.transform.localScale = sourceMesh.transform.localScale;
        
        // Add necessary components
        MeshFilter newMeshFilter = newPart.AddComponent<MeshFilter>();
        MeshRenderer newMeshRenderer = newPart.AddComponent<MeshRenderer>();
        ElasticMesh newElasticMesh = newPart.AddComponent<ElasticMesh>();
        MeshCollider newCollider = newPart.AddComponent<MeshCollider>();
        
        // Copy material
        MeshRenderer sourceRenderer = sourceMesh.GetComponent<MeshRenderer>();
        if (sourceRenderer != null)
        {
            newMeshRenderer.material = sourceRenderer.material;
        }
        
        // Create new mesh from selected vertices
        Mesh sourceMeshData = sourceMesh.GetComponent<MeshFilter>().mesh;
        Mesh newMesh = new Mesh();
        
        // This would need more implementation to properly extract submesh
        // For now, this is a placeholder
        
        newMeshFilter.mesh = newMesh;
        newCollider.sharedMesh = newMesh;
        
        separatedParts.Add(newPart);
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=green>Created separated part: {newPart.name}</color>");
        }
        
        return newPart;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || currentMode != SurgicalMode.Cut) return;
        
        if (rightController != null)
        {
            Vector3 rayOrigin = rightController.transform.position;
            Vector3 rayDirection = rightController.transform.forward;
            
            Gizmos.color = cutModeColor;
            Gizmos.DrawRay(rayOrigin, rayDirection * maxCuttingDistance);
            
            // Show cutting radius at ray hit point
            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, rayDirection, out hit, maxCuttingDistance))
            {
                Gizmos.color = new Color(cutModeColor.r, cutModeColor.g, cutModeColor.b, 0.3f);
                Gizmos.DrawWireSphere(hit.point, cuttingRadius);
            }
        }
    }
}