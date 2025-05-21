using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class MP2D_SyncSpriteFlip : NetworkBehaviour
{


    private NetworkVariable<bool> m_FlipSpriteX = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> m_FlipSpriteY = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private SpriteRenderer m_Sprite;

    void Start()
    {
        m_Sprite = GetComponent<SpriteRenderer>();
        
        if (!IsOwner)
        {
            m_Sprite.flipX = m_FlipSpriteX.Value;
            m_Sprite.flipY = m_FlipSpriteY.Value;

            m_FlipSpriteX.OnValueChanged += (bool p_OldValue, bool p_NewValue) => { m_Sprite.flipX = p_NewValue; };
            m_FlipSpriteY.OnValueChanged += (bool p_OldValue, bool p_NewValue) => { m_Sprite.flipY = p_NewValue; };
        }

    }

    void Update()
    {
        if (!IsOwner) return;

        LookForUpdates();
    }

    private void LookForUpdates()
    {
        if (m_Sprite.flipX != m_FlipSpriteX.Value) m_FlipSpriteX.Value = m_Sprite.flipX;
        if (m_Sprite.flipY != m_FlipSpriteY.Value) m_FlipSpriteX.Value = m_Sprite.flipX;
    }
}
