using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Centralized manager for handling GameObjects across multiplayer clients.
/// Provides functionality for spawning, destroying, and synchronizing GameObjects
/// in a multiplayer environment using Unity Netcode for GameObjects.
/// </summary>
public class MP_NetworkedGameObjectManager : NetworkBehaviour
{
    public static MP_NetworkedGameObjectManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private bool m_EnableDebugLogs = true;
    [SerializeField] private int m_MaxManagedObjects = 1000;

    [Header("Prefab Registry")]
    [SerializeField] private MP_NetworkedObjectRegistry m_ObjectRegistry;

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
    private NetworkList<ManagedObjectData> m_ManagedObjectsList;
    private NetworkVariable<int> m_TotalManagedObjects = new NetworkVariable<int>(0);

    // Local tracking
    private Dictionary<ulong, NetworkObject> m_NetworkObjectCache = new Dictionary<ulong, NetworkObject>();
    private Dictionary<NetworkObject, ManagedObjectData> m_ObjectDataCache = new Dictionary<NetworkObject, ManagedObjectData>();

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

        public void NetworkSerialize<T>(BufferSerializer<T> p_Serializer) where T : IReaderWriter
        {
            p_Serializer.SerializeValue(ref networkObjectId);
            p_Serializer.SerializeValue(ref ownerClientId);
            p_Serializer.SerializeValue(ref prefabId);
            p_Serializer.SerializeValue(ref position);
            p_Serializer.SerializeValue(ref rotation);
            p_Serializer.SerializeValue(ref spawnTime);
            p_Serializer.SerializeValue(ref isActive);
        }

        public bool Equals(ManagedObjectData p_Other)
        {
            return networkObjectId == p_Other.networkObjectId;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            m_ManagedObjectsList = new NetworkList<ManagedObjectData>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        m_ManagedObjectsList.OnListChanged += ManagedObjectsList_OnListChanged;
        
        if (IsServer)
        {
            NetworkManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
        }

        DebugLog("MP_NetworkedGameObjectManager spawned on network");
    }

    public override void OnNetworkDespawn()
    {
        m_ManagedObjectsList.OnListChanged -= ManagedObjectsList_OnListChanged;
        
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
    public void SpawnNetworkedObject(int p_PrefabId, Vector3 p_Position, Quaternion p_Rotation, ulong p_OwnerClientId = 0)
    {
        if (p_OwnerClientId == 0)
            p_OwnerClientId = NetworkManager.Singleton.LocalClientId;

        SpawnNetworkedObjectServerRpc(p_PrefabId, p_Position, p_Rotation, p_OwnerClientId);
    }

    /// <summary>
    /// Destroys a networked GameObject by its NetworkObject reference.
    /// </summary>
    public void DestroyNetworkedObject(NetworkObject p_NetworkObject)
    {
        if (p_NetworkObject == null) return;

        DestroyNetworkedObjectServerRpc(p_NetworkObject.NetworkObjectId);
    }

    /// <summary>
    /// Changes ownership of a networked object to a new client.
    /// </summary>
    public void ChangeObjectOwnership(NetworkObject p_NetworkObject, ulong p_NewOwnerClientId)
    {
        if (p_NetworkObject == null) return;

        ChangeObjectOwnershipServerRpc(p_NetworkObject.NetworkObjectId, p_NewOwnerClientId);
    }

    /// <summary>
    /// Gets all managed objects owned by a specific client.
    /// </summary>
    public List<NetworkObject> GetObjectsOwnedByClient(ulong p_ClientId)
    {
        List<NetworkObject> l_OwnedObjects = new List<NetworkObject>();
        
        foreach (var l_ObjectData in m_ManagedObjectsList)
        {
            if (l_ObjectData.ownerClientId == p_ClientId && l_ObjectData.isActive && m_NetworkObjectCache.ContainsKey(l_ObjectData.networkObjectId))
            {
                l_OwnedObjects.Add(m_NetworkObjectCache[l_ObjectData.networkObjectId]);
            }
        }
        
        return l_OwnedObjects;
    }

    /// <summary>
    /// Gets the total number of active managed objects.
    /// </summary>
    public int GetTotalManagedObjects()
    {
        return m_TotalManagedObjects.Value;
    }

    /// <summary>
    /// Teleports a networked object to a new position.
    /// </summary>
    public void TeleportObject(NetworkObject p_NetworkObject, Vector3 p_NewPosition)
    {
        if (p_NetworkObject == null) return;

        TeleportObjectServerRpc(p_NetworkObject.NetworkObjectId, p_NewPosition);
    }

    /// <summary>
    /// Sets the active state of a networked object.
    /// </summary>
    public void SetObjectActive(NetworkObject p_NetworkObject, bool p_Active)
    {
        if (p_NetworkObject == null) return;

        SetObjectActiveServerRpc(p_NetworkObject.NetworkObjectId, p_Active);
    }

    public int GetPrefabRegistryId(GameObject p_Prefab)
    {
        return m_ObjectRegistry.GetPrefabId(p_Prefab);
    }

    #endregion

    #region Server RPCs

    [ServerRpc(RequireOwnership = false)]
    private void SpawnNetworkedObjectServerRpc(int p_PrefabId, Vector3 p_Position, Quaternion p_Rotation, ulong p_OwnerClientId)
    {
        if (m_TotalManagedObjects.Value >= m_MaxManagedObjects)
        {
            DebugLogError($"Cannot spawn object: Maximum managed objects limit ({m_MaxManagedObjects}) reached");
            return;
        }

        if (m_ObjectRegistry == null || !m_ObjectRegistry.IsValidPrefabId(p_PrefabId))
        {
            DebugLogError($"Invalid prefab ID: {p_PrefabId}");
            return;
        }

        GameObject l_Prefab = m_ObjectRegistry.GetPrefab(p_PrefabId);
        if (l_Prefab == null)
        {
            DebugLogError($"Prefab not found for ID: {p_PrefabId}");
            return;
        }

        // Instantiate and spawn the object
        GameObject l_Instance = Instantiate(l_Prefab, p_Position, p_Rotation);
        NetworkObject l_NetworkObject = l_Instance.GetComponent<NetworkObject>();
        
        if (l_NetworkObject == null)
        {
            DebugLogError($"Prefab {l_Prefab.name} does not have a NetworkObject component");
            Destroy(l_Instance);
            return;
        }

        // Spawn with ownership
        l_NetworkObject.SpawnWithOwnership(p_OwnerClientId);

        // Create managed object data
        ManagedObjectData l_ObjectData = new ManagedObjectData
        {
            networkObjectId = l_NetworkObject.NetworkObjectId,
            ownerClientId = p_OwnerClientId,
            prefabId = p_PrefabId,
            position = p_Position,
            rotation = p_Rotation,
            spawnTime = Time.time,
            isActive = true
        };

        // Add to managed list
        m_ManagedObjectsList.Add(l_ObjectData);
        m_TotalManagedObjects.Value++;

        // Update local cache
        m_NetworkObjectCache[l_NetworkObject.NetworkObjectId] = l_NetworkObject;
        m_ObjectDataCache[l_NetworkObject] = l_ObjectData;

        DebugLog($"Spawned networked object: {l_Prefab.name} (ID: {l_NetworkObject.NetworkObjectId}) for client {p_OwnerClientId}");

        MP_FollowTransform l_FollowTransform;
        if (l_Instance.TryGetComponent<MP_FollowTransform>(out l_FollowTransform))
        {
            SetFollowTransformClientRpc(l_NetworkObject, p_OwnerClientId);
        }
        
        // Notify clients
        OnObjectSpawnedClientRpc(l_NetworkObject.NetworkObjectId, p_OwnerClientId, p_PrefabId, p_Position, p_Rotation);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyNetworkedObjectServerRpc(ulong p_NetworkObjectId)
    {
        if (!m_NetworkObjectCache.ContainsKey(p_NetworkObjectId))
        {
            DebugLogError($"Cannot destroy object: NetworkObject ID {p_NetworkObjectId} not found");
            return;
        }

        NetworkObject l_NetworkObject = m_NetworkObjectCache[p_NetworkObjectId];
        ManagedObjectData l_ObjectData = m_ObjectDataCache[l_NetworkObject];

        // Remove from managed list
        for (int l_Index = 0; l_Index < m_ManagedObjectsList.Count; l_Index++)
        {
            if (m_ManagedObjectsList[l_Index].networkObjectId == p_NetworkObjectId)
            {
                m_ManagedObjectsList.RemoveAt(l_Index);
                break;
            }
        }

        m_TotalManagedObjects.Value--;

        // Remove from local cache
        m_NetworkObjectCache.Remove(p_NetworkObjectId);
        m_ObjectDataCache.Remove(l_NetworkObject);

        DebugLog($"Destroying networked object: ID {p_NetworkObjectId}");

        // Notify clients before destroying
        OnObjectDestroyedClientRpc(p_NetworkObjectId, l_ObjectData.ownerClientId, l_ObjectData.prefabId);

        // Destroy the network object
        l_NetworkObject.Despawn(true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangeObjectOwnershipServerRpc(ulong p_NetworkObjectId, ulong p_NewOwnerClientId)
    {
        if (!m_NetworkObjectCache.ContainsKey(p_NetworkObjectId))
        {
            DebugLogError($"Cannot change ownership: NetworkObject ID {p_NetworkObjectId} not found");
            return;
        }

        NetworkObject l_NetworkObject = m_NetworkObjectCache[p_NetworkObjectId];
        ulong l_PreviousOwner = l_NetworkObject.OwnerClientId;

        // Change network ownership
        l_NetworkObject.ChangeOwnership(p_NewOwnerClientId);

        // Update managed object data
        for (int l_Index = 0; l_Index < m_ManagedObjectsList.Count; l_Index++)
        {
            ManagedObjectData l_Data = m_ManagedObjectsList[l_Index];
            if (l_Data.networkObjectId == p_NetworkObjectId)
            {
                l_Data.ownerClientId = p_NewOwnerClientId;
                m_ManagedObjectsList[l_Index] = l_Data;
                m_ObjectDataCache[l_NetworkObject] = l_Data;
                break;
            }
        }

        DebugLog($"Changed ownership of object {p_NetworkObjectId} from client {l_PreviousOwner} to client {p_NewOwnerClientId}");

        // Notify clients
        OnOwnershipChangedClientRpc(p_NetworkObjectId, l_PreviousOwner, p_NewOwnerClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TeleportObjectServerRpc(ulong p_NetworkObjectId, Vector3 p_NewPosition)
    {
        if (!m_NetworkObjectCache.ContainsKey(p_NetworkObjectId))
        {
            DebugLogError($"Cannot teleport object: NetworkObject ID {p_NetworkObjectId} not found");
            return;
        }

        NetworkObject l_NetworkObject = m_NetworkObjectCache[p_NetworkObjectId];
        
        // Update position
        TeleportObjectClientRpc(p_NetworkObjectId, p_NewPosition);
        
        // Update managed object data
        for (int l_Index = 0; l_Index < m_ManagedObjectsList.Count; l_Index++)
        {
            ManagedObjectData l_Data = m_ManagedObjectsList[l_Index];
            if (l_Data.networkObjectId == p_NetworkObjectId)
            {
                l_Data.position = p_NewPosition;
                m_ManagedObjectsList[l_Index] = l_Data;
                m_ObjectDataCache[l_NetworkObject] = l_Data;
                break;
            }
        }

        DebugLog($"Teleported object {p_NetworkObjectId} to position {p_NewPosition}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetObjectActiveServerRpc(ulong p_NetworkObjectId, bool p_Active)
    {
        if (!m_NetworkObjectCache.ContainsKey(p_NetworkObjectId))
        {
            DebugLogError($"Cannot set active state: NetworkObject ID {p_NetworkObjectId} not found");
            return;
        }

        // Update managed object data
        for (int l_Index = 0; l_Index < m_ManagedObjectsList.Count; l_Index++)
        {
            ManagedObjectData l_Data = m_ManagedObjectsList[l_Index];
            if (l_Data.networkObjectId == p_NetworkObjectId)
            {
                l_Data.isActive = p_Active;
                m_ManagedObjectsList[l_Index] = l_Data;
                break;
            }
        }

        SetObjectActiveClientRpc(p_NetworkObjectId, p_Active);
        DebugLog($"Set object {p_NetworkObjectId} active state to {p_Active}");
    }

    #endregion

    #region Client RPCs

    [ClientRpc]
    private void OnObjectSpawnedClientRpc(ulong p_NetworkObjectId, ulong p_OwnerClientId, int p_PrefabId, Vector3 p_Position, Quaternion p_Rotation)
    {
        OnGameObjectSpawned?.Invoke(this, new OnGameObjectSpawnedEventArgs
        {
            spawnedObject = m_NetworkObjectCache.ContainsKey(p_NetworkObjectId) ? m_NetworkObjectCache[p_NetworkObjectId] : null,
            ownerClientId = p_OwnerClientId,
            prefabId = p_PrefabId,
            position = p_Position,
            rotation = p_Rotation
        });
    }

    [ClientRpc]
    private void OnObjectDestroyedClientRpc(ulong p_NetworkObjectId, ulong p_OwnerClientId, int p_PrefabId)
    {
        OnGameObjectDestroyed?.Invoke(this, new OnGameObjectDestroyedEventArgs
        {
            networkObjectId = p_NetworkObjectId,
            ownerClientId = p_OwnerClientId,
            prefabId = p_PrefabId
        });
    }

    [ClientRpc]
    private void OnOwnershipChangedClientRpc(ulong p_NetworkObjectId, ulong p_PreviousOwner, ulong p_NewOwner)
    {
        NetworkObject l_NetworkObject = m_NetworkObjectCache.ContainsKey(p_NetworkObjectId) ? m_NetworkObjectCache[p_NetworkObjectId] : null;
        
        OnOwnershipChanged?.Invoke(this, new OnOwnershipChangedEventArgs
        {
            networkObject = l_NetworkObject,
            previousOwner = p_PreviousOwner,
            newOwner = p_NewOwner
        });
    }

    [ClientRpc]
    private void TeleportObjectClientRpc(ulong p_NetworkObjectId, Vector3 p_NewPosition)
    {
        if (m_NetworkObjectCache.ContainsKey(p_NetworkObjectId))
        {
            NetworkObject l_NetworkObject = m_NetworkObjectCache[p_NetworkObjectId];
            l_NetworkObject.transform.position = p_NewPosition;
        }
    }

    [ClientRpc]
    private void SetObjectActiveClientRpc(ulong p_NetworkObjectId, bool p_Active)
    {
        if (m_NetworkObjectCache.ContainsKey(p_NetworkObjectId))
        {
            NetworkObject l_NetworkObject = m_NetworkObjectCache[p_NetworkObjectId];
            l_NetworkObject.gameObject.SetActive(p_Active);
        }
    }

    [ClientRpc]
    private void SetFollowTransformClientRpc(NetworkObjectReference p_NetworkObjectReference, ulong p_ClientOwnerId)
    {

        if (NetworkManager.Singleton.LocalClientId != p_ClientOwnerId)
        {
            DebugLog($"Client {NetworkManager.Singleton.LocalClientId} attempted to set follow transform for object {p_NetworkObjectReference.NetworkObjectId} but is not the owner.");
            return;
        }

        NetworkObject l_NetworkObject;
        if (!p_NetworkObjectReference.TryGet(out l_NetworkObject))
        {
            DebugLogError($"Failed to get NetworkObject from reference {p_NetworkObjectReference.NetworkObjectId}");
            return;
        }
        
        MP_FollowTransform l_FollowTransform = l_NetworkObject.gameObject?.GetComponent<MP_FollowTransform>();

        if (l_FollowTransform != null)
        {
            l_FollowTransform.SetTarget(MP2D_PlayerManager.Instance.HoldItemPivot, Vector3.zero);
            MP2D_PlayerManager.Instance.HoldingItem = l_NetworkObject;
        }
        else
        {
            DebugLogError($"NetworkObject {p_NetworkObjectReference.NetworkObjectId} does not have a MP_FollowTransform component");
        }
    }

    #endregion

    #region Event Handlers

    private void ManagedObjectsList_OnListChanged(NetworkListEvent<ManagedObjectData> p_ChangeEvent)
    {
        DebugLog($"Managed objects list changed: {p_ChangeEvent.Type}");

        // Update local cache when list changes
        RefreshNetworkObjectCache();
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong p_ClientId)
    {
        if (!IsServer) return;

        DebugLog($"Client {p_ClientId} disconnected, cleaning up their objects");
        
        // Get all objects owned by the disconnected client
        List<NetworkObject> l_ObjectsToDestroy = GetObjectsOwnedByClient(p_ClientId);
        
        // Destroy all objects owned by the disconnected client
        foreach (NetworkObject l_Obj in l_ObjectsToDestroy)
        {
            DestroyNetworkedObject(l_Obj);
        }
    }

    #endregion

    #region Helper Methods

    private void RefreshNetworkObjectCache()
    {
        m_NetworkObjectCache.Clear();
        m_ObjectDataCache.Clear();

        foreach (var l_ObjectData in m_ManagedObjectsList)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(l_ObjectData.networkObjectId))
            {
                NetworkObject l_NetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[l_ObjectData.networkObjectId];
                m_NetworkObjectCache[l_ObjectData.networkObjectId] = l_NetworkObject;
                m_ObjectDataCache[l_NetworkObject] = l_ObjectData;
            }
        }
    }

    private void DebugLog(string p_Message)
    {
        if (m_EnableDebugLogs)
        {
            Debug.Log($"[MP_NetworkedGameObjectManager] {p_Message}");
        }
    }

    private void DebugLogError(string p_Message)
    {
        if (m_EnableDebugLogs)
        {
            Debug.LogError($"[MP_NetworkedGameObjectManager] {p_Message}");
        }
    }

    #endregion

    #region Public Utility Methods

    /// <summary>
    /// Checks if a NetworkObject is managed by this manager.
    /// </summary>
    public bool IsManagedObject(NetworkObject p_NetworkObject)
    {
        return m_ObjectDataCache.ContainsKey(p_NetworkObject);
    }

    /// <summary>
    /// Gets the managed object data for a NetworkObject.
    /// </summary>
    public ManagedObjectData? GetManagedObjectData(NetworkObject p_NetworkObject)
    {
        if (m_ObjectDataCache.ContainsKey(p_NetworkObject))
        {
            return m_ObjectDataCache[p_NetworkObject];
        }
        return null;
    }

    /// <summary>
    /// Gets all currently managed objects.
    /// </summary>
    public List<NetworkObject> GetAllManagedObjects()
    {
        List<NetworkObject> l_ManagedObjects = new List<NetworkObject>();
        
        foreach (var l_Kvp in m_NetworkObjectCache)
        {
            l_ManagedObjects.Add(l_Kvp.Value);
        }
        
        return l_ManagedObjects;
    }

    /// <summary>
    /// Cleanup all managed objects (server only).
    /// </summary>
    public void CleanupAllObjects()
    {
        if (!IsServer) return;

        List<NetworkObject> l_AllObjects = GetAllManagedObjects();
        foreach (NetworkObject l_Obj in l_AllObjects)
        {
            DestroyNetworkedObject(l_Obj);
        }
    }

    #endregion
}
