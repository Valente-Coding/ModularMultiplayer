using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SP2D_MarmadeController : NetworkBehaviour
{
    [SerializeField] private Animator m_MarmadeAnimator;
    [SerializeField] private string m_HarpoonTag;
    [SerializeField] private float m_Speed = 10f;
    [SerializeField] private List<Transform> m_TravelingPoints;

    private int m_CurrentPoint = 0;
    private bool m_IsDead = false;

    void Update()
    {
        if (!IsServer) return;

        if (!m_IsDead)
            Move();
    }

    private void Move()
    {
        if (m_TravelingPoints == null || m_TravelingPoints.Count == 0) return;

        Transform l_TargetPoint = m_TravelingPoints[m_CurrentPoint];

        transform.position = Vector2.MoveTowards(transform.position, l_TargetPoint.position, m_Speed * Time.deltaTime);

        if (Vector2.Distance(transform.position, l_TargetPoint.position) < 0.1f)
        {
            m_CurrentPoint = m_CurrentPoint + 1 < m_TravelingPoints.Count ? m_CurrentPoint + 1 : 0;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;
        if (collision.tag != m_HarpoonTag) return;

        m_IsDead = true;
        m_MarmadeAnimator.SetBool("Dead", true);
    }
}
