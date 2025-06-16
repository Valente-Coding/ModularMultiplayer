using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SP2D_MarmadeController : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer m_Sprite;
    [SerializeField] private Animator m_MarmadeAnimator;
    [SerializeField] private Collider2D m_MarmadeCollider;
    [SerializeField] private string m_HarpoonTag;
    [SerializeField] private float m_Speed = 10f;
    [SerializeField] private float m_TimeToRespawn = 10f;
    [SerializeField] private List<Transform> m_TravelingPoints;

    private int m_CurrentPoint = 0;
    private int m_LastPoint = 0;
    private bool m_IsDead = false;
    private NetworkVariable<bool> m_SpriteFlip = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> m_ColliderEnabled = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        m_SpriteFlip.OnValueChanged += OnSpriteFlip;
        m_ColliderEnabled.OnValueChanged += OnColliderEnabled;
    }

    private void OnSpriteFlip(bool p_OldValue, bool p_NewValue)
    {
        m_Sprite.flipX = p_NewValue;
    }

    private void OnColliderEnabled(bool p_OldValue, bool p_NewValue)
    {
        m_MarmadeCollider.enabled = p_NewValue;
    }

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
            m_LastPoint = m_CurrentPoint;
            m_CurrentPoint = m_CurrentPoint + 1 < m_TravelingPoints.Count ? m_CurrentPoint + 1 : 0;

            if (m_SpriteFlip.Value && m_TravelingPoints[m_LastPoint].position.x > m_TravelingPoints[m_CurrentPoint].position.x)
                m_SpriteFlip.Value = false;
            else if (!m_SpriteFlip.Value && m_TravelingPoints[m_LastPoint].position.x < m_TravelingPoints[m_CurrentPoint].position.x)
                m_SpriteFlip.Value = true;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;
        if (collision.tag != m_HarpoonTag) return;

        m_IsDead = true;
        m_MarmadeAnimator.SetBool("Dead", true);
        m_ColliderEnabled.Value = false;

        StartCoroutine(RespawnInSeconds());
    }

    private IEnumerator RespawnInSeconds()
    {
        yield return new WaitForSeconds(m_TimeToRespawn);

        m_MarmadeAnimator.SetBool("Dead", false);
        m_IsDead = false;
        m_ColliderEnabled.Value = true;
    }
}
