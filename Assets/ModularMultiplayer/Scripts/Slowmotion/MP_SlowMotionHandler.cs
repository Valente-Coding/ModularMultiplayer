using Unity.Netcode;
using UnityEngine;

public class MP_SlowMotionHandler : NetworkBehaviour
{
    private Rigidbody m_RigidBody;
    private Collider m_Collider;
    
    [SerializeField] private float m_SlowMotionFactor = 0.25f;
    [SerializeField] private PhysicsMaterial m_SlowMotionPhysicMaterial;
    [SerializeField] private float m_BounceScale = 0.5f; // Add this to control bounce intensity
    private PhysicsMaterial m_OriginalPhysicMaterial;
    private bool m_IsInSlowMotion = false;
    private Vector3 m_LastFrameVelocity;
    private float m_OriginalDrag;
    private float m_OriginalAngularDrag;

    private void Start()
    {
        TryGetComponent<Rigidbody>(out m_RigidBody);
        TryGetComponent<Collider>(out m_Collider);
        
        if (m_RigidBody != null && m_Collider != null)
        {
            m_OriginalPhysicMaterial = m_Collider.sharedMaterial;
        }
    }

    private float GetCurrentBounciness()
    {
        if (m_Collider != null && m_Collider.sharedMaterial != null)
        {
            return m_Collider.sharedMaterial.bounciness;
        }
        return 0.6f; // Default bounciness if material not found
    }

    void FixedUpdate()
    {
        if (m_RigidBody == null) return;
        
        if (m_IsInSlowMotion)
        {
            // Calculate velocity delta from physics (includes gravity, collisions, etc)
            Vector3 velocityDelta = m_RigidBody.linearVelocity - m_LastFrameVelocity;
            
            // Detect bounces - when velocity changes direction significantly
            if (Vector3.Dot(m_LastFrameVelocity.normalized, m_RigidBody.linearVelocity.normalized) < -0.2f)
            {
                // This is likely a bounce - use material's bounciness value but scale it
                float bounciness = GetCurrentBounciness() * m_BounceScale;
                
                // Apply a more controlled bounce effect
                Vector3 reflectionDirection = Vector3.Reflect(m_LastFrameVelocity.normalized, m_RigidBody.linearVelocity.normalized);
                float magnitude = m_RigidBody.linearVelocity.magnitude * (1f + bounciness * m_SlowMotionFactor);
                
                m_RigidBody.linearVelocity = reflectionDirection * magnitude;
                
                Debug.Log($"Bounce detected! Applied scaled bounciness: {bounciness}");
            }
            else
            {
                // Regular slow motion for non-bounce motion
                m_RigidBody.linearVelocity += velocityDelta * (m_SlowMotionFactor - 1f);
            }
            
            // Store current velocity for next frame's calculation
            m_LastFrameVelocity = m_RigidBody.linearVelocity;
        }
        else
        {
            // Update last frame velocity even when not in slow motion
            // This ensures we have the correct velocity when entering slow motion
            m_LastFrameVelocity = m_RigidBody.linearVelocity;
        }
    }

    // Add this method to manually call from inspector or other scripts
    public void ToggleSlowMotion()
    {
        if (m_IsInSlowMotion)
            ExitSlowMotionServerRpc();
        else
            EnterSlowMotionServerRpc();
    }

    // Add input handling
    private void Update()
    {
        if (!IsOwner) return;
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleSlowMotion();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void EnterSlowMotionServerRpc()
    {
        Debug.Log("Entering slow motion");
        EnterSlowMotionClientRpc();
    }

    [ClientRpc]
    private void EnterSlowMotionClientRpc()
    {
        if (m_RigidBody != null)
        {
            // Store current velocity
            m_LastFrameVelocity = m_RigidBody.linearVelocity;
            
            // Increase drag for slow motion effect
            m_OriginalDrag = m_RigidBody.linearDamping;
            m_OriginalAngularDrag = m_RigidBody.angularDamping;
            m_RigidBody.linearDamping = m_OriginalDrag / m_SlowMotionFactor;
            m_RigidBody.angularDamping = m_OriginalAngularDrag / m_SlowMotionFactor;
            
            // Apply slow motion physic material if specified
            if (m_SlowMotionPhysicMaterial != null && m_RigidBody.GetComponent<Collider>() is Collider collider)
            {
                collider.sharedMaterial = m_SlowMotionPhysicMaterial;
            }
        }
        
        m_IsInSlowMotion = true;
        Debug.Log("Slow motion activated");
    }

    [ServerRpc(RequireOwnership = false)]
    public void ExitSlowMotionServerRpc()
    {
        Debug.Log("Exiting slow motion");
        ExitSlowMotionClientRpc();
    }

    [ClientRpc]
    private void ExitSlowMotionClientRpc()
    {
        m_IsInSlowMotion = false;
        
        if (m_RigidBody != null)
        {
            // Restore original drag values
            m_RigidBody.linearDamping = m_OriginalDrag;
            m_RigidBody.angularDamping = m_OriginalAngularDrag;
            
            // Restore original physic material
            if (m_OriginalPhysicMaterial != null && m_RigidBody.GetComponent<Collider>() is Collider collider)
            {
                collider.sharedMaterial = m_OriginalPhysicMaterial;
            }
        }
        Debug.Log("Slow motion deactivated");
    }
}