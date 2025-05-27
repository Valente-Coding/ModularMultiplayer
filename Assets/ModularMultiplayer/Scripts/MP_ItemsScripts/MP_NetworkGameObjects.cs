using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public class MP_NetworkGameObjects : NetworkBehaviour
{
    public static MP_NetworkGameObjects Instance { get; private set; }

    [Header("Network Object Management")]
    [SerializeField] private List<GameObject> networkObjectPrefabs = new List<GameObject>();
    
    // Dictionary to track spawned objects by ID
    private Dictionary<ulong, NetworkObject> spawnedObjects = new Dictionary<ulong, NetworkObject>();
    
    // Dictionary to map prefab names to indices for network spawning
    private Dictionary<string, int> prefabNameToIndex = new Dictionary<string, int>();
    
    // Events for object lifecycle
    public static event Action<NetworkObject> OnNetworkObjectSpawned;
    public static event Action<NetworkObject> OnNetworkObjectDespawned;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize prefab dictionary
        InitializePrefabDictionary();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        
        base.OnNetworkDespawn();
    }

    private void InitializePrefabDictionary()
    {
        prefabNameToIndex.Clear();
        for (int i = 0; i < networkObjectPrefabs.Count; i++)
        {
            if (networkObjectPrefabs[i] != null)
            {
                prefabNameToIndex[networkObjectPrefabs[i].name] = i;
            }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        // Sync existing objects to the newly connected client
        SyncExistingObjectsToClient(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // Clean up any objects owned by the disconnected client
        CleanupClientObjects(clientId);
    }

    #region Object Spawning Methods

    /// <summary>
    /// Spawns a network object by prefab name
    /// </summary>
    public NetworkObject SpawnNetworkObject(string prefabName, Vector3 position, Quaternion rotation, ulong? parentNetworkObjectId = null)
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only server can spawn network objects!");
            return null;
        }

        if (!prefabNameToIndex.TryGetValue(prefabName, out int prefabIndex))
        {
            Debug.LogError($"Prefab '{prefabName}' not found in network object prefabs list!");
            return null;
        }

        return SpawnNetworkObject(prefabIndex, position, rotation, parentNetworkObjectId);
    }

    /// <summary>
    /// Spawns a network object by prefab index
    /// </summary>
    public NetworkObject SpawnNetworkObject(int prefabIndex, Vector3 position, Quaternion rotation, ulong? parentNetworkObjectId = null)
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only server can spawn network objects!");
            return null;
        }

        if (prefabIndex < 0 || prefabIndex >= networkObjectPrefabs.Count || networkObjectPrefabs[prefabIndex] == null)
        {
            Debug.LogError($"Invalid prefab index: {prefabIndex}");
            return null;
        }

        GameObject prefab = networkObjectPrefabs[prefabIndex];
        GameObject instance = Instantiate(prefab, position, rotation);
        
        NetworkObject networkObject = instance.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError($"Prefab '{prefab.name}' doesn't have a NetworkObject component!");
            Destroy(instance);
            return null;
        }

        // Set parent if specified
        if (parentNetworkObjectId.HasValue && spawnedObjects.TryGetValue(parentNetworkObjectId.Value, out NetworkObject parentObject))
        {
            instance.transform.SetParent(parentObject.transform);
        }

        // Spawn the object on the network
        networkObject.Spawn();
        
        // Track the spawned object
        spawnedObjects[networkObject.NetworkObjectId] = networkObject;
        
        // Invoke event
        OnNetworkObjectSpawned?.Invoke(networkObject);
        
        Debug.Log($"Spawned network object: {prefab.name} with ID: {networkObject.NetworkObjectId}");
        return networkObject;
    }

    /// <summary>
    /// Spawns a network object with ownership
    /// </summary>
    public NetworkObject SpawnNetworkObjectWithOwnership(string prefabName, Vector3 position, Quaternion rotation, ulong ownerId)
    {
        NetworkObject networkObject = SpawnNetworkObject(prefabName, position, rotation);
        if (networkObject != null)
        {
            networkObject.ChangeOwnership(ownerId);
        }
        return networkObject;
    }

    #endregion

    #region Object Despawning Methods

    /// <summary>
    /// Despawns a network object by NetworkObject reference
    /// </summary>
    public bool DespawnNetworkObject(NetworkObject networkObject, bool destroy = true)
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only server can despawn network objects!");
            return false;
        }

        if (networkObject == null || !networkObject.IsSpawned)
        {
            return false;
        }

        ulong networkObjectId = networkObject.NetworkObjectId;
        
        // Remove from tracking
        spawnedObjects.Remove(networkObjectId);
        
        // Invoke event before despawning
        OnNetworkObjectDespawned?.Invoke(networkObject);
        
        // Despawn the object
        networkObject.Despawn(destroy);
        
        Debug.Log($"Despawned network object with ID: {networkObjectId}");
        return true;
    }

    /// <summary>
    /// Despawns a network object by ID
    /// </summary>
    public bool DespawnNetworkObject(ulong networkObjectId, bool destroy = true)
    {
        if (spawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
        {
            return DespawnNetworkObject(networkObject, destroy);
        }
        return false;
    }

    #endregion

    #region Object Management Methods

    /// <summary>
    /// Gets a network object by ID
    /// </summary>
    public NetworkObject GetNetworkObject(ulong networkObjectId)
    {
        spawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject);
        return networkObject;
    }

    /// <summary>
    /// Gets all spawned network objects
    /// </summary>
    public Dictionary<ulong, NetworkObject> GetAllSpawnedObjects()
    {
        return new Dictionary<ulong, NetworkObject>(spawnedObjects);
    }

    /// <summary>
    /// Gets network objects owned by a specific client
    /// </summary>
    public List<NetworkObject> GetObjectsOwnedByClient(ulong clientId)
    {
        List<NetworkObject> ownedObjects = new List<NetworkObject>();
        
        foreach (var kvp in spawnedObjects)
        {
            if (kvp.Value.OwnerClientId == clientId)
            {
                ownedObjects.Add(kvp.Value);
            }
        }
        
        return ownedObjects;
    }

    /// <summary>
    /// Transfers ownership of a network object
    /// </summary>
    public bool TransferOwnership(ulong networkObjectId, ulong newOwnerId)
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only server can transfer ownership!");
            return false;
        }

        if (spawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
        {
            networkObject.ChangeOwnership(newOwnerId);
            Debug.Log($"Transferred ownership of object {networkObjectId} to client {newOwnerId}");
            return true;
        }
        
        return false;
    }

    #endregion

    #region Client Synchronization

    private void SyncExistingObjectsToClient(ulong clientId)
    {
        // Existing objects are automatically synced by Unity Netcode
        // This method can be extended for custom synchronization logic
        Debug.Log($"Syncing {spawnedObjects.Count} existing objects to client {clientId}");
    }

    private void CleanupClientObjects(ulong clientId)
    {
        List<NetworkObject> objectsToRemove = GetObjectsOwnedByClient(clientId);
        
        foreach (NetworkObject obj in objectsToRemove)
        {
            if (obj != null && obj.IsSpawned)
            {
                DespawnNetworkObject(obj, true);
            }
        }
        
        Debug.Log($"Cleaned up {objectsToRemove.Count} objects owned by disconnected client {clientId}");
    }

    #endregion

    #region RPC Methods

    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnObjectServerRpc(string prefabName, Vector3 position, Quaternion rotation, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        NetworkObject spawnedObject = SpawnNetworkObjectWithOwnership(prefabName, position, rotation, clientId);
        
        if (spawnedObject != null)
        {
            NotifyObjectSpawnedClientRpc(spawnedObject.NetworkObjectId, prefabName, position, rotation, clientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestDespawnObjectServerRpc(ulong networkObjectId, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        
        if (spawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject))
        {
            // Check if client owns the object or is server
            if (networkObject.OwnerClientId == clientId || clientId == NetworkManager.ServerClientId)
            {
                DespawnNetworkObject(networkObject, true);
            }
        }
    }

    [ClientRpc]
    private void NotifyObjectSpawnedClientRpc(ulong networkObjectId, string prefabName, Vector3 position, Quaternion rotation, ulong ownerId)
    {
        // This is mainly for logging/events on clients
        Debug.Log($"Client notified: Object {prefabName} spawned with ID {networkObjectId} owned by {ownerId}");
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Adds a prefab to the network objects list at runtime
    /// </summary>
    public void AddNetworkObjectPrefab(GameObject prefab)
    {
        if (prefab != null && !networkObjectPrefabs.Contains(prefab))
        {
            networkObjectPrefabs.Add(prefab);
            prefabNameToIndex[prefab.name] = networkObjectPrefabs.Count - 1;
            Debug.Log($"Added prefab '{prefab.name}' to network objects list");
        }
    }

    /// <summary>
    /// Gets the count of currently spawned objects
    /// </summary>
    public int GetSpawnedObjectCount()
    {
        return spawnedObjects.Count;
    }

    /// <summary>
    /// Clears all spawned objects (Server only)
    /// </summary>
    public void ClearAllSpawnedObjects()
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only server can clear all spawned objects!");
            return;
        }

        List<NetworkObject> objectsToRemove = new List<NetworkObject>(spawnedObjects.Values);
        
        foreach (NetworkObject obj in objectsToRemove)
        {
            if (obj != null && obj.IsSpawned)
            {
                DespawnNetworkObject(obj, true);
            }
        }
        
        spawnedObjects.Clear();
        Debug.Log("Cleared all spawned network objects");
    }

    #endregion
}