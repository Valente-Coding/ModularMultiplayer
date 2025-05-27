using Unity.Netcode;
using UnityEngine;

public class MP2D_PlayerManager : NetworkBehaviour
{
    [Header("Player Conponents")]
    [SerializeField] private MP2D_PlayerMovement m_PlayerMovement;
    [SerializeField] private MP_RespawnPoint m_PlayerRespawn;
    [SerializeField] private MP_NetworkedObjectSpawner m_PlayerObjectSpawner;
    [SerializeField] private Animator m_Animator;
    [SerializeField] private SpriteRenderer m_Sprite;
    [SerializeField] private Transform m_HoldItemPivot;

    private NetworkObject m_HoldingItem;

    public static MP2D_PlayerManager Instance { get; private set; }
    public Transform HoldItemPivot { get => m_HoldItemPivot; set => m_HoldItemPivot = value; }
    public NetworkObject HoldingItem { get => m_HoldingItem; set => m_HoldingItem = value; }
    public MP_NetworkedObjectSpawner PlayerObjectSpawner { get => m_PlayerObjectSpawner; set => m_PlayerObjectSpawner = value; }

    private void Start()
    {
        if (!IsOwner) m_Sprite.sortingOrder = -2;

        if (!IsOwner) return;

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
        //m_Sprite.flipX = m_PlayerMovement.Velocity.x < 0 ? true : m_PlayerMovement.Velocity.x > 1 ? false : m_Sprite.flipX;
        m_Animator.SetBool("Walking", m_PlayerMovement.Velocity.x != 0 && !m_PlayerMovement.IsRunning);
        m_Animator.SetBool("Running", m_PlayerMovement.IsRunning);
        m_Animator.SetBool("Grounded", m_PlayerMovement.IsGrounded);
        m_Animator.SetBool("Holding", m_HoldingItem != null);
    }
}
