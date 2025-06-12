using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MP2D_TableManager : NetworkBehaviour
{
    [SerializeField] private List<MP2D_TableClientManager> m_TableClients;
    [SerializeField] private Collider2D m_InteractionCollider;

    private int m_ReadyToAsk = 0;
    private int m_ReadyToCheck = 0;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        m_TableClients.ForEach(m_Client =>
        {
            m_Client.OnClientStateChange.AddListener(OnClientStateChange);
        });
    }

    private void OnClientStateChange(MP2D_TableClientManager.TableClientState p_ClientNewState)
    {
        if (MP2D_TableClientManager.TableClientState.ReadyToAsk == p_ClientNewState)
            m_ReadyToAsk++;

        if (MP2D_TableClientManager.TableClientState.ReadyToCheck == p_ClientNewState)
            m_ReadyToCheck++;

        if (m_ReadyToAsk >= m_TableClients.Count || m_ReadyToCheck >= m_TableClients.Count)
            SetTableInteractionColliderServerRpc(true);
    }

    public bool AreAllClientsReadyToPay()
    {
        bool l_Ready = true;

        m_TableClients.ForEach(m_Client =>
        {
            if (m_Client.CurrentState.Value != MP2D_TableClientManager.TableClientState.ReadyToCheck)
                l_Ready = false;
        });

        return l_Ready;
    }

    public void OnInteract()
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
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetTableInteractionColliderServerRpc(bool m_State)
    {
        m_InteractionCollider.enabled = m_State;
    }
}
