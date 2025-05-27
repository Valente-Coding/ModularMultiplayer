using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Example implementation showing how to use the NetworkedGameObjectManager
/// for managing GameObjects across multiplayer clients.
/// This script provides common use cases and patterns.
/// </summary>
public class NetworkedObjectSpawner : NetworkBehaviour
{
    [Header("Spawning Configuration")]
    [SerializeField] private NetworkedObjectRegistry objectRegistry;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private LayerMask groundLayerMask = 1;

    [Header("Auto Spawn Settings")]
    [SerializeField] private bool autoSpawnOnStart = false;
    [SerializeField] private int autoSpawnCount = 5;
    [SerializeField] private float autoSpawnInterval = 2f;

    [Header("Input Settings")]
    [SerializeField] private KeyCode spawnKey = KeyCode.E;
    [SerializeField] private KeyCode destroyKey = KeyCode.H;
    [SerializeField] private KeyCode teleportKey = KeyCode.J;

    // Tracking spawned objects
    private List<NetworkObject> spawnedObjects = new List<NetworkObject>();
    private float autoSpawnTimer = 0f;

    private void Start()
    {
        if (NetworkedGameObjectManager.Instance != null)
        {
            // Subscribe to events
            NetworkedGameObjectManager.Instance.OnGameObjectSpawned += OnGameObjectSpawned;
            NetworkedGameObjectManager.Instance.OnGameObjectDestroyed += OnGameObjectDestroyed;
            NetworkedGameObjectManager.Instance.OnOwnershipChanged += OnOwnershipChanged;
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
        // Spawn object on key press
        if (Input.GetKeyDown(spawnKey))
        {
            SpawnRandomObject();
        }

        // Destroy random object on key press
        if (Input.GetKeyDown(destroyKey))
        {
            DestroyRandomObject();
        }

        // Teleport random object on key press
        if (Input.GetKeyDown(teleportKey))
        {
            TeleportRandomObject();
        }
    }

    #endregion

    #region Auto Spawning

    private void HandleAutoSpawn()
    {
        if (!autoSpawnOnStart || !IsServer) return;

        autoSpawnTimer += Time.deltaTime;
        if (autoSpawnTimer >= autoSpawnInterval)
        {
            autoSpawnTimer = 0f;
            
            if (spawnedObjects.Count < autoSpawnCount)
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
        if (NetworkedGameObjectManager.Instance == null || objectRegistry == null)
        {
            Debug.LogWarning("[NetworkedObjectSpawner] NetworkedGameObjectManager or ObjectRegistry not available");
            return;
        }

        var activePrefabs = objectRegistry.GetAllActivePrefabs();
        if (activePrefabs.Count == 0)
        {
            Debug.LogWarning("[NetworkedObjectSpawner] No active prefabs available in registry");
            return;
        }

        // Select random prefab
        var randomPrefab = activePrefabs[Random.Range(0, activePrefabs.Count)];
        
        // Get spawn position
        Vector3 spawnPosition = GetRandomSpawnPosition();
        Quaternion spawnRotation = Quaternion.Euler(0, 0, 0);

        // Spawn the object
        NetworkedGameObjectManager.Instance.SpawnNetworkedObject(
            randomPrefab.PrefabId, 
            spawnPosition, 
            spawnRotation, 
            NetworkManager.Singleton.LocalClientId
        );

        Debug.Log($"[NetworkedObjectSpawner] Spawning {randomPrefab.EntryName} at {spawnPosition}");
    }

    /// <summary>
    /// Spawns a specific object by prefab ID.
    /// </summary>
    public void SpawnObjectById(int prefabId, Vector3 position, Quaternion rotation)
    {
        if (NetworkedGameObjectManager.Instance == null)
        {
            Debug.LogWarning("[NetworkedObjectSpawner] NetworkedGameObjectManager not available");
            return;
        }

        NetworkedGameObjectManager.Instance.SpawnNetworkedObject(
            prefabId, 
            position, 
            rotation, 
            NetworkManager.Singleton.LocalClientId
        );
    }

    /// <summary>
    /// Destroys a random spawned object.
    /// </summary>
    public void DestroyRandomObject()
    {
        if (NetworkedGameObjectManager.Instance == null || spawnedObjects.Count == 0)
        {
            Debug.LogWarning("[NetworkedObjectSpawner] No objects available to destroy");
            return;
        }

        // Remove null references
        spawnedObjects.RemoveAll(obj => obj == null);

        if (spawnedObjects.Count == 0) return;

        // Select random object
        NetworkObject randomObject = spawnedObjects[Random.Range(0, spawnedObjects.Count)];
        
        Debug.Log($"[NetworkedObjectSpawner] Destroying object: {randomObject.name}");
        NetworkedGameObjectManager.Instance.DestroyNetworkedObject(randomObject);
    }

    /// <summary>
    /// Teleports a random spawned object to a new location.
    /// </summary>
    public void TeleportRandomObject()
    {
        if (NetworkedGameObjectManager.Instance == null || spawnedObjects.Count == 0)
        {
            Debug.LogWarning("[NetworkedObjectSpawner] No objects available to teleport");
            return;
        }

        // Remove null references
        spawnedObjects.RemoveAll(obj => obj == null);

        if (spawnedObjects.Count == 0) return;

        // Select random object and position
        NetworkObject randomObject = spawnedObjects[Random.Range(0, spawnedObjects.Count)];
        Vector3 newPosition = GetRandomSpawnPosition();

        Debug.Log($"[NetworkedObjectSpawner] Teleporting {randomObject.name} to {newPosition}");
        NetworkedGameObjectManager.Instance.TeleportObject(randomObject, newPosition);
    }

    /// <summary>
    /// Changes ownership of a random spawned object to another client.
    /// </summary>
    public void TransferRandomObjectOwnership()
    {
        if (NetworkedGameObjectManager.Instance == null || spawnedObjects.Count == 0)
        {
            Debug.LogWarning("[NetworkedObjectSpawner] No objects available for ownership transfer");
            return;
        }

        // Remove null references
        spawnedObjects.RemoveAll(obj => obj == null);

        if (spawnedObjects.Count == 0) return;

        // Get all connected clients
        var connectedClients = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
        if (connectedClients.Count <= 1) return;

        // Select random object and client
        NetworkObject randomObject = spawnedObjects[Random.Range(0, spawnedObjects.Count)];
        ulong currentOwner = randomObject.OwnerClientId;
        
        // Find a different client
        connectedClients.Remove(currentOwner);
        if (connectedClients.Count == 0) return;

        ulong newOwner = connectedClients[Random.Range(0, connectedClients.Count)];

        Debug.Log($"[NetworkedObjectSpawner] Transferring ownership of {randomObject.name} from {currentOwner} to {newOwner}");
        NetworkedGameObjectManager.Instance.ChangeObjectOwnership(randomObject, newOwner);
    }

    /// <summary>
    /// Destroys all objects owned by the local client.
    /// </summary>
    public void DestroyAllOwnedObjects()
    {
        if (NetworkedGameObjectManager.Instance == null) return;

        var ownedObjects = NetworkedGameObjectManager.Instance.GetObjectsOwnedByClient(NetworkManager.Singleton.LocalClientId);
        
        Debug.Log($"[NetworkedObjectSpawner] Destroying {ownedObjects.Count} owned objects");
        
        foreach (var obj in ownedObjects)
        {
            NetworkedGameObjectManager.Instance.DestroyNetworkedObject(obj);
        }
    }

    #endregion

    #region Event Handlers

    private void OnGameObjectSpawned(object sender, NetworkedGameObjectManager.OnGameObjectSpawnedEventArgs e)
    {
        if (e.spawnedObject != null)
        {
            spawnedObjects.Add(e.spawnedObject);
            Debug.Log($"[NetworkedObjectSpawner] Object spawned: {e.spawnedObject.name} owned by client {e.ownerClientId}");
        }
    }

    private void OnGameObjectDestroyed(object sender, NetworkedGameObjectManager.OnGameObjectDestroyedEventArgs e)
    {
        Debug.Log($"[NetworkedObjectSpawner] Object destroyed: ID {e.networkObjectId} owned by client {e.ownerClientId}");
        
        // Remove from our tracking list
        spawnedObjects.RemoveAll(obj => obj == null || obj.NetworkObjectId == e.networkObjectId);
    }

    private void OnOwnershipChanged(object sender, NetworkedGameObjectManager.OnOwnershipChangedEventArgs e)
    {
        Debug.Log($"[NetworkedObjectSpawner] Ownership changed for {e.networkObject?.name}: {e.previousOwner} → {e.newOwner}");
    }

    #endregion

    #region Utility Methods

    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 spawnPosition;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // Use predefined spawn points
            Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            spawnPosition = randomSpawnPoint.position;
            
            // Add some random offset
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            spawnPosition += new Vector3(randomOffset.x, 0, randomOffset.y);
        }
        else
        {
            // Use random position around this object
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            spawnPosition = transform.position + new Vector3(randomOffset.x, 0, randomOffset.y);
        }

        // Try to place on ground
        if (Physics.Raycast(spawnPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundLayerMask))
        {
            spawnPosition = hit.point;
        }

        return spawnPosition;
    }

    /// <summary>
    /// Gets statistics about spawned objects.
    /// </summary>
    public string GetSpawnStatistics()
    {
        if (NetworkedGameObjectManager.Instance == null)
            return "NetworkedGameObjectManager not available";

        int totalManaged = NetworkedGameObjectManager.Instance.GetTotalManagedObjects();
        int localOwned = NetworkedGameObjectManager.Instance.GetObjectsOwnedByClient(NetworkManager.Singleton.LocalClientId).Count;
        int localTracked = spawnedObjects.Count;

        return $"Total Managed: {totalManaged}, Locally Owned: {localOwned}, Locally Tracked: {localTracked}";
    }

    #endregion

    #region Server RPC Examples

    /// <summary>
    /// Example of spawning objects through server RPC for better control.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SpawnObjectServerRpc(int prefabId, Vector3 position, Quaternion rotation, ulong ownerClientId)
    {
        if (NetworkedGameObjectManager.Instance != null)
        {
            NetworkedGameObjectManager.Instance.SpawnNetworkedObject(prefabId, position, rotation, ownerClientId);
        }
    }

    /// <summary>
    /// Example of batch spawning multiple objects.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void BatchSpawnObjectsServerRpc(int[] prefabIds, Vector3[] positions, Quaternion[] rotations, ulong ownerClientId)
    {
        if (NetworkedGameObjectManager.Instance == null) return;

        int count = Mathf.Min(prefabIds.Length, Mathf.Min(positions.Length, rotations.Length));
        
        for (int i = 0; i < count; i++)
        {
            NetworkedGameObjectManager.Instance.SpawnNetworkedObject(prefabIds[i], positions[i], rotations[i], ownerClientId);
        }
    }

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        if (NetworkedGameObjectManager.Instance != null)
        {
            NetworkedGameObjectManager.Instance.OnGameObjectSpawned -= OnGameObjectSpawned;
            NetworkedGameObjectManager.Instance.OnGameObjectDestroyed -= OnGameObjectDestroyed;
            NetworkedGameObjectManager.Instance.OnOwnershipChanged -= OnOwnershipChanged;
        }
    }

    #endregion
}
