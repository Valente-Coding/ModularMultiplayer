using Unity.Netcode;
using UnityEngine;

public class MP_ChangePlayerAnimator : NetworkBehaviour
{
    [SerializeField] private string m_ParamName;

    [ServerRpc(RequireOwnership = false)]
    public void SetParamBoolValueToTrueServerRpc(int p_PlayerId)
    {
        SetParamBoolValueClientRpc(p_PlayerId, true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetParamBoolValueToFalseServerRpc(int p_PlayerId)
    {
        SetParamBoolValueClientRpc(p_PlayerId, false);
    }

    [ClientRpc]
    public void SetParamBoolValueClientRpc(int p_PlayerId, bool p_Value)
    {
        if ((int)NetworkManager.Singleton.LocalClientId != p_PlayerId) return;

        MP2D_PlayerManager.Instance.PlayerAnimator.SetBool(m_ParamName, p_Value);
    }
}
