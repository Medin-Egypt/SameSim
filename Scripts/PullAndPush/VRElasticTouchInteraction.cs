using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class VRElasticTouchInteraction : MonoBehaviour
{
    [Header("Deformation Settings")]
    [Tooltip("How deep to push when gripping")]
    public float pushDepth = 0.05f;
    
    [Tooltip("How much to pull when secondary button pressed")]
    public float pullDepth = -0.05f;
    
    [Header("Collider Settings")]
    [Tooltip("Radius of the touch sphere")]
    public float touchRadius = 0.03f;
    
    private ActionBasedController controller;
    private SphereCollider touchCollider;
    private bool isGripPressed;
    private bool isSecondaryPressed;

    void Start()
    {
        // Get the controller component
        controller = GetComponentInParent<ActionBasedController>();
        
        // Setup touch collider
        touchCollider = gameObject.AddComponent<SphereCollider>();
        touchCollider.isTrigger = true;
        touchCollider.radius = touchRadius;
        
        if (controller == null)
        {
            Debug.LogWarning($"VRElasticTouchInteraction on {gameObject.name}: No ActionBasedController found in parent!");
        }
    }

    void Update()
    {
        if (controller != null)
        {
            // Read input from Unity 6's action-based system
            isGripPressed = controller.selectAction.action.IsPressed();
            isSecondaryPressed = controller.uiPressAction.action.IsPressed();
        }
    }

    void OnTriggerStay(Collider other)
    {
        ElasticMesh mesh = other.GetComponent<ElasticMesh>();
        
        if (mesh != null)
        {
            if (isGripPressed)
            {
                // Push
                mesh.ApplyDeformation(transform.position, pushDepth);
                
                // Haptic feedback
                if (controller != null)
                {
                    controller.SendHapticImpulse(0.1f, 0.05f);
                }
            }
            else if (isSecondaryPressed)
            {
                // Pull
                mesh.ApplyDeformation(transform.position, pullDepth);
                
                // Haptic feedback
                if (controller != null)
                {
                    controller.SendHapticImpulse(0.15f, 0.05f);
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, touchRadius);
    }
}