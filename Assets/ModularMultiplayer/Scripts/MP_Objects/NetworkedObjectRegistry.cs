using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject registry for storing prefab references that can be spawned
/// across the network. Provides a centralized way to manage networked prefabs
/// with unique IDs for multiplayer scenarios.
/// </summary>
[CreateAssetMenu(fileName = "NetworkedObjectRegistry", menuName = "Multiplayer/Networked Object Registry")]
public class NetworkedObjectRegistry : ScriptableObject
{
    [System.Serializable]
    public class NetworkedPrefabEntry
    {
        [SerializeField] private string entryName;
        [SerializeField] private int prefabId;
        [SerializeField] private GameObject prefab;
        [SerializeField] private string description;
        [SerializeField] private bool isActive = true;

        public string EntryName => entryName;
        public int PrefabId => prefabId;
        public GameObject Prefab => prefab;
        public string Description => description;
        public bool IsActive => isActive;

        public NetworkedPrefabEntry(string name, int id, GameObject prefabObject, string desc = "")
        {
            entryName = name;
            prefabId = id;
            prefab = prefabObject;
            description = desc;
            isActive = true;
        }
    }

    [Header("Registry Configuration")]
    [SerializeField] private string registryName = "Default Registry";
    [SerializeField] private string registryDescription = "Registry for networked game objects";

    [Header("Prefab Entries")]
    [SerializeField] private List<NetworkedPrefabEntry> prefabEntries = new List<NetworkedPrefabEntry>();

    // Runtime lookup cache
    private Dictionary<int, GameObject> prefabCache;
    private Dictionary<GameObject, int> reversePrefabCache;
    private bool cacheInitialized = false;

    #region Public API

    /// <summary>
    /// Gets a prefab by its unique ID.
    /// </summary>
    public GameObject GetPrefab(int prefabId)
    {
        InitializeCacheIfNeeded();
        
        if (prefabCache.ContainsKey(prefabId))
        {
            return prefabCache[prefabId];
        }
        
        Debug.LogWarning($"[NetworkedObjectRegistry] Prefab with ID {prefabId} not found in registry '{registryName}'");
        return null;
    }

    /// <summary>
    /// Gets the prefab ID for a given GameObject prefab.
    /// </summary>
    public int GetPrefabId(GameObject prefab)
    {
        InitializeCacheIfNeeded();
        
        if (reversePrefabCache.ContainsKey(prefab))
        {
            return reversePrefabCache[prefab];
        }
        
        Debug.LogWarning($"[NetworkedObjectRegistry] Prefab '{prefab.name}' not found in registry '{registryName}'");
        return -1;
    }

    /// <summary>
    /// Checks if a prefab ID exists in the registry and is active.
    /// </summary>
    public bool IsValidPrefabId(int prefabId)
    {
        InitializeCacheIfNeeded();
        return prefabCache.ContainsKey(prefabId);
    }

    /// <summary>
    /// Gets all active prefab entries.
    /// </summary>
    public List<NetworkedPrefabEntry> GetAllActivePrefabs()
    {
        List<NetworkedPrefabEntry> activePrefabs = new List<NetworkedPrefabEntry>();
        
        foreach (var entry in prefabEntries)
        {
            if (entry.IsActive && entry.Prefab != null)
            {
                activePrefabs.Add(entry);
            }
        }
        
        return activePrefabs;
    }

    /// <summary>
    /// Gets a prefab entry by ID.
    /// </summary>
    public NetworkedPrefabEntry GetPrefabEntry(int prefabId)
    {
        foreach (var entry in prefabEntries)
        {
            if (entry.PrefabId == prefabId && entry.IsActive)
            {
                return entry;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Gets all prefab IDs in the registry.
    /// </summary>
    public List<int> GetAllPrefabIds()
    {
        InitializeCacheIfNeeded();
        return new List<int>(prefabCache.Keys);
    }

    /// <summary>
    /// Gets the total number of active prefabs in the registry.
    /// </summary>
    public int GetActivePrefabCount()
    {
        InitializeCacheIfNeeded();
        return prefabCache.Count;
    }

    #endregion

    #region Editor Support

#if UNITY_EDITOR
    /// <summary>
    /// Adds a new prefab entry to the registry (Editor only).
    /// </summary>
    public bool AddPrefabEntry(string entryName, GameObject prefab, string description = "")
    {
        if (prefab == null)
        {
            Debug.LogError("[NetworkedObjectRegistry] Cannot add null prefab to registry");
            return false;
        }

        // Check if prefab already exists
        foreach (var entry in prefabEntries)
        {
            if (entry.Prefab == prefab)
            {
                Debug.LogWarning($"[NetworkedObjectRegistry] Prefab '{prefab.name}' already exists in registry");
                return false;
            }
        }

        // Generate unique ID
        int newId = GenerateUniquePrefabId();
        
        // Create new entry
        NetworkedPrefabEntry newEntry = new NetworkedPrefabEntry(entryName, newId, prefab, description);
        prefabEntries.Add(newEntry);
        
        // Invalidate cache
        cacheInitialized = false;
        
        Debug.Log($"[NetworkedObjectRegistry] Added prefab '{entryName}' with ID {newId} to registry '{registryName}'");
        return true;
    }

    /// <summary>
    /// Removes a prefab entry from the registry (Editor only).
    /// </summary>
    public bool RemovePrefabEntry(int prefabId)
    {
        for (int i = 0; i < prefabEntries.Count; i++)
        {
            if (prefabEntries[i].PrefabId == prefabId)
            {
                string entryName = prefabEntries[i].EntryName;
                prefabEntries.RemoveAt(i);
                
                // Invalidate cache
                cacheInitialized = false;
                
                Debug.Log($"[NetworkedObjectRegistry] Removed prefab entry '{entryName}' with ID {prefabId} from registry '{registryName}'");
                return true;
            }
        }
        
        Debug.LogWarning($"[NetworkedObjectRegistry] Prefab with ID {prefabId} not found in registry '{registryName}'");
        return false;
    }

    /// <summary>
    /// Validates the registry and removes invalid entries (Editor only).
    /// </summary>
    public void ValidateRegistry()
    {
        List<NetworkedPrefabEntry> validEntries = new List<NetworkedPrefabEntry>();
        bool hasChanges = false;

        foreach (var entry in prefabEntries)
        {
            if (entry.Prefab != null)
            {
                // Check if prefab has NetworkObject component
                if (entry.Prefab.GetComponent<Unity.Netcode.NetworkObject>() != null)
                {
                    validEntries.Add(entry);
                }
                else
                {
                    Debug.LogWarning($"[NetworkedObjectRegistry] Removing entry '{entry.EntryName}' - prefab does not have NetworkObject component");
                    hasChanges = true;
                }
            }
            else
            {
                Debug.LogWarning($"[NetworkedObjectRegistry] Removing entry '{entry.EntryName}' - prefab is null");
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            prefabEntries = validEntries;
            cacheInitialized = false;
            Debug.Log($"[NetworkedObjectRegistry] Registry '{registryName}' validated and cleaned up");
        }
    }

    /// <summary>
    /// Regenerates all prefab IDs to ensure they are sequential (Editor only).
    /// </summary>
    public void RegenerateIds()
    {
        for (int i = 0; i < prefabEntries.Count; i++)
        {
            // Use reflection to set the private prefabId field
            var entry = prefabEntries[i];
            var field = typeof(NetworkedPrefabEntry).GetField("prefabId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(entry, i);
        }
        
        cacheInitialized = false;
        Debug.Log($"[NetworkedObjectRegistry] Regenerated IDs for registry '{registryName}'");
    }

    private int GenerateUniquePrefabId()
    {
        HashSet<int> existingIds = new HashSet<int>();
        foreach (var entry in prefabEntries)
        {
            existingIds.Add(entry.PrefabId);
        }

        int newId = 0;
        while (existingIds.Contains(newId))
        {
            newId++;
        }

        return newId;
    }
#endif

    #endregion

    #region Private Methods

    private void InitializeCacheIfNeeded()
    {
        if (cacheInitialized) return;

        prefabCache = new Dictionary<int, GameObject>();
        reversePrefabCache = new Dictionary<GameObject, int>();

        foreach (var entry in prefabEntries)
        {
            if (entry.IsActive && entry.Prefab != null)
            {
                // Check for duplicate IDs
                if (prefabCache.ContainsKey(entry.PrefabId))
                {
                    Debug.LogError($"[NetworkedObjectRegistry] Duplicate prefab ID {entry.PrefabId} found in registry '{registryName}'. Entry '{entry.EntryName}' will be ignored.");
                    continue;
                }

                prefabCache[entry.PrefabId] = entry.Prefab;
                reversePrefabCache[entry.Prefab] = entry.PrefabId;
            }
        }

        cacheInitialized = true;
        Debug.Log($"[NetworkedObjectRegistry] Initialized cache for registry '{registryName}' with {prefabCache.Count} entries");
    }

    /// <summary>
    /// Clears the runtime cache. Useful when registry changes at runtime.
    /// </summary>
    public void ClearCache()
    {
        cacheInitialized = false;
        prefabCache?.Clear();
        reversePrefabCache?.Clear();
    }

    #endregion

    #region Unity Callbacks

    private void OnValidate()
    {
        // Clear cache when registry is modified in editor
        ClearCache();
    }

    #endregion

    #region Information Methods

    /// <summary>
    /// Gets registry information as a formatted string.
    /// </summary>
    public string GetRegistryInfo()
    {
        InitializeCacheIfNeeded();
        
        System.Text.StringBuilder info = new System.Text.StringBuilder();
        info.AppendLine($"Registry Name: {registryName}");
        info.AppendLine($"Description: {registryDescription}");
        info.AppendLine($"Active Prefabs: {prefabCache.Count}");
        info.AppendLine($"Total Entries: {prefabEntries.Count}");
        
        if (prefabCache.Count > 0)
        {
            info.AppendLine("\nActive Prefab Entries:");
            foreach (var entry in GetAllActivePrefabs())
            {
                info.AppendLine($"  ID {entry.PrefabId}: {entry.EntryName} ({entry.Prefab.name})");
                if (!string.IsNullOrEmpty(entry.Description))
                {
                    info.AppendLine($"    Description: {entry.Description}");
                }
            }
        }
        
        return info.ToString();
    }

    #endregion
}
