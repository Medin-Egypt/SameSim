using UnityEngine;

public class ScalpelController : MonoBehaviour
{
    [Header("Scalpel Settings")]
    [Tooltip("The sharp edge should point in this local direction")]
    public Vector3 sharpEdgeDirection = Vector3.up;
    
    [Tooltip("The handle should point in this local direction")]
    public Vector3 handleDirection = Vector3.forward;
    
    [Header("Pickup Settings")]
    [Tooltip("Distance from controller when held")]
    public float holdDistance = 0.15f;
    
    [Tooltip("Smoothness of movement when held")]
    public float followSpeed = 15f;
    
    [Tooltip("Smoothness of rotation when held")]
    public float rotationSpeed = 10f;
    
    [Header("Return Settings")]
    [Tooltip("Speed of returning to original position")]
    public float returnSpeed = 5f;

    [Header("Scalpel Properties")]
    [Tooltip("The actual cutting point (drag a child object here)")]
    public Transform tipTransform;
    
    [Tooltip("Radius of the tool's influence")]
    public float toolRadius = 0.05f;
    
    [Tooltip("How fast it pushes geometry")]
    public float deformationStrength = 0.5f;

    [Tooltip("Layer of the objects we can cut")]
    public LayerMask surgicalLayer;

    // State tracking
    private bool isHeld = false;
    private Transform heldByController = null;
    
    // Original position/rotation for returning
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    
    // Cutting
    private float lastCutTime = 0f;
    
    // Components
    private Rigidbody rb;
    private Collider col;

    void Start()
    {
        // Store original transform
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        
        // Get or add Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure Rigidbody
        rb.isKinematic = true;
        rb.useGravity = false;
        
        // Get collider
        col = GetComponent<Collider>();
        
        Debug.Log($"<color=cyan>✓ Scalpel initialized at position: {originalPosition}</color>");
    }

    void Update()
    {
        // Update position/rotation if held
        if (isHeld && heldByController != null)
        {
            UpdateHeldPosition();
            
            if (tipTransform == null) return;

            // Check for objects around the tip
            // We use OverlapSphere so we can 'paint' the deformation
            Collider[] hits = Physics.OverlapSphere(tipTransform.position, toolRadius, surgicalLayer);

            foreach (var hit in hits)
            {
                // Look for the specific surgical script
                // Using GetComponentInParent allows hitting child colliders
                SurgicalMesh target = hit.GetComponentInParent<SurgicalMesh>();
            
                if (target != null)
                {
                    target.Deform(tipTransform.position, deformationStrength);
                }
            }
        }
    }

    public void PickUp(Transform controller)
    {
        if (isHeld) return;
        
        isHeld = true;
        heldByController = controller;
        
        if (col != null) col.enabled = false;
        
        Debug.Log($"<color=green>Picked up scalpel with {controller.name}</color>");
    }

    public void Release()
    {
        if (!isHeld) return;
        
        isHeld = false;
        heldByController = null;
        
        if (col != null) col.enabled = true;
        
        Debug.Log("<color=yellow>Released scalpel</color>");
    }

    private void UpdateHeldPosition()
    {
        Vector3 targetPosition = heldByController.position + heldByController.forward * holdDistance;
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        
        // Use the directions defined in inspector
        Vector3 worldSharpEdge = sharpEdgeDirection; // You might want to transform this if it's local
        Vector3 worldHandle = heldByController.forward;
        
        // Simple look rotation often works best for tools
        Quaternion targetRotation = Quaternion.LookRotation(worldHandle, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    public void ReturnToOriginalPosition()
    {
        if (isHeld) Release();
        StartCoroutine(ReturnAnimation());
    }

    private System.Collections.IEnumerator ReturnAnimation()
    {
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * returnSpeed;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime);
            
            transform.position = Vector3.Lerp(startPosition, originalPosition, t);
            transform.rotation = Quaternion.Slerp(startRotation, originalRotation, t);
            
            yield return null;
        }
        
        transform.position = originalPosition;
        transform.rotation = originalRotation;
    }

    public bool IsHeld() => isHeld;

    void OnDrawGizmosSelected()
    {
        if (tipTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(tipTransform.position, toolRadius);
        }
    }
}