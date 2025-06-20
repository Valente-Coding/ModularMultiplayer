using System.Collections;
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
    private bool m_LeftHanging = false;

    public float SpawnInterval = 0f;
    public float LeaveHangingInterval = 0f;

    public override void OnNetworkSpawn()
    {
        m_InteractionCollider.enabled = m_InteractionColliderActive.Value;

        m_InteractionColliderActive.OnValueChanged += OnInteractionColliderActiveChange;

        if (!IsServer) return;

        m_TableClients.ForEach(m_Client =>
        {
            m_Client.OnClientStateChange.AddListener(OnClientStateChange);
        });

        //SpawnNewClientsServerRpc();
    }

    private void OnInteractionColliderActiveChange(bool p_OldValue, bool p_NewValue)
    {
        m_InteractionCollider.enabled = p_NewValue;
    }

    private void OnClientStateChange(MP2D_TableClientManager.TableClientState p_ClientNewState)
    {
        if (!IsServer) return;
        
        if (MP2D_TableClientManager.TableClientState.ReadyToAsk == p_ClientNewState)
            m_ReadyToAsk++;

        if (MP2D_TableClientManager.TableClientState.ReadyToCheck == p_ClientNewState)
            m_ReadyToCheck++;


        if (m_ReadyToCheck >= m_TableClients.Count)
            SetClientsStateServerRpc(MP2D_TableClientManager.TableClientState.AskToCheck);

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
        m_LeftHanging = false;

        m_TableClients.ForEach(m_Client =>
        {
            m_Client.SetTableClientFoodServerRpc(UnityEngine.Random.Range(0, m_Client.GetFoodChoicesAmount()));
        });

        SetTableInteractionColliderServerRpc(false);
    }

    public void TakeCheck()
    {
        m_ReadyToCheck = 0;

        SetClientsStateServerRpc(MP2D_TableClientManager.TableClientState.None);

        SetTableInteractionColliderServerRpc(false);

        MP_MMS_GameLoop.Instance.AddScoreServerRpc();

        SpawnNewClientsServerRpc();

    }

    [ServerRpc(RequireOwnership = false)]
    private void SetClientsStateServerRpc(MP2D_TableClientManager.TableClientState p_State)
    {
        m_TableClients.ForEach(m_Client =>
        {
            m_Client.SetTableClientStateServerRpc(p_State);
        });
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetTableInteractionColliderServerRpc(bool m_State)
    {
        m_InteractionColliderActive.Value = m_State;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnNewClientsServerRpc()
    {
        m_TableClients.ForEach(m_Client =>
        {
            m_Client.IntervalBetweenClients = SpawnInterval;
            StartCoroutine(m_Client.SpawnTableClient());
        });

        m_LeftHanging = true;
        StartCoroutine(LeftHanging());
    }

    private void DespawnClients()
    {
        m_ReadyToAsk = 0;
        m_ReadyToCheck = 0;

        SetClientsStateServerRpc(MP2D_TableClientManager.TableClientState.None);

        SetTableInteractionColliderServerRpc(false);

        SpawnNewClientsServerRpc();
    }

    public void StopClients()
    {
        m_ReadyToAsk = 0;
        m_ReadyToCheck = 0;

        SetClientsStateServerRpc(MP2D_TableClientManager.TableClientState.None);

        SetTableInteractionColliderServerRpc(false);
    }

    private IEnumerator LeftHanging()
    {
        yield return new WaitForSeconds(LeaveHangingInterval);

        if (m_LeftHanging)
            DespawnClients();
    }
}
