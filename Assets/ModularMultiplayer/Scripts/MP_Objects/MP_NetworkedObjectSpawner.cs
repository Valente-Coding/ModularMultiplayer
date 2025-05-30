using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Example implementation showing how to use the MP_NetworkedGameObjectManager
/// for managing GameObjects across multiplayer clients.
/// This script provides common use cases and patterns.
/// </summary>
public class MP_NetworkedObjectSpawner : NetworkBehaviour
{
    [Header("Spawning Configuration")]
    [SerializeField] private MP_NetworkedObjectRegistry m_ObjectRegistry;
    [SerializeField] private Transform[] m_SpawnPoints;
    [SerializeField] private float m_SpawnRadius = 5f;
    [SerializeField] private LayerMask m_GroundLayerMask = 1;

    [Header("Auto Spawn Settings")]
    [SerializeField] private bool m_AutoSpawnOnStart = false;
    [SerializeField] private int m_AutoSpawnCount = 5;
    [SerializeField] private float m_AutoSpawnInterval = 2f;

    [Header("Input Settings")]
    [SerializeField] private KeyCode m_SpawnKey = KeyCode.E;
    [SerializeField] private KeyCode m_DestroyKey = KeyCode.E;
    [SerializeField] private KeyCode m_TeleportKey = KeyCode.J;

    // Tracking spawned objects
    private List<NetworkObject> m_SpawnedObjects = new List<NetworkObject>();
    private float m_AutoSpawnTimer = 0f;
    private int m_SpawnPrefabId = -1;
    private bool m_CanSpawnPrefab = false;
    private bool m_CanDespawnPrefab = false;

    private void Start()
    {
        if (MP_NetworkedGameObjectManager.Instance != null)
        {
            // Subscribe to events
            MP_NetworkedGameObjectManager.Instance.OnGameObjectSpawned += OnGameObjectSpawned;
            MP_NetworkedGameObjectManager.Instance.OnGameObjectDestroyed += OnGameObjectDestroyed;
            MP_NetworkedGameObjectManager.Instance.OnOwnershipChanged += OnOwnershipChanged;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleInput();
        HandleAutoSpawn();
    }

    #region Input Handling

    private void HandleInput()
    {

        // Despawn object on key press
        if (m_CanDespawnPrefab && Input.GetKeyDown(m_DestroyKey))
        {
            MP_NetworkedGameObjectManager.Instance.DestroyNetworkedObject(MP2D_PlayerManager.Instance.HoldingItem);
        }
    }

    #endregion

    #region Auto Spawning

    private void HandleAutoSpawn()
    {
        if (!m_AutoSpawnOnStart || !IsServer) return;

        m_AutoSpawnTimer += Time.deltaTime;
        if (m_AutoSpawnTimer >= m_AutoSpawnInterval)
        {
            m_AutoSpawnTimer = 0f;
            
            if (m_SpawnedObjects.Count < m_AutoSpawnCount)
            {
                SpawnRandomObject();
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Spawns a random object from the registry at a random spawn point.
    /// </summary>
    public void SpawnRandomObject()
    {
        if (MP_NetworkedGameObjectManager.Instance == null || m_ObjectRegistry == null)
        {
            Debug.LogWarning("[MP_NetworkedObjectSpawner] MP_NetworkedGameObjectManager or ObjectRegistry not available");
            return;
        }

        var l_ActivePrefabs = m_ObjectRegistry.GetAllActivePrefabs();
        if (l_ActivePrefabs.Count == 0)
        {
            Debug.LogWarning("[MP_NetworkedObjectSpawner] No active prefabs available in registry");
            return;
        }

        // Select random prefab
        var l_RandomPrefab = l_ActivePrefabs[Random.Range(0, l_ActivePrefabs.Count)];
        
        // Get spawn position
        Vector3 l_SpawnPosition = GetRandomSpawnPosition();
        Quaternion l_SpawnRotation = Quaternion.Euler(0, 0, 0);

        // Spawn the object
        MP_NetworkedGameObjectManager.Instance.SpawnNetworkedObject(
            l_RandomPrefab.PrefabId, 
            l_SpawnPosition, 
            l_SpawnRotation, 
            NetworkManager.Singleton.LocalClientId
        );

        Debug.Log($"[MP_NetworkedObjectSpawner] Spawning {l_RandomPrefab.EntryName} at {l_SpawnPosition}");
    }

    /// <summary>
    /// Spawns a specific object by prefab ID.
    /// </summary>
    public void SpawnObjectById(int p_PrefabId, Vector3 p_Position, Quaternion p_Rotation)
    {
        if (MP_NetworkedGameObjectManager.Instance == null)
        {
            Debug.LogWarning("[MP_NetworkedObjectSpawner] MP_NetworkedGameObjectManager not available");
            return;
        }

        MP_NetworkedGameObjectManager.Instance.SpawnNetworkedObject(
            p_PrefabId, 
            p_Position, 
            p_Rotation, 
            NetworkManager.Singleton.LocalClientId
        );
    }
    
    public void DespawnObject(NetworkObject p_Object)
    {
        if (MP_NetworkedGameObjectManager.Instance == null)
        {
            Debug.LogWarning("[MP_NetworkedObjectSpawner] MP_NetworkedGameObjectManager not available");
            return;
        }

        MP_NetworkedGameObjectManager.Instance.DestroyNetworkedObject(p_Object);
    }

    /// <summary>
    /// Destroys a random spawned object.
    /// </summary>
    public void DestroyRandomObject()
    {
        if (MP_NetworkedGameObjectManager.Instance == null || m_SpawnedObjects.Count == 0)
        {
            Debug.LogWarning("[MP_NetworkedObjectSpawner] No objects available to destroy");
            return;
        }

        // Remove null references
        m_SpawnedObjects.RemoveAll(l_Obj => l_Obj == null);

        if (m_SpawnedObjects.Count == 0) return;

        // Select random object
        NetworkObject l_RandomObject = m_SpawnedObjects[Random.Range(0, m_SpawnedObjects.Count)];

        Debug.Log($"[MP_NetworkedObjectSpawner] Destroying object: {l_RandomObject.name}");
        MP_NetworkedGameObjectManager.Instance.DestroyNetworkedObject(l_RandomObject);
    }

    /// <summary>
    /// Teleports a random spawned object to a new location.
    /// </summary>
    public void TeleportRandomObject()
    {
        if (MP_NetworkedGameObjectManager.Instance == null || m_SpawnedObjects.Count == 0)
        {
            Debug.LogWarning("[MP_NetworkedObjectSpawner] No objects available to teleport");
            return;
        }

        // Remove null references
        m_SpawnedObjects.RemoveAll(l_Obj => l_Obj == null);

        if (m_SpawnedObjects.Count == 0) return;

        // Select random object and position
        NetworkObject l_RandomObject = m_SpawnedObjects[Random.Range(0, m_SpawnedObjects.Count)];
        Vector3 l_NewPosition = GetRandomSpawnPosition();

        Debug.Log($"[MP_NetworkedObjectSpawner] Teleporting {l_RandomObject.name} to {l_NewPosition}");
        MP_NetworkedGameObjectManager.Instance.TeleportObject(l_RandomObject, l_NewPosition);
    }

    /// <summary>
    /// Changes ownership of a random spawned object to another client.
    /// </summary>
    public void TransferRandomObjectOwnership()
    {
        if (MP_NetworkedGameObjectManager.Instance == null || m_SpawnedObjects.Count == 0)
        {
            Debug.LogWarning("[MP_NetworkedObjectSpawner] No objects available for ownership transfer");
            return;
        }

        // Remove null references
        m_SpawnedObjects.RemoveAll(l_Obj => l_Obj == null);

        if (m_SpawnedObjects.Count == 0) return;

        // Get all connected clients
        var l_ConnectedClients = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
        if (l_ConnectedClients.Count <= 1) return;

        // Select random object and client
        NetworkObject l_RandomObject = m_SpawnedObjects[Random.Range(0, m_SpawnedObjects.Count)];
        ulong l_CurrentOwner = l_RandomObject.OwnerClientId;
        
        // Find a different client
        l_ConnectedClients.Remove(l_CurrentOwner);
        if (l_ConnectedClients.Count == 0) return;

        ulong l_NewOwner = l_ConnectedClients[Random.Range(0, l_ConnectedClients.Count)];

        Debug.Log($"[MP_NetworkedObjectSpawner] Transferring ownership of {l_RandomObject.name} from {l_CurrentOwner} to {l_NewOwner}");
        MP_NetworkedGameObjectManager.Instance.ChangeObjectOwnership(l_RandomObject, l_NewOwner);
    }

    /// <summary>
    /// Destroys all objects owned by the local client.
    /// </summary>
    public void DestroyAllOwnedObjects()
    {
        if (MP_NetworkedGameObjectManager.Instance == null) return;

        var l_OwnedObjects = MP_NetworkedGameObjectManager.Instance.GetObjectsOwnedByClient(NetworkManager.Singleton.LocalClientId);
        
        Debug.Log($"[MP_NetworkedObjectSpawner] Destroying {l_OwnedObjects.Count} owned objects");
        
        foreach (var l_Obj in l_OwnedObjects)
        {
            MP_NetworkedGameObjectManager.Instance.DestroyNetworkedObject(l_Obj);
        }
    }

    #endregion

    #region Event Handlers

    private void OnGameObjectSpawned(object p_Sender, MP_NetworkedGameObjectManager.OnGameObjectSpawnedEventArgs p_E)
    {
        if (p_E.spawnedObject != null)
        {
            m_SpawnedObjects.Add(p_E.spawnedObject);
            Debug.Log($"[MP_NetworkedObjectSpawner] Object spawned: {p_E.spawnedObject.name} owned by client {p_E.ownerClientId}");
        }
    }

    private void OnGameObjectDestroyed(object p_Sender, MP_NetworkedGameObjectManager.OnGameObjectDestroyedEventArgs p_E)
    {
        Debug.Log($"[MP_NetworkedObjectSpawner] Object destroyed: ID {p_E.networkObjectId} owned by client {p_E.ownerClientId}");
        
        // Remove from our tracking list
        m_SpawnedObjects.RemoveAll(l_Obj => l_Obj == null || l_Obj.NetworkObjectId == p_E.networkObjectId);
    }

    private void OnOwnershipChanged(object p_Sender, MP_NetworkedGameObjectManager.OnOwnershipChangedEventArgs p_E)
    {
        Debug.Log($"[MP_NetworkedObjectSpawner] Ownership changed for {p_E.networkObject?.name}: {p_E.previousOwner} → {p_E.newOwner}");
    }

    #endregion

    #region Utility Methods

    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 l_SpawnPosition;

        if (m_SpawnPoints != null && m_SpawnPoints.Length > 0)
        {
            // Use predefined spawn points
            Transform l_RandomSpawnPoint = m_SpawnPoints[Random.Range(0, m_SpawnPoints.Length)];
            l_SpawnPosition = l_RandomSpawnPoint.position;
            
            // Add some random offset
            Vector2 l_RandomOffset = Random.insideUnitCircle * m_SpawnRadius;
            l_SpawnPosition += new Vector3(l_RandomOffset.x, 0, l_RandomOffset.y);
        }
        else
        {
            // Use random position around this object
            Vector2 l_RandomOffset = Random.insideUnitCircle * m_SpawnRadius;
            l_SpawnPosition = transform.position + new Vector3(l_RandomOffset.x, 0, l_RandomOffset.y);
        }

        // Try to place on ground
        if (Physics.Raycast(l_SpawnPosition + Vector3.up * 10f, Vector3.down, out RaycastHit l_Hit, 20f, m_GroundLayerMask))
        {
            l_SpawnPosition = l_Hit.point;
        }

        return l_SpawnPosition;
    }

    /// <summary>
    /// Gets statistics about spawned objects.
    /// </summary>
    public string GetSpawnStatistics()
    {
        if (MP_NetworkedGameObjectManager.Instance == null)
            return "MP_NetworkedGameObjectManager not available";

        int l_TotalManaged = MP_NetworkedGameObjectManager.Instance.GetTotalManagedObjects();
        int l_LocalOwned = MP_NetworkedGameObjectManager.Instance.GetObjectsOwnedByClient(NetworkManager.Singleton.LocalClientId).Count;
        int l_LocalTracked = m_SpawnedObjects.Count;

        return $"Total Managed: {l_TotalManaged}, Locally Owned: {l_LocalOwned}, Locally Tracked: {l_LocalTracked}";
    }

    #endregion

    #region Server RPC Examples

    /// <summary>
    /// Example of spawning objects through server RPC for better control.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SpawnObjectServerRpc(int p_PrefabId, Vector3 p_Position, Quaternion p_Rotation, ulong p_OwnerClientId)
    {
        if (MP_NetworkedGameObjectManager.Instance != null)
        {
            MP_NetworkedGameObjectManager.Instance.SpawnNetworkedObject(p_PrefabId, p_Position, p_Rotation, p_OwnerClientId);
        }
    }

    /// <summary>
    /// Example of batch spawning multiple objects.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void BatchSpawnObjectsServerRpc(int[] p_PrefabIds, Vector3[] p_Positions, Quaternion[] p_Rotations, ulong p_OwnerClientId)
    {
        if (MP_NetworkedGameObjectManager.Instance == null) return;

        int l_Count = Mathf.Min(p_PrefabIds.Length, Mathf.Min(p_Positions.Length, p_Rotations.Length));
        
        for (int l_I = 0; l_I < l_Count; l_I++)
        {
            MP_NetworkedGameObjectManager.Instance.SpawnNetworkedObject(p_PrefabIds[l_I], p_Positions[l_I], p_Rotations[l_I], p_OwnerClientId);
        }
    }

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        if (MP_NetworkedGameObjectManager.Instance != null)
        {
            MP_NetworkedGameObjectManager.Instance.OnGameObjectSpawned -= OnGameObjectSpawned;
            MP_NetworkedGameObjectManager.Instance.OnGameObjectDestroyed -= OnGameObjectDestroyed;
            MP_NetworkedGameObjectManager.Instance.OnOwnershipChanged -= OnOwnershipChanged;
        }
    }

    #endregion
}
