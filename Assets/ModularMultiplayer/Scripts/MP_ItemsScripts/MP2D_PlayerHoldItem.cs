using Unity.Netcode;
using UnityEngine;

public class MP2D_PlayerHoldItem : MonoBehaviour
{
    [SerializeField] private GameObject m_ItemPrefab;

    public void AttachItemToPlayer()
    {
        if (MP2D_PlayerManager.Instance.HoldingItem) return;

        MP_NetworkGameObjects.Instance.RequestSpawnObjectServerRpc(m_ItemPrefab.name, MP2D_PlayerManager.Instance.HoldItemPivot.position, MP2D_PlayerManager.Instance.HoldItemPivot.rotation);
        MP2D_PlayerManager.Instance.HoldingItem = true;
    }
}