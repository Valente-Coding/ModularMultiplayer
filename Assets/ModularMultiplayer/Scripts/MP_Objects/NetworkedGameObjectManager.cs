using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Centralized manager for handling GameObjects across multiplayer clients.
/// Provides functionality for spawning, destroying, and synchronizing GameObjects
/// in a multiplayer environment using Unity Netcode for GameObjects.
/// </summary>
public class NetworkedGameObjectManager : NetworkBehaviour
{
    public static NetworkedGameObjectManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private int maxManagedObjects = 1000;

    [Header("Prefab Registry")]
    [SerializeField] private NetworkedObjectRegistry objectRegistry;

    // Events for game object lifecycle
    public event EventHandler<OnGameObjectSpawnedEventArgs> OnGameObjectSpawned;
    public event EventHandler<OnGameObjectDestroyedEventArgs> OnGameObjectDestroyed;
    public event EventHandler<OnOwnershipChangedEventArgs> OnOwnershipChanged;

    public class OnGameObjectSpawnedEventArgs : EventArgs
    {
        public NetworkObject spawnedObject;
        public ulong ownerClientId;
        public int prefabId;
        public Vector3 position;
        public Quaternion rotation;
    }

    public class OnGameObjectDestroyedEventArgs : EventArgs
    {
        public ulong networkObjectId;
        public ulong ownerClientId;
        public int prefabId;
    }

    public class OnOwnershipChangedEventArgs : EventArgs
    {
        public NetworkObject networkObject;
        public ulong previousOwner;
        public ulong newOwner;
    }

    // Network synchronized data
    private NetworkList<ManagedObjectData> managedObjectsList;
    private NetworkVariable<int> totalManagedObjects = new NetworkVariable<int>(0);

    // Local tracking
    private Dictionary<ulong, NetworkObject> networkObjectCache = new Dictionary<ulong, NetworkObject>();
    private Dictionary<NetworkObject, ManagedObjectData> objectDataCache = new Dictionary<NetworkObject, ManagedObjectData>();

    [System.Serializable]
    public struct ManagedObjectData : INetworkSerializable, IEquatable<ManagedObjectData>
    {
        public ulong networkObjectId;
        public ulong ownerClientId;
        public int prefabId;
        public Vector3 position;
        public Quaternion rotation;
        public float spawnTime;
        public bool isActive;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref networkObjectId);
            serializer.SerializeValue(ref ownerClientId);
            serializer.SerializeValue(ref prefabId);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref rotation);
            serializer.SerializeValue(ref spawnTime);
            serializer.SerializeValue(ref isActive);
        }

        public bool Equals(ManagedObjectData other)
        {
            return networkObjectId == other.networkObjectId;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            managedObjectsList = new NetworkList<ManagedObjectData>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        managedObjectsList.OnListChanged += ManagedObjectsList_OnListChanged;
        
        if (IsServer)
        {
            NetworkManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
        }

        DebugLog("NetworkedGameObjectManager spawned on network");
    }

    public override void OnNetworkDespawn()
    {
        managedObjectsList.OnListChanged -= ManagedObjectsList_OnListChanged;
        
        if (IsServer)
        {
            NetworkManager.OnClientDisconnectCallback -= NetworkManager_OnClientDisconnectCallback;
        }
    }

    #region Public API

    /// <summary>
    /// Spawns a networked GameObject with the specified prefab ID at the given position and rotation.
    /// Only callable by server or object owner.
    /// </summary>
    public void SpawnNetworkedObject(int prefabId, Vector3 position, Quaternion rotation, ulong ownerClientId = 0)
    {
        if (ownerClientId == 0)
            ownerClientId = NetworkManager.Singleton.LocalClientId;

        SpawnNetworkedObjectServerRpc(prefabId, position, rotation, ownerClientId);
    }

    /// <summary>
    /// Destroys a networked GameObject by its NetworkObject reference.
    /// </summary>
    public void DestroyNetworkedObject(NetworkObject networkObject)
    {
        if (networkObject == null) return;

        DestroyNetworkedObjectServerRpc(networkObject.NetworkObjectId);
    }

    /// <summary>
    /// Changes ownership of a networked object to a new client.
    /// </summary>
    public void ChangeObjectOwnership(NetworkObject networkObject, ulong newOwnerClientId)
    {
        if (networkObject == null) return;

        ChangeObjectOwnershipServerRpc(networkObject.NetworkObjectId, newOwnerClientId);
    }

    /// <summary>
    /// Gets all managed objects owned by a specific client.
    /// </summary>
    public List<NetworkObject> GetObjectsOwnedByClient(ulong clientId)
    {
        List<NetworkObject> ownedObjects = new List<NetworkObject>();
        
        foreach (var objectData in managedObjectsList)
        {
            if (objectData.ownerClientId == clientId && objectData.isActive && networkObjectCache.ContainsKey(objectData.networkObjectId))
            {
                ownedObjects.Add(networkObjectCache[objectData.networkObjectId]);
            }
        }
        
        return ownedObjects;
    }

    /// <summary>
    /// Gets the total number of active managed objects.
    /// </summary>
    public int GetTotalManagedObjects()
    {
        return totalManagedObjects.Value;
    }

    /// <summary>
    /// Teleports a networked object to a new position.
    /// </summary>
    public void TeleportObject(NetworkObject networkObject, Vector3 newPosition)
    {
        if (networkObject == null) return;

        TeleportObjectServerRpc(networkObject.NetworkObjectId, newPosition);
    }

    /// <summary>
    /// Sets the active state of a networked object.
    /// </summary>
    public void SetObjectActive(NetworkObject networkObject, bool active)
    {
        if (networkObject == null) return;

        SetObjectActiveServerRpc(networkObject.NetworkObjectId, active);
    }

    #endregion

    #region Server RPCs

    [ServerRpc(RequireOwnership = false)]
    private void SpawnNetworkedObjectServerRpc(int prefabId, Vector3 position, Quaternion rotation, ulong ownerClientId)
    {
        if (totalManagedObjects.Value >= maxManagedObjects)
        {
            DebugLogError($"Cannot spawn object: Maximum managed objects limit ({maxManagedObjects}) reached");
            return;
        }

        if (objectRegistry == null || !objectRegistry.IsValidPrefabId(prefabId))
        {
            DebugLogError($"Invalid prefab ID: {prefabId}");
            return;
        }

        GameObject prefab = objectRegistry.GetPrefab(prefabId);
        if (prefab == null)
        {
            DebugLogError($"Prefab not found for ID: {prefabId}");
            return;
        }

        // Instantiate and spawn the object
        GameObject instance = Instantiate(prefab, position, rotation);
        NetworkObject networkObject = instance.GetComponent<NetworkObject>();
        
        if (networkObject == null)
        {
            DebugLogError($"Prefab {prefab.name} does not have a NetworkObject component");
            Destroy(instance);
            return;
        }

        // Spawn with ownership
        networkObject.SpawnWithOwnership(ownerClientId);

        // Create managed object data
        ManagedObjectData objectData = new ManagedObjectData
        {
            networkObjectId = networkObject.NetworkObjectId,
            ownerClientId = ownerClientId,
            prefabId = prefabId,
            position = position,
            rotation = rotation,
            spawnTime = Time.time,
            isActive = true
        };

        // Add to managed list
        managedObjectsList.Add(objectData);
        totalManagedObjects.Value++;

        // Update local cache
        networkObjectCache[networkObject.NetworkObjectId] = networkObject;
        objectDataCache[networkObject] = objectData;

        DebugLog($"Spawned networked object: {prefab.name} (ID: {networkObject.NetworkObjectId}) for client {ownerClientId}");

        MP_FollowTransform l_FollowTransform;
        if (instance.TryGetComponent<MP_FollowTransform>(out l_FollowTransform))
        {
            SetFollowTransformClientRpc(networkObject, ownerClientId);
        }
        
        // Notify clients
        OnObjectSpawnedClientRpc(networkObject.NetworkObjectId, ownerClientId, prefabId, position, rotation);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyNetworkedObjectServerRpc(ulong networkObjectId)
    {
        if (!networkObjectCache.ContainsKey(networkObjectId))
        {
            DebugLogError($"Cannot destroy object: NetworkObject ID {networkObjectId} not found");
            return;
        }

        NetworkObject networkObject = networkObjectCache[networkObjectId];
        ManagedObjectData objectData = objectDataCache[networkObject];

        // Remove from managed list
        for (int i = 0; i < managedObjectsList.Count; i++)
        {
            if (managedObjectsList[i].networkObjectId == networkObjectId)
            {
                managedObjectsList.RemoveAt(i);
                break;
            }
        }

        totalManagedObjects.Value--;

        // Remove from local cache
        networkObjectCache.Remove(networkObjectId);
        objectDataCache.Remove(networkObject);

        DebugLog($"Destroying networked object: ID {networkObjectId}");

        // Notify clients before destroying
        OnObjectDestroyedClientRpc(networkObjectId, objectData.ownerClientId, objectData.prefabId);

        // Destroy the network object
        networkObject.Despawn(true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangeObjectOwnershipServerRpc(ulong networkObjectId, ulong newOwnerClientId)
    {
        if (!networkObjectCache.ContainsKey(networkObjectId))
        {
            DebugLogError($"Cannot change ownership: NetworkObject ID {networkObjectId} not found");
            return;
        }

        NetworkObject networkObject = networkObjectCache[networkObjectId];
        ulong previousOwner = networkObject.OwnerClientId;

        // Change network ownership
        networkObject.ChangeOwnership(newOwnerClientId);

        // Update managed object data
        for (int i = 0; i < managedObjectsList.Count; i++)
        {
            ManagedObjectData data = managedObjectsList[i];
            if (data.networkObjectId == networkObjectId)
            {
                data.ownerClientId = newOwnerClientId;
                managedObjectsList[i] = data;
                objectDataCache[networkObject] = data;
                break;
            }
        }

        DebugLog($"Changed ownership of object {networkObjectId} from client {previousOwner} to client {newOwnerClientId}");

        // Notify clients
        OnOwnershipChangedClientRpc(networkObjectId, previousOwner, newOwnerClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TeleportObjectServerRpc(ulong networkObjectId, Vector3 newPosition)
    {
        if (!networkObjectCache.ContainsKey(networkObjectId))
        {
            DebugLogError($"Cannot teleport object: NetworkObject ID {networkObjectId} not found");
            return;
        }

        NetworkObject networkObject = networkObjectCache[networkObjectId];
        
        // Update position
        TeleportObjectClientRpc(networkObjectId, newPosition);
        
        // Update managed object data
        for (int i = 0; i < managedObjectsList.Count; i++)
        {
            ManagedObjectData data = managedObjectsList[i];
            if (data.networkObjectId == networkObjectId)
            {
                data.position = newPosition;
                managedObjectsList[i] = data;
                objectDataCache[networkObject] = data;
                break;
            }
        }

        DebugLog($"Teleported object {networkObjectId} to position {newPosition}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetObjectActiveServerRpc(ulong networkObjectId, bool active)
    {
        if (!networkObjectCache.ContainsKey(networkObjectId))
        {
            DebugLogError($"Cannot set active state: NetworkObject ID {networkObjectId} not found");
            return;
        }

        // Update managed object data
        for (int i = 0; i < managedObjectsList.Count; i++)
        {
            ManagedObjectData data = managedObjectsList[i];
            if (data.networkObjectId == networkObjectId)
            {
                data.isActive = active;
                managedObjectsList[i] = data;
                break;
            }
        }

        SetObjectActiveClientRpc(networkObjectId, active);
        DebugLog($"Set object {networkObjectId} active state to {active}");
    }

    #endregion

    #region Client RPCs

    [ClientRpc]
    private void OnObjectSpawnedClientRpc(ulong networkObjectId, ulong ownerClientId, int prefabId, Vector3 position, Quaternion rotation)
    {
        OnGameObjectSpawned?.Invoke(this, new OnGameObjectSpawnedEventArgs
        {
            spawnedObject = networkObjectCache.ContainsKey(networkObjectId) ? networkObjectCache[networkObjectId] : null,
            ownerClientId = ownerClientId,
            prefabId = prefabId,
            position = position,
            rotation = rotation
        });
    }

    [ClientRpc]
    private void OnObjectDestroyedClientRpc(ulong networkObjectId, ulong ownerClientId, int prefabId)
    {
        OnGameObjectDestroyed?.Invoke(this, new OnGameObjectDestroyedEventArgs
        {
            networkObjectId = networkObjectId,
            ownerClientId = ownerClientId,
            prefabId = prefabId
        });
    }

    [ClientRpc]
    private void OnOwnershipChangedClientRpc(ulong networkObjectId, ulong previousOwner, ulong newOwner)
    {
        NetworkObject networkObject = networkObjectCache.ContainsKey(networkObjectId) ? networkObjectCache[networkObjectId] : null;
        
        OnOwnershipChanged?.Invoke(this, new OnOwnershipChangedEventArgs
        {
            networkObject = networkObject,
            previousOwner = previousOwner,
            newOwner = newOwner
        });
    }

    [ClientRpc]
    private void TeleportObjectClientRpc(ulong networkObjectId, Vector3 newPosition)
    {
        if (networkObjectCache.ContainsKey(networkObjectId))
        {
            NetworkObject networkObject = networkObjectCache[networkObjectId];
            networkObject.transform.position = newPosition;
        }
    }

    [ClientRpc]
    private void SetObjectActiveClientRpc(ulong networkObjectId, bool active)
    {
        if (networkObjectCache.ContainsKey(networkObjectId))
        {
            NetworkObject networkObject = networkObjectCache[networkObjectId];
            networkObject.gameObject.SetActive(active);
        }
    }

    [ClientRpc]
    private void SetFollowTransformClientRpc(NetworkObjectReference networkObjectReference, ulong clientOwnerId)
    {

        if (NetworkManager.Singleton.LocalClientId != clientOwnerId)
        {
            DebugLog($"Client {NetworkManager.Singleton.LocalClientId} attempted to set follow transform for object {networkObjectReference.NetworkObjectId} but is not the owner.");
            return;
        }

        NetworkObject networkObject;
        if (!networkObjectReference.TryGet(out networkObject))
        {
            DebugLogError($"Failed to get NetworkObject from reference {networkObjectReference.NetworkObjectId}");
            return;
        }
        
        MP_FollowTransform followTransform = networkObject.gameObject?.GetComponent<MP_FollowTransform>();

        if (followTransform != null)
        {
            followTransform.SetTarget(MP2D_PlayerManager.Instance.HoldItemPivot, Vector3.zero);
            MP2D_PlayerManager.Instance.HoldingItem = true;
            DebugLog($"Client {NetworkManager.Singleton.LocalClientId} attempted to set follow transform for object {networkObjectReference.NetworkObjectId} but is not the owner.");
        }
        else
        {
            DebugLogError($"NetworkObject {networkObjectReference.NetworkObjectId} does not have a MP_FollowTransform component");
        }
    }

    #endregion

    #region Event Handlers

    private void ManagedObjectsList_OnListChanged(NetworkListEvent<ManagedObjectData> changeEvent)
    {
        DebugLog($"Managed objects list changed: {changeEvent.Type}");

        // Update local cache when list changes
        RefreshNetworkObjectCache();
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        if (!IsServer) return;

        DebugLog($"Client {clientId} disconnected, cleaning up their objects");
        
        // Get all objects owned by the disconnected client
        List<NetworkObject> objectsToDestroy = GetObjectsOwnedByClient(clientId);
        
        // Destroy all objects owned by the disconnected client
        foreach (NetworkObject obj in objectsToDestroy)
        {
            DestroyNetworkedObject(obj);
        }
    }

    #endregion

    #region Helper Methods

    private void RefreshNetworkObjectCache()
    {
        networkObjectCache.Clear();
        objectDataCache.Clear();

        foreach (var objectData in managedObjectsList)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(objectData.networkObjectId))
            {
                NetworkObject networkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectData.networkObjectId];
                networkObjectCache[objectData.networkObjectId] = networkObject;
                objectDataCache[networkObject] = objectData;
            }
        }
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[NetworkedGameObjectManager] {message}");
        }
    }

    private void DebugLogError(string message)
    {
        if (enableDebugLogs)
        {
            Debug.LogError($"[NetworkedGameObjectManager] {message}");
        }
    }

    #endregion

    #region Public Utility Methods

    /// <summary>
    /// Checks if a NetworkObject is managed by this manager.
    /// </summary>
    public bool IsManagedObject(NetworkObject networkObject)
    {
        return objectDataCache.ContainsKey(networkObject);
    }

    /// <summary>
    /// Gets the managed object data for a NetworkObject.
    /// </summary>
    public ManagedObjectData? GetManagedObjectData(NetworkObject networkObject)
    {
        if (objectDataCache.ContainsKey(networkObject))
        {
            return objectDataCache[networkObject];
        }
        return null;
    }

    /// <summary>
    /// Gets all currently managed objects.
    /// </summary>
    public List<NetworkObject> GetAllManagedObjects()
    {
        List<NetworkObject> managedObjects = new List<NetworkObject>();
        
        foreach (var kvp in networkObjectCache)
        {
            managedObjects.Add(kvp.Value);
        }
        
        return managedObjects;
    }

    /// <summary>
    /// Cleanup all managed objects (server only).
    /// </summary>
    public void CleanupAllObjects()
    {
        if (!IsServer) return;

        List<NetworkObject> allObjects = GetAllManagedObjects();
        foreach (NetworkObject obj in allObjects)
        {
            DestroyNetworkedObject(obj);
        }
    }

    #endregion
}
