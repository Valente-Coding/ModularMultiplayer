using UnityEngine;
using UnityEngine.Events;

public class MP2D_PlayerItemCollector : MonoBehaviour
{
    [SerializeField] private GameObject m_ItemToCollect;
    [SerializeField] private UnityEvent m_OnCollect;

    public void CollectItem()
    {
        if (MP2D_PlayerManager.Instance.HoldingItem == null) return;
        if (MP2D_PlayerManager.Instance.HoldingItem.name.Replace("(Clone)", "") != m_ItemToCollect.name) return;


        MP2D_PlayerManager.Instance.PlayerObjectSpawner.DespawnObject(MP2D_PlayerManager.Instance.HoldingItem);
        m_OnCollect?.Invoke();
    }
}
