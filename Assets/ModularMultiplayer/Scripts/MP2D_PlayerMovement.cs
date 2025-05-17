using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(MP_GroundCheck))]
public class MP2D_PlayerMovement : NetworkBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private float m_MovementSpeed = 1f;
    [SerializeField] private float m_SprintSpeedMultiplier = 2f;
    [SerializeField] private bool m_CanJump = true;
    [SerializeField] private float m_JumpForce = 15f;
    [SerializeField] private float m_Gravity = -9.81f;

    private Rigidbody2D m_RB;
    private MP_GroundCheck m_GroundCheck;
    private Vector2 m_Velocity = Vector2.zero;
    private bool m_Jumping = false;

    private void Start()
    {
        if (!IsOwner) return;

        m_RB = GetComponent<Rigidbody2D>();
        m_GroundCheck = GetComponent<MP_GroundCheck>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (m_CanJump)
            Jump(Input.GetKeyDown(KeyCode.Space), Input.GetKeyUp(KeyCode.Space), m_GroundCheck.Grounded);
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        Move(Input.GetAxis("Horizontal"), Input.GetKey(KeyCode.LeftShift));

        m_RB.linearVelocity = m_Velocity * Time.fixedDeltaTime;
    }

    private void Move(float p_HorizontalInput, bool p_IsSprinting)
    {
        m_Velocity.x = p_HorizontalInput * m_MovementSpeed * (p_IsSprinting ? m_SprintSpeedMultiplier : 1f);
    }

    private void Jump(bool p_IsJumping, bool p_IsCancelingJump, bool p_Grounded)
    {
        if (p_IsJumping && p_Grounded)
            m_Jumping = true;
        else if (p_IsCancelingJump)
            m_Jumping = false;

        m_Velocity.y += p_IsJumping && p_Grounded ? m_JumpForce : 0f + (!p_Grounded ? m_Gravity : 0f);
        m_Velocity.y *= p_IsCancelingJump ? 0.5f : 1f;
        m_Velocity.y = !m_Jumping && p_Grounded ? 0f : m_Velocity.y;
    }
}