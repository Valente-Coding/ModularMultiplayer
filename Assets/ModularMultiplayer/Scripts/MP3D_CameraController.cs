using UnityEngine;

/// <summary>
/// Controls the camera movement and follows the player.
/// This script should be attached to the main camera or a camera parent object.
/// </summary>
public class MP3D_CameraController : MonoBehaviour
{
    #region Serialized Fields
    [Header("Follow Settings")]
    [SerializeField] private Transform m_Target;
    [SerializeField] private float m_FollowSpeed = 5f;
    [SerializeField] private float m_RotationSpeed = 3f;
    [SerializeField] private Vector3 m_Offset = new Vector3(0, 1, -5);
    
    [Header("Zoom Settings")]
    [SerializeField] private float m_ZoomSpeed = 1f;
    [SerializeField] private float m_MinZoomDistance = 2f;
    [SerializeField] private float m_MaxZoomDistance = 10f;
    
    [Header("Collision Settings")]
    [SerializeField] private bool m_EnableCollision = true;
    [SerializeField] private float m_CollisionRadius = 0.2f;
    [SerializeField] private LayerMask m_CollisionLayers;
    #endregion

    #region Private Fields
    private Vector3 m_CurrentOffset;
    private float m_CurrentZoomDistance;
    private Vector3 m_DesiredPosition;
    private Quaternion m_DesiredRotation;
    private Vector3 m_FinalPosition;
    private Vector2 m_CameraRotation;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Initialize values
        m_CurrentOffset = m_Offset;
        m_CurrentZoomDistance = -m_Offset.z;
        m_CameraRotation = new Vector2(0, 0);
        
        // Lock cursor for first-person mode
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void LateUpdate()
    {
        if (m_Target == null) return;
        
        // Handle input for camera rotation and zoom
        HandleInput();
        
        // Calculate desired rotation
        m_DesiredRotation = Quaternion.Euler(m_CameraRotation.x, m_CameraRotation.y, 0);
        
        // Calculate camera position based on target and offset
        m_CurrentOffset.z = -m_CurrentZoomDistance;
        m_DesiredPosition = m_Target.position + m_DesiredRotation * m_CurrentOffset;
        
        // Check for collision and adjust position if needed
        if (m_EnableCollision)
        {
            m_FinalPosition = HandleCollision();
        }
        else
        {
            m_FinalPosition = m_DesiredPosition;
        }
        
        // Smoothly move camera to desired position and rotation
        transform.position = Vector3.Lerp(transform.position, m_FinalPosition, Time.deltaTime * m_FollowSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, m_DesiredRotation, Time.deltaTime * m_RotationSpeed);
        m_Target.rotation = Quaternion.Euler(m_Target.rotation.x, m_CameraRotation.y, m_Target.rotation.z);
    }
    #endregion

    #region Camera Control
    /// <summary>
    /// Handle input for camera control
    /// </summary>
    private void HandleInput()
    {
        // Mouse input for rotation
        float mouseX = Input.GetAxis("Mouse X") * m_RotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * m_RotationSpeed;
        
        // Update rotation with mouse input
        m_CameraRotation.y += mouseX;
        m_CameraRotation.x -= mouseY;
        
        // Clamp vertical rotation to prevent flipping
        m_CameraRotation.x = Mathf.Clamp(m_CameraRotation.x, -80f, 80f);
        
        // Mouse wheel input for zoom
        float scrollWheel = Input.GetAxis("Mouse ScrollWheel") * m_ZoomSpeed;
        m_CurrentZoomDistance = Mathf.Clamp(m_CurrentZoomDistance - scrollWheel, m_MinZoomDistance, m_MaxZoomDistance);
    }
    
    /// <summary>
    /// Handle camera collision with environment
    /// </summary>
    private Vector3 HandleCollision()
    {
        RaycastHit hit;
        Vector3 direction = m_DesiredPosition - m_Target.position;
        float distance = direction.magnitude;
        
        // Check for collisions between target and desired camera position
        if (Physics.SphereCast(m_Target.position, m_CollisionRadius, direction.normalized, out hit, distance, m_CollisionLayers))
        {
            // Return position at hit point, offset by collision radius
            return m_Target.position + direction.normalized * (hit.distance - m_CollisionRadius);
        }
        
        // No collision, return desired position
        return m_DesiredPosition;
    }
    
    /// <summary>
    /// Set a new target for the camera to follow
    /// </summary>
    /// <param name="p_Target">The transform to follow</param>
    public void SetTarget(Transform p_Target)
    {
        m_Target = p_Target;
    }
    #endregion
}
