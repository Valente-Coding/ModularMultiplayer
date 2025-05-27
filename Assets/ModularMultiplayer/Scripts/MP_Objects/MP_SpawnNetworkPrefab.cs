using UnityEngine;

public class MP_SpawnNetworkPrefab : MonoBehaviour
{
    [SerializeField] private GameObject m_PrefabToSpawn;

    public void SetPlayerAllowedToSpawnPrefab(bool p_Allow)
    {
        if (p_Allow)
        {
            if (MP2D_PlayerManager.Instance.HoldingItem != null)
                return;

            MP2D_PlayerManager.Instance.PlayerObjectSpawner.SpawnPrefabId = MP_NetworkedGameObjectManager.Instance.GetPrefabRegistryId(m_PrefabToSpawn);
            MP2D_PlayerManager.Instance.PlayerObjectSpawner.CanSpawnPrefab = true;
        }
        else
        {
            MP2D_PlayerManager.Instance.PlayerObjectSpawner.SpawnPrefabId = -1;
            MP2D_PlayerManager.Instance.PlayerObjectSpawner.CanSpawnPrefab = false;
        }
        
    }
}
