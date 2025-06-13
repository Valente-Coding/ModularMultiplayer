using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MP2D_TableManager : NetworkBehaviour
{
    [SerializeField] private List<MP2D_TableClientManager> m_TableClients;
    [SerializeField] private Collider2D m_InteractionCollider;

    private NetworkVariable<bool> m_InteractionColliderActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private int m_ReadyToAsk = 0;
    private int m_ReadyToCheck = 0;

    public override void OnNetworkSpawn()
    {
        m_InteractionCollider.enabled = m_InteractionColliderActive.Value;

        m_InteractionColliderActive.OnValueChanged += OnInteractionColliderActiveChange;

        if (!IsServer) return;

        m_TableClients.ForEach(m_Client =>
        {
            m_Client.OnClientStateChange.AddListener(OnClientStateChange);
        });

        SpawnNewClientsServerRpc();
    }

    private void OnInteractionColliderActiveChange(bool p_OldValue, bool p_NewValue)
    {
        m_InteractionCollider.enabled = p_NewValue;
    }

    private void OnClientStateChange(MP2D_TableClientManager.TableClientState p_ClientNewState)
    {
        if (MP2D_TableClientManager.TableClientState.ReadyToAsk == p_ClientNewState)
            m_ReadyToAsk++;

        if (MP2D_TableClientManager.TableClientState.ReadyToCheck == p_ClientNewState)
            m_ReadyToCheck++;


        if (m_ReadyToCheck >= m_TableClients.Count)
            SetCheckClientState();

        if (m_ReadyToAsk >= m_TableClients.Count || m_ReadyToCheck >= m_TableClients.Count)
            SetTableInteractionColliderServerRpc(true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnInteractServerRpc()
    {
        if (m_ReadyToAsk >= m_TableClients.Count)
            TakeOrder();

        if (m_ReadyToCheck >= m_TableClients.Count)
            TakeCheck();
    }

    public void TakeOrder()
    {
        m_ReadyToAsk = 0;

        m_TableClients.ForEach(m_Client =>
        {
            m_Client.SetTableClientFoodServerRpc(UnityEngine.Random.Range(0, m_Client.GetFoodChoicesAmount()));
        });

        SetTableInteractionColliderServerRpc(false);
    }

    public void TakeCheck()
    {
        m_ReadyToCheck = 0;

        m_TableClients.ForEach(m_Client =>
        {
            m_Client.SetTableClientStateServerRpc(MP2D_TableClientManager.TableClientState.None);
        });

        SetTableInteractionColliderServerRpc(false);

        SpawnNewClientsServerRpc();
    }

    private void SetCheckClientState()
    {
        m_TableClients.ForEach(m_Client =>
        {
            m_Client.SetTableClientStateServerRpc(MP2D_TableClientManager.TableClientState.AskToCheck);
        });
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetTableInteractionColliderServerRpc(bool m_State)
    {
        m_InteractionColliderActive.Value = m_State;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnNewClientsServerRpc()
    {
        m_TableClients.ForEach(m_Client =>
        {
            StartCoroutine(m_Client.SpawnTableClient());
        });
    }
}
