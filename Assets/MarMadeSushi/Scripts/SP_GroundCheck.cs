using System.Collections.Generic;
using UnityEngine;

public class SP_GroundCheck : MonoBehaviour
{
    [SerializeField] private List<Transform> m_CheckPositions;
    [SerializeField] private LayerMask m_GroundLayer;
    [SerializeField] private float m_CheckDistance = 0.05f;
    [SerializeField] private bool m_Grounded = false;

    public bool Grounded { get => m_Grounded; set => m_Grounded = value; }

    private void Update()
    {
        bool l_FoundGround = false;
        foreach (Transform l_Position in m_CheckPositions)
            if (Physics2D.Raycast(l_Position.position, Vector3.down, m_CheckDistance, m_GroundLayer))
                l_FoundGround = true;

        Grounded = l_FoundGround;
    }
}