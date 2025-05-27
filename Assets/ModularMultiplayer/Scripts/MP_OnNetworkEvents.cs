using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class MP_OnNetworkEvents : NetworkBehaviour
{
    [SerializeField] private UnityEvent m_OnNetworkStartEvents;
    [SerializeField] private UnityEvent m_OnNetworkStopEvents;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        m_OnNetworkStartEvents?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        m_OnNetworkStartEvents?.Invoke();
    }
}