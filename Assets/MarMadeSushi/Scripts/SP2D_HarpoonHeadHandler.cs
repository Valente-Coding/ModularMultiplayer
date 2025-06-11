using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class SP2D_HarpoonHeadHandler : MonoBehaviour
{
    [SerializeField] private float m_Speed = 10f;

    private Vector3 m_InicialPosition;
    private Vector3 m_InicialLocalPosition;
    private Vector3 m_FinalPosition = Vector2.zero;
    private bool m_Retracting = false;
    private UnityEvent m_OnRetractEnd = new UnityEvent();
    private UnityEvent<string> m_OnCollision = new UnityEvent<string>();

    public UnityEvent OnRetractEnd { get => m_OnRetractEnd; set => m_OnRetractEnd = value; }
    public UnityEvent<string> OnCollision { get => m_OnCollision; set => m_OnCollision = value; }

    void Start()
    {
        m_InicialPosition = transform.position;
        m_InicialLocalPosition = transform.localPosition;
    }

    void Update()
    {
        if (m_FinalPosition != Vector3.zero)
            Move();
    }

    private void Move()
    {
        transform.position = Vector3.MoveTowards(transform.position, m_FinalPosition, m_Speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, m_FinalPosition) < 0.01f)
        {
            transform.position = m_FinalPosition;

            if (m_Retracting)
            {
                ResetValues();
                m_OnRetractEnd?.Invoke();
            }
            else
            {
                Retract();
            }
        }
    }

    private void Retract()
    {
        Vector3 l_TempPosition = m_InicialPosition;
        m_InicialPosition = m_FinalPosition;
        m_FinalPosition = l_TempPosition;

        m_Retracting = true;
    }

    private void ResetValues()
    {
        m_InicialPosition = m_FinalPosition;
        m_FinalPosition = Vector3.zero;
        m_Retracting = false;
        transform.localPosition = m_InicialLocalPosition;
    }

    public void MoveHarpoonHearTowards(Vector3 p_TargetPosition)
    {
        m_FinalPosition = p_TargetPosition;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        m_OnCollision?.Invoke(collision.tag);
    }
}
