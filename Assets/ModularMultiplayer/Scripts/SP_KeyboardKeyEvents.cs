using UnityEngine;
using UnityEngine.Events;

public class SP_KeyboardKeyEvents : MonoBehaviour
{
    [SerializeField] private KeyCode m_TriggerKey;
    [SerializeField] bool m_Toggle = true;
    [SerializeField] private UnityEvent m_OnTrue;
    [SerializeField] private UnityEvent m_OnFalse;


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(m_TriggerKey))
            ToggleKey();
    }

    private void ToggleKey()
    {
        m_Toggle = !m_Toggle;

        if (m_Toggle)
            m_OnTrue?.Invoke();
        else
            m_OnFalse?.Invoke();
    }
}
