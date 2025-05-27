using UnityEngine;

public class MP_DespawnNetworkPrefab : MonoBehaviour
{
    public void SetPlayerAllowedToDespawnPrefab(bool p_Allow)
    {
        if (p_Allow)
        {
            if (MP2D_PlayerManager.Instance.HoldingItem == null)
                return;

            MP2D_PlayerManager.Instance.PlayerObjectSpawner.CanDespawnPrefab = true;
        }
        else
        {
            MP2D_PlayerManager.Instance.PlayerObjectSpawner.CanDespawnPrefab = false;
        }
        
    }
}
