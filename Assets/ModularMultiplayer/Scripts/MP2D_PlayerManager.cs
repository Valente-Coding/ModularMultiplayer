using Unity.Netcode;
using UnityEngine;

public class MP2D_PlayerManager : NetworkBehaviour
{
    [Header("Player Conponents")]
    [SerializeField] private MP2D_PlayerMovement m_PlayerMovement;
    [SerializeField] private MP_RespawnPoint m_PlayerRespawn;
    [SerializeField] private Animator m_Animator;
    [SerializeField] private SpriteRenderer m_Sprite;

    public void SetRespawnPoint(Transform p_NewTransform)
    {
        if (!IsOwner) return;

        m_PlayerRespawn.RespawnTransform = p_NewTransform;
    }

    public void RespawnPlayer()
    {
        if (!IsOwner) return;

        m_PlayerRespawn.Respawn();
    }

    void Update()
    {
        if (!IsOwner) return;

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
