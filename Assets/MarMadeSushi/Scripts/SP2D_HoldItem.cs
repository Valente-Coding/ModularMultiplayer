using UnityEngine;

public class SP2D_HoldItem : MonoBehaviour
{
    [SerializeField] private GameObject m_ItemPrefab;
    [SerializeField] private Vector3 m_ItemOffset;

    public void MakePlayerHoldItem()
    {
        MP2D_PlayerManager.Instance.Hold(m_ItemPrefab, m_ItemOffset);
    }
}