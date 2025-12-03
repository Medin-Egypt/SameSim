using UnityEngine;

public class VRElasticInteraction : MonoBehaviour
{
    [Header("VR Controller Transforms")]
    public Transform leftControllerTransform;
    public Transform rightControllerTransform;
    
    [Header("Interaction Settings")]
    public float pushDepth = 0.05f;
    public float pullDepth = -0.05f;
    public LayerMask interactionLayer;
    public float raycastDistance = 0.5f;
    
    [Header("Scalpel Interaction")]
    public LayerMask scalpelLayer;
    
    [Header("Visual Feedback")]
    public bool showDebugRays = true;
    public Color leftRayColor = Color.blue;
    public Color rightRayColor = Color.red;
    public Color hitColor = new Color(1f, 0.5f, 0f);
    public Color activeColor = Color.green;
    public Color scalpelHitColor = Color.cyan;
    
    [Header("Input Settings")]
    [Tooltip("Use mouse buttons for testing")]
    public bool useMouseInput = true;
    
    [Tooltip("Use VR controller grip/trigger")]
    public bool useVRInput = true;

    // Scalpel tracking
    private ScalpelController heldScalpel = null;
    private bool wasLeftClickLastFrame = false;

    void Update()
    {
        bool isLeftClick = false;
        
        // Mouse input (for testing)
        if (useMouseInput)
        {
            isLeftClick = Input.GetMouseButton(0);   // Left click
        }

        // Detect left click press (not hold)
        bool isLeftClickNow = isLeftClick;
        bool justPressed = isLeftClickNow && !wasLeftClickLastFrame;
        
        if (justPressed)
        {
            // Priority: Check if holding scalpel first
            if (heldScalpel != null)
            {
                // Already holding scalpel - return it
                heldScalpel.ReturnToOriginalPosition();
                heldScalpel = null;
                Debug.Log("<color=yellow>Returned scalpel to original position</color>");
            }
            else
            {
                // Not holding scalpel - try to pick up or interact with muscle
                bool pickedUpScalpel = TryPickupScalpel(rightControllerTransform);
                
                // If didn't pick up scalpel, do nothing (muscle interaction is on hold)
            }
        }
        
        wasLeftClickLastFrame = isLeftClickNow;

        // Show controller rays
        if (leftControllerTransform != null)
        {
            ShowControllerRay(leftControllerTransform, leftRayColor);
        }
        
        if (rightControllerTransform != null)
        {
            ShowControllerRay(rightControllerTransform, rightRayColor);
        }
        
        // Handle continuous muscle deformation (hold left click)
        if (heldScalpel == null && isLeftClick && !justPressed)
        {
            // Left click is being held (not just pressed)
            HandleMuscleInteraction(rightControllerTransform);
        }
    }

    bool TryPickupScalpel(Transform controller)
    {
        if (controller == null) return false;

        Ray ray = new Ray(controller.position, controller.forward);
        RaycastHit hit;

        // Check for scalpel
        if (Physics.Raycast(ray, out hit, raycastDistance, scalpelLayer))
        {
            ScalpelController scalpel = hit.collider.GetComponent<ScalpelController>();
            
            if (scalpel != null && !scalpel.IsHeld())
            {
                scalpel.PickUp(controller);
                heldScalpel = scalpel;
                Debug.Log("<color=cyan>Picked up scalpel with RIGHT controller!</color>");
                return true;
            }
        }
        
        return false;
    }

    void HandleMuscleInteraction(Transform controller)
    {
        if (controller == null) return;
        
        Ray ray = new Ray(controller.position, controller.forward);
        RaycastHit hit;

        // Check for elastic mesh
        if (Physics.Raycast(ray, out hit, raycastDistance, interactionLayer))
        {
            ElasticMesh mesh = hit.collider.GetComponent<ElasticMesh>();
            
            if (mesh != null)
            {
                mesh.ApplyDeformation(hit.point, pushDepth);
            }
        }
    }

    void ShowControllerRay(Transform controller, Color defaultRayColor)
    {
        if (!showDebugRays) return;
        
        Ray ray = new Ray(controller.position, controller.forward);
        RaycastHit hit;

        Color currentColor = defaultRayColor;
        
        // Check what we're pointing at
        bool hitScalpel = Physics.Raycast(ray, out hit, raycastDistance, scalpelLayer);
        bool hitMuscle = !hitScalpel && Physics.Raycast(ray, out hit, raycastDistance, interactionLayer);
        
        if (hitScalpel)
        {
            currentColor = scalpelHitColor; // Cyan for scalpel
            Debug.DrawLine(controller.position, hit.point, currentColor);
        }
        else if (hitMuscle)
        {
            ElasticMesh mesh = hit.collider.GetComponent<ElasticMesh>();
            if (mesh != null)
            {
                // Check if actively deforming
                if (Input.GetMouseButton(0) && heldScalpel == null)
                {
                    currentColor = activeColor; // Green when pushing
                }
                else
                {
                    currentColor = hitColor; // Orange when hovering
                }
                Debug.DrawLine(controller.position, hit.point, currentColor);
                DrawDebugSphere(hit.point, 0.02f, currentColor);
            }
        }
        else
        {
            Debug.DrawRay(controller.position, controller.forward * raycastDistance, defaultRayColor);
        }
    }

    void DrawDebugSphere(Vector3 center, float radius, Color color)
    {
        Debug.DrawLine(center + Vector3.up * radius, center - Vector3.up * radius, color);
        Debug.DrawLine(center + Vector3.right * radius, center - Vector3.right * radius, color);
        Debug.DrawLine(center + Vector3.forward * radius, center - Vector3.forward * radius, color);
    }

    private void OnDrawGizmos()
    {
        if (!showDebugRays) return;

        if (leftControllerTransform != null)
        {
            Gizmos.color = leftRayColor;
            Gizmos.DrawRay(leftControllerTransform.position, leftControllerTransform.forward * raycastDistance);
            Gizmos.DrawWireSphere(leftControllerTransform.position + leftControllerTransform.forward * raycastDistance, 0.02f);
        }

        if (rightControllerTransform != null)
        {
            Gizmos.color = rightRayColor;
            Gizmos.DrawRay(rightControllerTransform.position, rightControllerTransform.forward * raycastDistance);
            Gizmos.DrawWireSphere(rightControllerTransform.position + rightControllerTransform.forward * raycastDistance, 0.02f);
        }
    }
}