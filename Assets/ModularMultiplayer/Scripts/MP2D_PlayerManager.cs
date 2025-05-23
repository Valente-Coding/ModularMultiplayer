using Unity.Netcode;
using UnityEngine;
using System.Linq;

public class MP2D_PlayerManager : NetworkBehaviour
{
    [Header("Player Conponents")]
    [SerializeField] private MP2D_PlayerMovement m_PlayerMovement;
    [SerializeField] private MP_RespawnPoint m_PlayerRespawn;
    [SerializeField] private Animator m_Animator;
    [SerializeField] private SpriteRenderer m_Sprite;
    [SerializeField] private Transform m_HoldItemPivot;

    private GameObject m_HoldingItem;

    public static MP2D_PlayerManager Instance { get; private set; }

    private void Start()
    {
        if (!IsOwner) m_Sprite.sortingOrder = -1;

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


    public void Hold(GameObject p_ItemToHoldPrefab, Vector3 p_Offset)
    {
        if (!IsOwner) return;
        if (m_HoldingItem != null) return;

        SpawnAndHoldItemServerRpc(p_ItemToHoldPrefab.GetComponent<NetworkObject>().NetworkObjectId, p_Offset);
    }

    [ServerRpc]
    private void SpawnAndHoldItemServerRpc(ulong prefabNetworkId, Vector3 offset)
    {
        // Find the prefab with the matching NetworkObjectId
        NetworkObject prefab = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs
            .FirstOrDefault(p => p.Prefab.GetComponent<NetworkObject>().NetworkObjectId == prefabNetworkId)?.Prefab.GetComponent<NetworkObject>();

        if (prefab != null)
        {
            // Spawn the object
            NetworkObject spawnedObject = Instantiate(prefab.gameObject).GetComponent<NetworkObject>();
            spawnedObject.Spawn(true);
            
            // Tell the client to handle the spawned object
            AttachHeldItemClientRpc(new NetworkObjectReference(spawnedObject), offset);
        }
    }

    [ClientRpc]
    private void AttachHeldItemClientRpc(NetworkObjectReference objectRef, Vector3 offset)
    {
        if (objectRef.TryGet(out NetworkObject networkObject))
        {
            // Only attach the item if this is the player that should hold it
            if (IsOwner)
            {
                m_HoldingItem = networkObject.gameObject;
                m_HoldingItem.transform.SetParent(m_HoldItemPivot);
                m_HoldingItem.transform.localPosition = offset;
            }
        }
    }

    public void ReleaseHeldItem()
    {
        if (!IsOwner || m_HoldingItem == null) return;
        
        ReleaseItemServerRpc();
    }

    [ServerRpc]
    private void ReleaseItemServerRpc()
    {
        if (m_HoldingItem != null)
        {
            NetworkObject networkObject = m_HoldingItem.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                ReleaseItemClientRpc(new NetworkObjectReference(networkObject));
            }
        }
    }

    [ClientRpc]
    private void ReleaseItemClientRpc(NetworkObjectReference objectRef)
    {
        if (objectRef.TryGet(out NetworkObject networkObject))
        {
            if (networkObject.gameObject == m_HoldingItem)
            {
                m_HoldingItem.transform.SetParent(null);
                m_HoldingItem = null;
            }
        }
    }
}
