using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(MP_GroundCheck))]
public class MP2D_PlayerMovement : NetworkBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private float m_MovementSpeed = 150f;
    [SerializeField] private float m_SprintSpeedMultiplier = 2f;
    [SerializeField] private bool m_CanJump = true;
    [SerializeField] private bool m_CanSprint = true;
    [SerializeField] private float m_JumpForce = 500f;
    [SerializeField] private float m_Gravity = -2000f;

    private Rigidbody2D m_RB;
    private MP_GroundCheck m_GroundCheck;
    private Vector2 m_Velocity = Vector2.zero;
    private bool m_Jumping = false;
    private bool m_IsRunning = false;
    private bool m_IsGrounded = false;
    private bool m_IsFrozen = false;

    public Vector2 Velocity { get => m_Velocity; set => m_Velocity = value; }
    public bool IsRunning { get => m_IsRunning; set => m_IsRunning = value; }
    public bool IsGrounded { get => m_IsGrounded; set => m_IsGrounded = value; }
    public bool IsFrozen { get => m_IsFrozen; set => m_IsFrozen = value; }

    private void Start()
    {
        if (!IsOwner) return;

        m_RB = GetComponent<Rigidbody2D>();
        m_GroundCheck = GetComponent<MP_GroundCheck>();
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (m_IsFrozen) return;

        Jump(m_CanJump ? Input.GetKeyDown(KeyCode.Space) : false, m_CanJump ? Input.GetKeyUp(KeyCode.Space) : false, m_GroundCheck != null ? m_GroundCheck.Grounded : true);
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        float l_XMovement = !m_IsFrozen ? Input.GetAxis("Horizontal") : 0f;
        bool l_Sprinting = m_CanSprint ? !m_IsFrozen ? Input.GetKey(KeyCode.LeftShift) : false : false;

        Move(l_XMovement, l_Sprinting);

        m_RB.linearVelocity = m_Velocity * Time.fixedDeltaTime;
    }

    private void Move(float p_HorizontalInput, bool p_IsSprinting)
    {
        m_IsRunning = p_IsSprinting;
        m_Velocity.x = p_HorizontalInput * m_MovementSpeed * (p_IsSprinting ? m_SprintSpeedMultiplier : 1f);
        transform.localScale = (p_HorizontalInput < 0 && transform.localScale.x > 0) || (p_HorizontalInput > 0 && transform.localScale.x < 0) ? new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z) : transform.localScale;
    }

    private void Jump(bool p_IsJumping, bool p_IsCancelingJump, bool p_Grounded)
    {
        if (p_IsJumping && p_Grounded)
            m_Jumping = true;
        else if (p_IsCancelingJump)
            m_Jumping = false;

        m_Velocity.y += p_IsJumping && p_Grounded ? m_JumpForce : 0f + (!p_Grounded ? m_Gravity * Time.deltaTime : 0f);
        m_Velocity.y *= p_IsCancelingJump ? 0.5f : 1f;
        m_Velocity.y = !m_Jumping && p_Grounded ? 0f : m_Velocity.y;

        m_IsGrounded = p_Grounded;
    }
}