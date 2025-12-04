using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // For XRI 3.x

namespace Unity.VRTemplate
{
    /// <summary>
    /// A tooth that stays stuck until pulled with sufficient distance/force.
    /// </summary>
    public class ToothInteractable : XRGrabInteractable
    {
        [Header("Extraction Settings")]
        [Tooltip("Distance the hand must pull to extract the tooth.")]
        [SerializeField] float m_ExtractionDistance = 0.15f;

        [Tooltip("Maximum wiggle amount before extraction.")]
        [SerializeField] float m_WiggleAmount = 0.02f;

        [Tooltip("Force required if using physics (optional, mostly visual here).")]
        [SerializeField] float m_PullForce = 10.0f;

        [Header("Feedback")]
        [SerializeField] ParticleSystem m_BloodEffect;
        [SerializeField] AudioSource m_AudioSource;
        [SerializeField] AudioClip m_PopSound;

        bool m_IsExtracted = false;
        Vector3 m_InitialPosition;
        Quaternion m_InitialRotation;

        private Rigidbody m_Rigidbody;
        
        // Define the constraints *before* extraction
        const RigidbodyConstraints InitialConstraints = 
            RigidbodyConstraints.FreezePositionX | 
            RigidbodyConstraints.FreezePositionY | 
            RigidbodyConstraints.FreezePositionZ | 
            RigidbodyConstraints.FreezeRotationX | 
            RigidbodyConstraints.FreezeRotationY | 
            RigidbodyConstraints.FreezeRotationZ;


        protected override void Awake()
        {
            base.Awake();
            m_InitialPosition = transform.position;
            m_InitialRotation = transform.rotation;
            
            // Initially, we don't want the standard grab to move it freely
            trackPosition = false;
            trackRotation = false;
            
            // --- MODIFICATION START ---
            if (TryGetComponent<Rigidbody>(out m_Rigidbody))
            {
                // Ensure the Rigidbody is non-kinematic to be affected by constraints
                m_Rigidbody.isKinematic = false; 
                // Apply the constraints
                m_Rigidbody.constraints = InitialConstraints;
            }
            // --- MODIFICATION END ---
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            // If already extracted, ensure tracking is on
            if (m_IsExtracted)
            {
                trackPosition = true;
                trackRotation = true;
            }
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                if (isSelected && !m_IsExtracted)
                {
                    // Calculate pull
                    // Get the interactor's attach point
                    var interactor = interactorsSelecting[0];
                    var handPos = interactor.GetAttachTransform(this).position;

                    float distance = Vector3.Distance(handPos, m_InitialPosition);

                    if (distance > m_ExtractionDistance)
                    {
                        Extract();
                    }
                    else
                    {
                        // Wiggle logic: Move slightly towards hand but clamped
                        Vector3 direction = (handPos - m_InitialPosition).normalized;
                        float wiggle = Mathf.Clamp(distance, 0, m_WiggleAmount);
                        // The movement here is *transform* manipulation, bypassing the Rigidbody constraints for the wiggle effect.
                        transform.position = m_InitialPosition + direction * wiggle;
                        
                        // Random vibration/rotation for struggle effect
                        transform.rotation = m_InitialRotation * Quaternion.Euler(Random.insideUnitSphere * distance * 500f);
                    }
                }
            }
        }

        void Extract()
        {
            m_IsExtracted = true;
            trackPosition = true;
            trackRotation = true;

            if (m_BloodEffect != null) m_BloodEffect.Play();
            if (m_AudioSource != null && m_PopSound != null) m_AudioSource.PlayOneShot(m_PopSound);

            if (m_Rigidbody != null)
            {
                // Clear all constraints so it can move freely after extraction
                m_Rigidbody.constraints = RigidbodyConstraints.None;
                
                // Add a little impulse in the pull direction
                var interactor = interactorsSelecting[0];
                Vector3 pullDir = (interactor.GetAttachTransform(this).position - m_InitialPosition).normalized;
                m_Rigidbody.AddForce(pullDir * 2.0f, ForceMode.Impulse);
            }
        }
    }
}