using UnityEngine;

public class SP_RespawnPoint : MonoBehaviour
{
    [SerializeField] private Transform m_RespawnTransform;

    public void Respawn()
    {
        transform.position = m_RespawnTransform.position;
        transform.rotation = m_RespawnTransform.rotation;
    }
}