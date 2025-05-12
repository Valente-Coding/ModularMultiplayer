using Unity.Netcode;
using UnityEngine;

public class MP2D_ClientMovement : NetworkBehaviour
{
    [SerializeField] private float m_Speed = 5;
    [SerializeField] private bool m_EnableVerticalMovement = true;
    [SerializeField] private bool m_EnableHorizontalMovement = true;

    private bool m_KeyPressed_A = false;
    private bool m_KeyPressed_D = false;
    private bool m_KeyPressed_W = false;
    private bool m_KeyPressed_S = false;
    private float m_MoveX;
    private float m_MoveY;

    void Update()
    {
        if (!IsOwner || !IsSpawned) return;

        float l_multiplier = m_Speed * Time.deltaTime;

#if ENABLE_INPUT_SYSTEM && NEW_INPUT_SYSTEM_INSTALLED
        if (m_EnableHorizontalMovement)
        {
            m_KeyPressed_A = Keyboard.current.aKey.isPressed;
            m_KeyPressed_D = Keyboard.current.dKey.isPressed;

            if (m_KeyPressed_A || m_KeyPressed_D)
            {
                m_MoveX = (m_KeyPressed_A ? -l_multiplier : 0) + (m_KeyPressed_D ? l_multiplier : 0);

                transform.position += new Vector3(m_MoveX, 0, 0);
            }
        }

        if (m_EnableVerticalMovement)
        {
            m_KeyPressed_W = Keyboard.current.wKey.isPressed;
            m_KeyPressed_S = Keyboard.current.sKey.isPressed;

            if (m_KeyPressed_W || m_KeyPressed_S)
            {
                m_MoveY = (m_KeyPressed_W ? l_multiplier : 0) + (m_KeyPressed_S ? -l_multiplier : 0);

                transform.position += new Vector3(0, m_MoveY, 0);
            }
        }
#else
        if (m_EnableHorizontalMovement)
        {
            m_KeyPressed_A = Input.GetKey(KeyCode.A);
            m_KeyPressed_D = Input.GetKey(KeyCode.D);

            if (m_KeyPressed_A || m_KeyPressed_D)
            {
                m_MoveX = (m_KeyPressed_A ? -l_multiplier : 0) + (m_KeyPressed_D ? l_multiplier : 0);

                transform.position += new Vector3(m_MoveX, 0, 0);
            }
        }


        if (m_EnableVerticalMovement)
        {
            m_KeyPressed_W = Input.GetKey(KeyCode.W);
            m_KeyPressed_S = Input.GetKey(KeyCode.S);

            if (m_KeyPressed_W || m_KeyPressed_S)
            {
                m_MoveY = (m_KeyPressed_W ? l_multiplier : 0) + (m_KeyPressed_S ? -l_multiplier : 0);

                transform.position += new Vector3(0, m_MoveY, 0);
            }
        }
#endif
    }
}
