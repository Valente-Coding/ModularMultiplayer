using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class MP2D_PlayerObjectConversion : NetworkBehaviour
{
    [SerializeField] private SO_ObjectConversion m_ConversionData;
    [SerializeField] private UnityEvent<int> m_OnInsertObject;
    [SerializeField] private UnityEvent<int> m_OnReadyObject;
    [SerializeField] private UnityEvent<int> m_OnTakeObject;

    private System.DateTime m_EpochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);

    private NetworkVariable<int> m_ConvertionTimeEnd = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> m_Converting = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {

    }

    private bool HasInputPrefab(GameObject p_Object)
    {
        if (m_ConversionData.InputPrefab == null) return false;

        return p_Object.name.Replace("(Clone)", "") == m_ConversionData.InputPrefab.name;
    }

    private bool IsConversionComplete()
    {
        int l_CurrentTime = (int)(System.DateTime.UtcNow - m_EpochStart).TotalSeconds;
        return l_CurrentTime >= m_ConvertionTimeEnd.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartObjectConversionServerRpc(int p_PlayerId)
    {
        int l_CurrentTime = (int)(System.DateTime.UtcNow - m_EpochStart).TotalSeconds;
        m_ConvertionTimeEnd.Value = l_CurrentTime + m_ConversionData.ConversionTimeInSeconds;
        m_Converting.Value = true;
        m_OnInsertObject?.Invoke(p_PlayerId);

        StartCoroutine(SetConversionReady(p_PlayerId));
    }

    [ServerRpc(RequireOwnership = false)]
    private void ResetObjectConversionServerRpc(int p_PlayerId)
    {
        m_Converting.Value = false;
        m_OnTakeObject?.Invoke(p_PlayerId);
    }

    private void TakeOutputPrefab()
    {
        if (m_ConversionData.OutputPrefab == null) return;
        if (!IsConversionComplete()) return;
        if (MP2D_PlayerManager.Instance.HoldingItem != null) return;

        MP2D_PlayerManager.Instance.PlayerObjectSpawner.SpawnObjectById(
            MP_NetworkedGameObjectManager.Instance.GetPrefabRegistryId(m_ConversionData.OutputPrefab),
            MP2D_PlayerManager.Instance.transform.position,
            MP2D_PlayerManager.Instance.transform.rotation
        );

        ResetObjectConversionServerRpc((int)NetworkManager.Singleton.LocalClientId);
    }

    private void InsertInputPrefab()
    {
        if (m_ConversionData.InputPrefab == null) return;
        if (MP2D_PlayerManager.Instance.HoldingItem == null) return;
        if (!HasInputPrefab(MP2D_PlayerManager.Instance.HoldingItem.gameObject)) return;

        MP2D_PlayerManager.Instance.PlayerObjectSpawner.DespawnObject(MP2D_PlayerManager.Instance.HoldingItem);

        StartObjectConversionServerRpc((int)NetworkManager.Singleton.LocalClientId);
    }

    public void OnPlayerInteraction()
    {
        if (m_Converting.Value)
            TakeOutputPrefab();
        else
            InsertInputPrefab();
    }

    private IEnumerator SetConversionReady(int p_PlayerId)
    {
        yield return new WaitForSeconds(m_ConversionData.ConversionTimeInSeconds);

        m_OnReadyObject?.Invoke(p_PlayerId);
    }

}
