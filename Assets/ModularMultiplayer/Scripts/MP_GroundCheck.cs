using Unity.Netcode;
using UnityEngine;

public class MP_GroundCheck : NetworkBehaviour
{
    [SerializeField] private Transform m_StartingPosition;
    [SerializeField] private LayerMask m_GroundLayer;
    [SerializeField] private float m_CheckDistance = 0.05f;
    [SerializeField] private bool m_Grounded = false;

    public bool Grounded { get => m_Grounded; set => m_Grounded = value; }

    private void Update()
    {
        if (!IsOwner) return;

        Grounded = Physics2D.Raycast(m_StartingPosition.position, Vector3.down, m_CheckDistance, m_GroundLayer);
    }
}