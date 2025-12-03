using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshCollider))]
public class ElasticMesh : MonoBehaviour
{
    [Header("Cut Integration")]
    public bool respectCuts = true;
    public HashSet<int> permanentlyDisplacedVertices = new HashSet<int>();
    private HashSet<int> removedVertices = new HashSet<int>();

    [Header("Elastic Properties")]
    [Range(0.1f, 20f)]
    [Tooltip("How quickly mesh returns to original shape (higher = faster)")]
    public float elasticity = 10f;
    
    [Range(0.001f, 0.5f)]
    [Tooltip("Maximum depth of deformation")]
    public float maxDeformation = 0.08f;
    
    [Range(0.01f, 0.5f)]
    [Tooltip("Radius of influence around touch point")]
    public float influenceRadius = 0.2f;
    
    [Range(1f, 4f)]
    [Tooltip("Smoothness of deformation falloff (higher = smoother edges)")]
    public float falloffPower = 3f;
    
    [Range(0.1f, 1f)]
    [Tooltip("Smoothness of deformation application (higher = smoother)")]
    public float deformationSmoothing = 0.7f;
    
    [Header("Edge Anchoring")]
    [Range(0f, 5f)]
    [Tooltip("How strongly edges are anchored (0 = no anchoring, higher = stronger)")]
    public float edgeAnchorStrength = 2f;

    [Header("Cut Influence")]
    [Range(0.01f, 0.3f)]
    [Tooltip("Radius around cut vertices that limits elasticity")]
    public float cutInfluenceRadius = 0.1f;
    
    [Range(0f, 1f)]
    [Tooltip("How much to reduce elasticity near cuts (0 = no reduction, 1 = full reduction)")]
    public float cutElasticityReduction = 0.7f;

    [Header("Debug")]
    public bool showDebugInfo = false;

    private Mesh workingMesh;
    private Vector3[] originalVertices;
    private Vector3[] displacedVertices;
    private Vector3[] vertexVelocities;
    private MeshCollider meshCollider;
    private MeshFilter meshFilter;

    private bool isDeforming = false;
    private Vector3 lockedDeformationPoint;
    
    // Track which vertices are part of separated regions
    private Dictionary<int, int> vertexRegionMap = new Dictionary<int, int>();
    private int nextRegionId = 0;

    void Start()
    {
        InitializeMesh();
    }

    void InitializeMesh()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        
        Mesh sourceMesh = null;
        
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            sourceMesh = meshFilter.sharedMesh;
        }
        else if (meshCollider != null && meshCollider.sharedMesh != null)
        {
            sourceMesh = meshCollider.sharedMesh;
        }
        
        if (sourceMesh == null)
        {
            Debug.LogError($"ElasticMesh: No mesh found on {gameObject.name}");
            enabled = false;
            return;
        }

        workingMesh = new Mesh();
        workingMesh.name = gameObject.name + "_Elastic";
        
        try
        {
            originalVertices = sourceMesh.vertices;
            workingMesh.vertices = originalVertices;
            workingMesh.triangles = sourceMesh.triangles;
            workingMesh.normals = sourceMesh.normals;
            workingMesh.uv = sourceMesh.uv;
            workingMesh.RecalculateBounds();
            
            displacedVertices = new Vector3[originalVertices.Length];
            vertexVelocities = new Vector3[originalVertices.Length];
            System.Array.Copy(originalVertices, displacedVertices, originalVertices.Length);
            
            // Initialize all vertices to region 0
            for (int i = 0; i < originalVertices.Length; i++)
            {
                vertexRegionMap[i] = 0;
            }
            
            if (meshFilter != null)
            {
                meshFilter.mesh = workingMesh;
            }
            
            if (meshCollider != null)
            {
                meshCollider.sharedMesh = workingMesh;
                meshCollider.convex = true;
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=green>✓ ElasticMesh initialized: {gameObject.name} ({originalVertices.Length} vertices)</color>");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"<color=red>✗ ElasticMesh failed: {gameObject.name} - {e.Message}</color>");
            enabled = false;
        }
    }

    void Update()
    {
        if (originalVertices == null || displacedVertices == null)
            return;

        bool needsUpdate = false;
        
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            // Skip removed vertices
            if (removedVertices.Contains(i))
                continue;
                
            // Calculate elasticity multiplier based on proximity to cuts
            float elasticityMultiplier = CalculateElasticityMultiplier(i);
            
            Vector3 displacement = originalVertices[i] - displacedVertices[i];
            
            if (displacement.sqrMagnitude > 0.00001f)
            {
                needsUpdate = true;
                
                Vector3 force = displacement * elasticity * elasticityMultiplier;
                vertexVelocities[i] += force * Time.deltaTime;
                vertexVelocities[i] *= 0.92f;
                
                displacedVertices[i] += vertexVelocities[i] * Time.deltaTime;
            }
            else
            {
                displacedVertices[i] = originalVertices[i];
                vertexVelocities[i] = Vector3.zero;
            }
        }
        
        if (needsUpdate)
        {
            workingMesh.vertices = displacedVertices;
            workingMesh.RecalculateNormals();
            workingMesh.RecalculateBounds();
            
            if (meshCollider != null)
            {
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = workingMesh;
            }
        }
    }

    float CalculateElasticityMultiplier(int vertexIndex)
    {
        if (!respectCuts || permanentlyDisplacedVertices.Count == 0)
            return 1f;
        
        // Check if this vertex is near any cut vertices
        float minDistance = float.MaxValue;
        
        foreach (int cutVertex in permanentlyDisplacedVertices)
        {
            float distance = Vector3.Distance(originalVertices[vertexIndex], originalVertices[cutVertex]);
            minDistance = Mathf.Min(minDistance, distance);
        }
        
        // If within cut influence radius, reduce elasticity
        if (minDistance < cutInfluenceRadius)
        {
            float normalizedDistance = minDistance / cutInfluenceRadius;
            float reduction = Mathf.Lerp(cutElasticityReduction, 0f, normalizedDistance);
            return 1f - reduction;
        }
        
        return 1f;
    }

    public void StartDeformation(Vector3 worldPoint)
    {
        isDeforming = true;
        lockedDeformationPoint = transform.InverseTransformPoint(worldPoint);
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>Deformation locked at: {lockedDeformationPoint}</color>");
        }
    }

    public void EndDeformation()
    {
        isDeforming = false;
        
        if (showDebugInfo)
        {
            Debug.Log("<color=yellow>Deformation unlocked</color>");
        }
    }

    public void ApplyDeformation(Vector3 worldPoint, float pushDepth)
    {
        if (originalVertices == null || displacedVertices == null)
            return;

        Vector3 localPoint = isDeforming ? lockedDeformationPoint : transform.InverseTransformPoint(worldPoint);
        
        Bounds meshBounds = workingMesh.bounds;
        Vector3 boundsCenter = meshBounds.center;
        Vector3 boundsExtents = meshBounds.extents;
        
        for (int i = 0; i < originalVertices.Length; i++)
        {
            // Skip removed vertices
            if (removedVertices.Contains(i))
                continue;
                
            float distance = Vector3.Distance(localPoint, originalVertices[i]);
            
            if (distance < influenceRadius)
            {
                // Distance-based influence
                float normalizedDistance = distance / influenceRadius;
                float influence = 1f - Mathf.Pow(normalizedDistance, falloffPower);
                influence = influence * influence * (3f - 2f * influence);
                
                // Edge anchoring calculation
                Vector3 vertexLocal = originalVertices[i];
                float edgeX = Mathf.Abs(vertexLocal.x - boundsCenter.x) / boundsExtents.x;
                float edgeY = Mathf.Abs(vertexLocal.y - boundsCenter.y) / boundsExtents.y;
                float edgeZ = Mathf.Abs(vertexLocal.z - boundsCenter.z) / boundsExtents.z;
                float edgeProximity = Mathf.Max(edgeX, Mathf.Max(edgeY, edgeZ));
                
                float anchorFactor = 1f - edgeProximity;
                anchorFactor = Mathf.Pow(anchorFactor, edgeAnchorStrength);
                
                // Check if near cut - reduce deformation if so
                float cutProximity = GetCutProximity(i);
                float cutFactor = 1f - (cutProximity * 0.5f); // Reduce deformation by up to 50% near cuts
                
                // Combine influences
                float finalInfluence = influence * anchorFactor * cutFactor;
                
                // Apply deformation
                Vector3 direction = (localPoint - originalVertices[i]).normalized;
                float actualDepth = Mathf.Clamp(pushDepth * finalInfluence, -maxDeformation, maxDeformation);
                Vector3 targetPosition = originalVertices[i] + direction * actualDepth;
                displacedVertices[i] = Vector3.Lerp(displacedVertices[i], targetPosition, deformationSmoothing);
            }
        }
    }

    float GetCutProximity(int vertexIndex)
    {
        if (permanentlyDisplacedVertices.Count == 0)
            return 0f;
        
        float minDistance = float.MaxValue;
        
        foreach (int cutVertex in permanentlyDisplacedVertices)
        {
            float distance = Vector3.Distance(originalVertices[vertexIndex], originalVertices[cutVertex]);
            minDistance = Mathf.Min(minDistance, distance);
        }
        
        if (minDistance < cutInfluenceRadius)
        {
            return 1f - (minDistance / cutInfluenceRadius);
        }
        
        return 0f;
    }

    public void MarkVerticesAsRemoved(List<int> vertexIndices)
    {
        foreach (int index in vertexIndices)
        {
            removedVertices.Add(index);
            permanentlyDisplacedVertices.Add(index);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"<color=red>Marked {vertexIndices.Count} vertices as removed</color>");
        }
    }

    public void AssignVertexToRegion(int vertexIndex, int regionId)
    {
        vertexRegionMap[vertexIndex] = regionId;
    }

    public int GetVertexRegion(int vertexIndex)
    {
        return vertexRegionMap.ContainsKey(vertexIndex) ? vertexRegionMap[vertexIndex] : 0;
    }

    public int CreateNewRegion()
    {
        nextRegionId++;
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>Created new region: {nextRegionId}</color>");
        }
        return nextRegionId;
    }

    void OnDestroy()
    {
        if (workingMesh != null)
        {
            Destroy(workingMesh);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, influenceRadius);
        
        if (isDeforming)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.TransformPoint(lockedDeformationPoint), 0.01f);
        }
        
        // Show cut influence areas
        if (respectCuts && permanentlyDisplacedVertices.Count > 0 && originalVertices != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            foreach (int cutVertex in permanentlyDisplacedVertices)
            {
                if (cutVertex < originalVertices.Length)
                {
                    Vector3 worldPos = transform.TransformPoint(originalVertices[cutVertex]);
                    Gizmos.DrawWireSphere(worldPos, cutInfluenceRadius);
                }
            }
        }
    }
}