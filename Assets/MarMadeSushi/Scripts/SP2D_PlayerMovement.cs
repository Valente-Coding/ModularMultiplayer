using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SP_GroundCheck))]
public class SP2D_PlayerMovement : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private float m_MovementSpeed = 150f;
    [SerializeField] private float m_SprintSpeedMultiplier = 2f;
    [SerializeField] private bool m_CanJump = true;
    [SerializeField] private float m_JumpForce = 500f;
    [SerializeField] private float m_Gravity = -9.81f;

    private Rigidbody2D m_RB;
    private SP_GroundCheck m_GroundCheck;
    private Vector2 m_Velocity = Vector2.zero;
    private bool m_Jumping = false;
    private bool m_IsRunning = false;
    private bool m_IsGrounded = false;

    public Vector2 Velocity { get => m_Velocity; set => m_Velocity = value; }
    public bool IsRunning { get => m_IsRunning; set => m_IsRunning = value; }
    public bool IsGrounded { get => m_IsGrounded; set => m_IsGrounded = value; }

    private void Start()
    {
        m_RB = GetComponent<Rigidbody2D>();
        m_GroundCheck = GetComponent<SP_GroundCheck>();
    }

    private void Update()
    {
        if (m_CanJump)
            Jump(Input.GetKeyDown(KeyCode.Space), Input.GetKeyUp(KeyCode.Space), m_GroundCheck.Grounded);
    }

    void FixedUpdate()
    {
        Move(Input.GetAxis("Horizontal"), Input.GetKey(KeyCode.LeftShift));

        m_RB.linearVelocity = m_Velocity * Time.fixedDeltaTime;
    }

    private void Move(float p_HorizontalInput, bool p_IsSprinting)
    {
        m_IsRunning = p_IsSprinting;
        m_Velocity.x = p_HorizontalInput * m_MovementSpeed * (p_IsSprinting ? m_SprintSpeedMultiplier : 1f);
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