using UnityEngine;

public class SP2D_PlayerManager : MonoBehaviour
{
    [Header("Player Conponents")]
    [SerializeField] private SP2D_PlayerMovement m_PlayerMovement;
    [SerializeField] private SP_RespawnPoint m_PlayerRespawn;
    [SerializeField] private Animator m_Animator;
    [SerializeField] private SpriteRenderer m_Sprite;

    public static SP2D_PlayerManager Instance { get; private set; }

    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public void RespawnPlayer()
    {
        m_PlayerRespawn.Respawn();
    }

    void Update()
    {
        Animate();
    }

    private void Animate()
    {
        m_Sprite.flipX = m_PlayerMovement.Velocity.x < 0 ? true : m_PlayerMovement.Velocity.x > 1 ? false : m_Sprite.flipX;
        m_Animator.SetBool("Walking", m_PlayerMovement.Velocity.x != 0 && !m_PlayerMovement.IsRunning);
        m_Animator.SetBool("Running", m_PlayerMovement.IsRunning);
        m_Animator.SetBool("Grounded", m_PlayerMovement.IsGrounded);
    }
}
