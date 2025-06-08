using UnityEngine;

public class MP2D_SetPlayerCameraPosition : MonoBehaviour
{
    [SerializeField] private Transform m_PositionTransform;

    public void SetCameraPosition()
    {
        MP2D_PlayerManager.Instance.PlayerCameraController.CameraFixedPosition = m_PositionTransform.position;
    }

    public void ResetCameraPosition()
    {
        MP2D_PlayerManager.Instance.PlayerCameraController.CameraFixedPosition = Vector3.zero;
    }
}
