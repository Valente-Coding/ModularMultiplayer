using Unity.Netcode;
using UnityEngine;

public class MP2D_FreezePlayerMovement : NetworkBehaviour
{

    [ServerRpc(RequireOwnership = false)]
    public void FreezePlayerServerRpc(int p_PlayerId)
    {
        FreezePlayerClientRpc(p_PlayerId);
    }

    [ClientRpc]
    private void FreezePlayerClientRpc(int p_PlayerId)
    {
        if ((int)NetworkManager.Singleton.LocalClientId != p_PlayerId) return;

        MP2D_PlayerManager.Instance.PlayerMovement.IsFrozen = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UnfreezePlayerServerRpc(int p_PlayerId)
    {
        UnfreezePlayerClientRpc(p_PlayerId);
    }

    [ClientRpc]
    private void UnfreezePlayerClientRpc(int p_PlayerId)
    {
        if ((int)NetworkManager.Singleton.LocalClientId != p_PlayerId) return;

        MP2D_PlayerManager.Instance.PlayerMovement.IsFrozen = false;

    }
}
