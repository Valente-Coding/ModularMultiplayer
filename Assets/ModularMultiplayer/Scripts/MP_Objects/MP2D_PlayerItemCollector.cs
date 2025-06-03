using UnityEngine;
using UnityEngine.Events;

public class MP2D_PlayerItemCollector : MonoBehaviour
{
    [SerializeField] private GameObject m_ItemToCollect;
    [SerializeField] private UnityEvent m_OnCollectCorrectItem;
    [SerializeField] private UnityEvent m_OnCollectWrongItem;

    public GameObject ItemToCollect { get => m_ItemToCollect; set => m_ItemToCollect = value; }
    public UnityEvent OnCollectCorrectItem { get => m_OnCollectCorrectItem; set => m_OnCollectCorrectItem = value; }
    public UnityEvent OnCollectWrongItem { get => m_OnCollectWrongItem; set => m_OnCollectWrongItem = value; }

    public void CollectItem()
    {
        if (MP2D_PlayerManager.Instance.HoldingItem == null) return;
        if (MP2D_PlayerManager.Instance.HoldingItem.name.Replace("(Clone)", "") != m_ItemToCollect.name)
        {
            m_OnCollectWrongItem?.Invoke();
            return;   
        }


        MP2D_PlayerManager.Instance.PlayerObjectSpawner.DespawnObject(MP2D_PlayerManager.Instance.HoldingItem);
        m_OnCollectCorrectItem?.Invoke();
    }
}
