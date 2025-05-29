using Unity.Netcode;
using UnityEngine;

public class MP2D_PlayerObjectConversion : NetworkBehaviour
{
    [SerializeField] private SO_ObjectConversion m_ConversionData;

    private System.DateTime m_EpochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);

    private NetworkVariable<int> m_ConvertionTimeEnd = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> m_Converting = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private bool HasInputPrefab(GameObject p_Object)
    {
        if (m_ConversionData.InputPrefab == null) return false;

        return p_Object.name == m_ConversionData.InputPrefab.name;
    }

    private bool IsConversionComplete()
    {
        int l_CurrentTime = (int)(System.DateTime.UtcNow - m_EpochStart).TotalSeconds;
        return l_CurrentTime >= m_ConvertionTimeEnd.Value;
    }

    private void StartObjectConversion()
    {
        int l_CurrentTime = (int)(System.DateTime.UtcNow - m_EpochStart).TotalSeconds;
        m_ConvertionTimeEnd.Value = l_CurrentTime + m_ConversionData.ConversionTimeInSeconds;
        m_Converting.Value = true;
    }

    private void TakeOutputPrefab()
    {
        if (!IsOwner) return;
        if (m_ConversionData.OutputPrefab == null) return;
        if (!IsConversionComplete()) return;
        if (MP2D_PlayerManager.Instance.HoldingItem != null) return;

        adaisghdasgdjgajsdg
    }

    private void InsertInputPrefab()
    {
        if (!IsOwner) return;
        if (m_ConversionData.InputPrefab == null) return;
        if (MP2D_PlayerManager.Instance.HoldingItem == null) return;
        if (!HasInputPrefab(MP2D_PlayerManager.Instance.HoldingItem.gameObject)) return;

        StartObjectConversion();
    }

    public void OnPlayerInteraction()
    {
        if (!IsOwner) return;

        if (m_Converting.Value)
            TakeOutputPrefab();
        else
            InsertInputPrefab();
    }
}
