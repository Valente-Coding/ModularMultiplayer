using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class MP2D_OnPlayerTrigger : NetworkBehaviour
{
    [SerializeField] private string m_PlayerTag = "Player";
    [SerializeField] private bool m_ServerOnly = true;
    [SerializeField] private bool m_LocalPlayerOnly = false;
    [SerializeField] private UnityEvent m_EnterEvents;
    [SerializeField] private UnityEvent m_ExitEvents;

    void OnTriggerEnter2D(Collider2D collision)
    {
        HandleTriggerEvent(collision, true);
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        HandleTriggerEvent(collision, false);
    }

    private void HandleTriggerEvent(Collider2D collision, bool isEnter)
    {
        // Check if the colliding object has the correct tag
        if (!collision.CompareTag(m_PlayerTag)) return;

        // Get the NetworkObject component from the colliding object
        NetworkObject networkObject = collision.GetComponent<NetworkObject>();
        if (networkObject == null) return;

        // Handle different multiplayer scenarios
        if (m_ServerOnly)
        {
            // Only execute on the server
            if (!IsServer) return;
        }
        else if (m_LocalPlayerOnly)
        {
            // Only execute for the local player
            if (!networkObject.IsOwner) return;
        }
        else
        {
            // Execute on all clients, but we might want to prevent duplicates
            // by only executing on the server and then using ClientRpc if needed
            if (!IsServer) return;
        }

        // Invoke the appropriate events
        if (isEnter)
        {
            m_EnterEvents?.Invoke();
            // Optionally notify all clients about the trigger event
            if (m_ServerOnly && IsServer)
            {
                OnPlayerTriggerEnterClientRpc(networkObject.OwnerClientId);
            }
        }
        else
        {
            m_ExitEvents?.Invoke();
            // Optionally notify all clients about the trigger event
            if (m_ServerOnly && IsServer)
            {
                OnPlayerTriggerExitClientRpc(networkObject.OwnerClientId);
            }
        }
    }

    [ClientRpc]
    private void OnPlayerTriggerEnterClientRpc(ulong playerId)
    {
        // This will be called on all clients when a player enters the trigger
        // You can add client-specific logic here if needed
        // For example: UI updates, sound effects, etc.
    }

    [ClientRpc]
    private void OnPlayerTriggerExitClientRpc(ulong playerId)
    {
        // This will be called on all clients when a player exits the trigger
        // You can add client-specific logic here if needed
    }

    // Alternative method using ServerRpc if you want clients to report trigger events to server
    [ServerRpc(RequireOwnership = false)]
    private void ReportTriggerEventServerRpc(bool isEnter, ulong reportingClientId)
    {
        // Handle the trigger event on the server
        if (isEnter)
        {
            m_EnterEvents?.Invoke();
        }
        else
        {
            m_ExitEvents?.Invoke();
        }
    }
}
