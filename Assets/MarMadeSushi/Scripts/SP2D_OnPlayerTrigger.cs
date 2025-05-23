using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class SP2D_OnPlayerTrigger : MonoBehaviour
{
    [SerializeField] private string m_PlayerTag;
    [SerializeField] private UnityEvent m_EnterEvents;
    [SerializeField] private UnityEvent m_ExitEvents;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == m_PlayerTag) m_EnterEvents?.Invoke();
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == m_PlayerTag) m_ExitEvents?.Invoke();
    }
}
