using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MP2D_HarpoonController : NetworkBehaviour
{
    [Serializable]
    private struct HarpoonTargets
    {
        public string TargetTag;
        public GameObject TargetPrefab;
    }

    [SerializeField] private float m_RotationOffset = 0f;
    [SerializeField] private float m_AngleTolerance = 1f;
    [SerializeField] private SP2D_HarpoonHeadHandler m_HarpoonHead;
    [SerializeField] private List<HarpoonTargets> m_HarpoonTargets;

    private NetworkVariable<int> m_PlayerControlling = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> m_HarpoonHeadHooked = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool m_Controlling = false;
    private float m_LastAngle = 0f;

    public override void OnNetworkSpawn()
    {
        m_PlayerControlling.OnValueChanged += OnPlayerControllingChange;

        if (!IsServer) return;

        m_HarpoonHead.OnRetractEnd.AddListener(OnHarpoonHeadRetracted);
        m_HarpoonHead.OnCollision.AddListener(OnHarpoonHeadCollision);
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

    private void OnHarpoonHeadRetracted()
    {
        m_HarpoonHeadHooked.Value = true;
    }

    private void Update()
    {
        if (m_Controlling && m_HarpoonHeadHooked.Value)
            Move();

        if (m_Controlling && m_HarpoonHeadHooked.Value && Input.GetMouseButtonDown(0))
            Fire();
    }

    private void Move()
    {
        Vector3 l_MouseWorldPos = MP2D_PlayerManager.Instance.PlayerCameraController.PlayerCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.position.z - MP2D_PlayerManager.Instance.PlayerCameraController.PlayerCamera.transform.position.z));
        float l_Angle = m_RotationOffset + GetAngle(transform.position, l_MouseWorldPos);

        if (Mathf.Abs(l_Angle - m_LastAngle) > m_AngleTolerance)
        {
            UpdateRotationServerRpc(l_Angle);
            m_LastAngle = l_Angle;
        }
    }

    private void Fire()
    {
        Vector3 l_MouseWorldPos = MP2D_PlayerManager.Instance.PlayerCameraController.PlayerCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.position.z - MP2D_PlayerManager.Instance.PlayerCameraController.PlayerCamera.transform.position.z));
        l_MouseWorldPos.z = transform.position.z;

        FireHarpoonHeadServerRpc(l_MouseWorldPos);
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateRotationServerRpc(float p_Angle)
    {
        transform.rotation = Quaternion.Euler(0f, 0f, p_Angle);
    }

    private float GetAngle(Vector2 p_A, Vector2 p_B)
    {
        Vector2 l_Direction = p_B - p_A;
        return Mathf.Atan2(l_Direction.y, l_Direction.x) * Mathf.Rad2Deg;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerControllingServerRpc(int p_PlayerId)
    {
        m_PlayerControlling.Value = p_PlayerId;
    }

    [ServerRpc(RequireOwnership = false)]
    private void FireHarpoonHeadServerRpc(Vector3 p_FirePosition)
    {
        m_HarpoonHeadHooked.Value = false;
        m_HarpoonHead.MoveHarpoonHearTowards(p_FirePosition);
    }

    private GameObject GetTargetPrefab(string p_TargetTag)
    {
        return m_HarpoonTargets.Find(l_Target => l_Target.TargetTag == p_TargetTag).TargetPrefab;
    }

    private void OnHarpoonHeadCollision(string p_TargetTag)
    {
        SpawnTargetPrefabClientRpc(p_TargetTag);
    }

    [ClientRpc]
    private void SpawnTargetPrefabClientRpc(string p_PrefabTag)
    {
        if ((int)NetworkManager.LocalClientId != m_PlayerControlling.Value) return;
        if (MP2D_PlayerManager.Instance.HoldingItem != null) return;

        GameObject l_TargetPrefab = GetTargetPrefab(p_PrefabTag);

        int l_ObjectId = MP_NetworkedGameObjectManager.Instance.GetPrefabRegistryId(l_TargetPrefab);
        MP2D_PlayerManager.Instance.PlayerObjectSpawner.SpawnObjectById(
            l_ObjectId,
            MP2D_PlayerManager.Instance.transform.position,
            MP2D_PlayerManager.Instance.transform.rotation
        );
    }
}
