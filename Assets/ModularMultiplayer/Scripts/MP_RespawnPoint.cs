using UnityEngine;

public class MP_RespawnPoint : MonoBehaviour
{
    [SerializeField] private Transform m_RespawnTransform;

    public Transform RespawnTransform { get => m_RespawnTransform; set => m_RespawnTransform = value; }

    public void Respawn()
    {
        transform.position = m_RespawnTransform.position;
        transform.rotation = m_RespawnTransform.rotation;
    }
}