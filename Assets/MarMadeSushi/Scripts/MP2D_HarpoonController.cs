using System;
using Unity.Netcode;
using UnityEngine;

public class MP2D_HarpoonController : NetworkBehaviour
{
    [SerializeField] private float m_RotationOffset = 0f;

    private NetworkVariable<int> m_PlayerControlling = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool m_Controlling = false;

    public override void OnNetworkSpawn()
    {
        m_PlayerControlling.OnValueChanged += OnPlayerControllingChange;
    }

    private void OnPlayerControllingChange(int p_OldValue, int p_NewValue)
    {
        if ((int)NetworkManager.LocalClientId == p_NewValue)
        {
            m_Controlling = true;
        }
        else
        {
            m_Controlling = false;
        }
    }

    private void Update()
    {
        if (m_Controlling)
            Move();
    }

    private void Move()
    {
        transform.rotation = Quaternion.Euler(0f, 0f, m_RotationOffset + GetAngle(transform.position, MP2D_PlayerManager.Instance.PlayerCameraController.PlayerCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.position.z - MP2D_PlayerManager.Instance.PlayerCameraController.PlayerCamera.transform.position.z))));
    }

    private float GetAngle(Vector2 p_A, Vector2 p_B)
    {
        Vector2 direction = p_B - p_A;
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerControllingServerRpc(int p_PlayerId)
    {
        m_PlayerControlling.Value = p_PlayerId;
    }
}
