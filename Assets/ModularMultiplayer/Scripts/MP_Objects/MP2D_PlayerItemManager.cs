using UnityEngine;

public class MP2D_PlayerItemManager : MonoBehaviour
{

    public void SpawnPlayerItem(GameObject p_Prefab)
    {
        if (MP2D_PlayerManager.Instance.HoldingItem != null) return;

        MP2D_PlayerManager.Instance.PlayerObjectSpawner.SpawnObjectById(
            MP_NetworkedGameObjectManager.Instance.GetPrefabRegistryId(p_Prefab),
            MP2D_PlayerManager.Instance.transform.position,
            MP2D_PlayerManager.Instance.transform.rotation
        );
    }

    public void RemovePlayerItem()
    {
        if (MP2D_PlayerManager.Instance.HoldingItem == null) return;

        MP2D_PlayerManager.Instance.PlayerObjectSpawner.DespawnObject(MP2D_PlayerManager.Instance.HoldingItem);
    }
}
