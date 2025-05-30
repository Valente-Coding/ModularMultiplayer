using UnityEngine;

public class MP2D_DeleteHoldingObject : MonoBehaviour
{
    public void DeleteObject()
    {
        if (MP2D_PlayerManager.Instance.HoldingItem == null) return;

        MP2D_PlayerManager.Instance.PlayerObjectSpawner.DespawnObject(MP2D_PlayerManager.Instance.HoldingItem);
    }
}
