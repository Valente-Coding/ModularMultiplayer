using UnityEngine;

public class MP_SpawnNetworkPrefab : MonoBehaviour
{
    [SerializeField] private GameObject m_PrefabToSpawn;

    public void SpawnPrefab()
    {
        if (MP2D_PlayerManager.Instance.HoldingItem != null) return;

        MP2D_PlayerManager.Instance.PlayerObjectSpawner.SpawnObjectById(
            MP_NetworkedGameObjectManager.Instance.GetPrefabRegistryId(m_PrefabToSpawn),
            MP2D_PlayerManager.Instance.transform.position,
            MP2D_PlayerManager.Instance.transform.rotation
        );
    }
}
