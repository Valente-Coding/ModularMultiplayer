using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject registry for storing prefab references that can be spawned
/// across the network. Provides a centralized way to manage networked prefabs
/// with unique IDs for multiplayer scenarios.
/// </summary>
[CreateAssetMenu(fileName = "MP_NetworkedObjectRegistry", menuName = "Multiplayer/Networked Object Registry")]
public class MP_NetworkedObjectRegistry : ScriptableObject
{
    [System.Serializable]
    public class NetworkedPrefabEntry
    {
        [SerializeField] private string m_EntryName;
        [SerializeField] private int m_PrefabId;
        [SerializeField] private GameObject m_Prefab;
        [SerializeField] private string m_Description;
        [SerializeField] private bool m_IsActive = true;

        public string EntryName => m_EntryName;
        public int PrefabId => m_PrefabId;
        public GameObject Prefab => m_Prefab;
        public string Description => m_Description;
        public bool IsActive => m_IsActive;

        public NetworkedPrefabEntry(string p_Name, int p_Id, GameObject p_PrefabObject, string p_Desc = "")
        {
            m_EntryName = p_Name;
            m_PrefabId = p_Id;
            m_Prefab = p_PrefabObject;
            m_Description = p_Desc;
            m_IsActive = true;
        }
    }

    [Header("Registry Configuration")]
    [SerializeField] private string m_RegistryName = "Default Registry";
    [SerializeField] private string m_RegistryDescription = "Registry for networked game objects";

    [Header("Prefab Entries")]
    [SerializeField] private List<NetworkedPrefabEntry> m_PrefabEntries = new List<NetworkedPrefabEntry>();

    // Runtime lookup cache
    private Dictionary<int, GameObject> m_PrefabCache;
    private Dictionary<GameObject, int> m_ReversePrefabCache;
    private bool m_CacheInitialized = false;

    #region Public API

    /// <summary>
    /// Gets a prefab by its unique ID.
    /// </summary>
    public GameObject GetPrefab(int p_PrefabId)
    {
        InitializeCacheIfNeeded();
        
        if (m_PrefabCache.ContainsKey(p_PrefabId))
        {
            return m_PrefabCache[p_PrefabId];
        }
        
        Debug.LogWarning($"[MP_NetworkedObjectRegistry] Prefab with ID {p_PrefabId} not found in registry '{m_RegistryName}'");
        return null;
    }

    /// <summary>
    /// Gets the prefab ID for a given GameObject prefab.
    /// </summary>
    public int GetPrefabId(GameObject p_Prefab)
    {
        InitializeCacheIfNeeded();
        
        if (m_ReversePrefabCache.ContainsKey(p_Prefab))
        {
            return m_ReversePrefabCache[p_Prefab];
        }
        
        Debug.LogWarning($"[MP_NetworkedObjectRegistry] Prefab '{p_Prefab.name}' not found in registry '{m_RegistryName}'");
        return -1;
    }

    /// <summary>
    /// Checks if a prefab ID exists in the registry and is active.
    /// </summary>
    public bool IsValidPrefabId(int p_PrefabId)
    {
        InitializeCacheIfNeeded();
        return m_PrefabCache.ContainsKey(p_PrefabId);
    }

    /// <summary>
    /// Gets all active prefab entries.
    /// </summary>
    public List<NetworkedPrefabEntry> GetAllActivePrefabs()
    {
        List<NetworkedPrefabEntry> l_ActivePrefabs = new List<NetworkedPrefabEntry>();
        
        foreach (var l_Entry in m_PrefabEntries)
        {
            if (l_Entry.IsActive && l_Entry.Prefab != null)
            {
                l_ActivePrefabs.Add(l_Entry);
            }
        }
        
        return l_ActivePrefabs;
    }

    /// <summary>
    /// Gets a prefab entry by ID.
    /// </summary>
    public NetworkedPrefabEntry GetPrefabEntry(int p_PrefabId)
    {
        foreach (var l_Entry in m_PrefabEntries)
        {
            if (l_Entry.PrefabId == p_PrefabId && l_Entry.IsActive)
            {
                return l_Entry;
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
        return new List<int>(m_PrefabCache.Keys);
    }

    /// <summary>
    /// Gets the total number of active prefabs in the registry.
    /// </summary>
    public int GetActivePrefabCount()
    {
        InitializeCacheIfNeeded();
        return m_PrefabCache.Count;
    }

    #endregion

    #region Editor Support

#if UNITY_EDITOR
    /// <summary>
    /// Adds a new prefab entry to the registry (Editor only).
    /// </summary>
    public bool AddPrefabEntry(string p_EntryName, GameObject p_Prefab, string p_Description = "")
    {
        if (p_Prefab == null)
        {
            Debug.LogError("[MP_NetworkedObjectRegistry] Cannot add null prefab to registry");
            return false;
        }

        // Check if prefab already exists
        foreach (var l_Entry in m_PrefabEntries)
        {
            if (l_Entry.Prefab == p_Prefab)
            {
                Debug.LogWarning($"[MP_NetworkedObjectRegistry] Prefab '{p_Prefab.name}' already exists in registry");
                return false;
            }
        }

        // Generate unique ID
        int l_NewId = GenerateUniquePrefabId();
        
        // Create new entry
        NetworkedPrefabEntry l_NewEntry = new NetworkedPrefabEntry(p_EntryName, l_NewId, p_Prefab, p_Description);
        m_PrefabEntries.Add(l_NewEntry);
        
        // Invalidate cache
        m_CacheInitialized = false;
        
        Debug.Log($"[MP_NetworkedObjectRegistry] Added prefab '{p_EntryName}' with ID {l_NewId} to registry '{m_RegistryName}'");
        return true;
    }

    /// <summary>
    /// Removes a prefab entry from the registry (Editor only).
    /// </summary>
    public bool RemovePrefabEntry(int p_PrefabId)
    {
        for (int l_I = 0; l_I < m_PrefabEntries.Count; l_I++)
        {
            if (m_PrefabEntries[l_I].PrefabId == p_PrefabId)
            {
                string l_EntryName = m_PrefabEntries[l_I].EntryName;
                m_PrefabEntries.RemoveAt(l_I);
                
                // Invalidate cache
                m_CacheInitialized = false;
                
                Debug.Log($"[MP_NetworkedObjectRegistry] Removed prefab entry '{l_EntryName}' with ID {p_PrefabId} from registry '{m_RegistryName}'");
                return true;
            }
        }
        
        Debug.LogWarning($"[MP_NetworkedObjectRegistry] Prefab with ID {p_PrefabId} not found in registry '{m_RegistryName}'");
        return false;
    }

    /// <summary>
    /// Validates the registry and removes invalid entries (Editor only).
    /// </summary>
    public void ValidateRegistry()
    {
        List<NetworkedPrefabEntry> l_ValidEntries = new List<NetworkedPrefabEntry>();
        bool l_HasChanges = false;

        foreach (var l_Entry in m_PrefabEntries)
        {
            if (l_Entry.Prefab != null)
            {
                // Check if prefab has NetworkObject component
                if (l_Entry.Prefab.GetComponent<Unity.Netcode.NetworkObject>() != null)
                {
                    l_ValidEntries.Add(l_Entry);
                }
                else
                {
                    Debug.LogWarning($"[MP_NetworkedObjectRegistry] Removing entry '{l_Entry.EntryName}' - prefab does not have NetworkObject component");
                    l_HasChanges = true;
                }
            }
            else
            {
                Debug.LogWarning($"[MP_NetworkedObjectRegistry] Removing entry '{l_Entry.EntryName}' - prefab is null");
                l_HasChanges = true;
            }
        }

        if (l_HasChanges)
        {
            m_PrefabEntries = l_ValidEntries;
            m_CacheInitialized = false;
            Debug.Log($"[MP_NetworkedObjectRegistry] Registry '{m_RegistryName}' validated and cleaned up");
        }
    }

    /// <summary>
    /// Regenerates all prefab IDs to ensure they are sequential (Editor only).
    /// </summary>
    public void RegenerateIds()
    {
        for (int l_I = 0; l_I < m_PrefabEntries.Count; l_I++)
        {
            // Use reflection to set the private prefabId field
            var l_Entry = m_PrefabEntries[l_I];
            var l_Field = typeof(NetworkedPrefabEntry).GetField("m_PrefabId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            l_Field?.SetValue(l_Entry, l_I);
        }
        
        m_CacheInitialized = false;
        Debug.Log($"[MP_NetworkedObjectRegistry] Regenerated IDs for registry '{m_RegistryName}'");
    }

    private int GenerateUniquePrefabId()
    {
        HashSet<int> l_ExistingIds = new HashSet<int>();
        foreach (var l_Entry in m_PrefabEntries)
        {
            l_ExistingIds.Add(l_Entry.PrefabId);
        }

        int l_NewId = 0;
        while (l_ExistingIds.Contains(l_NewId))
        {
            l_NewId++;
        }

        return l_NewId;
    }
#endif

    #endregion

    #region Private Methods

    private void InitializeCacheIfNeeded()
    {
        if (m_CacheInitialized) return;

        m_PrefabCache = new Dictionary<int, GameObject>();
        m_ReversePrefabCache = new Dictionary<GameObject, int>();

        foreach (var l_Entry in m_PrefabEntries)
        {
            if (l_Entry.IsActive && l_Entry.Prefab != null)
            {
                // Check for duplicate IDs
                if (m_PrefabCache.ContainsKey(l_Entry.PrefabId))
                {
                    Debug.LogError($"[MP_NetworkedObjectRegistry] Duplicate prefab ID {l_Entry.PrefabId} found in registry '{m_RegistryName}'. Entry '{l_Entry.EntryName}' will be ignored.");
                    continue;
                }

                m_PrefabCache[l_Entry.PrefabId] = l_Entry.Prefab;
                m_ReversePrefabCache[l_Entry.Prefab] = l_Entry.PrefabId;
            }
        }

        m_CacheInitialized = true;
        Debug.Log($"[MP_NetworkedObjectRegistry] Initialized cache for registry '{m_RegistryName}' with {m_PrefabCache.Count} entries");
    }

    /// <summary>
    /// Clears the runtime cache. Useful when registry changes at runtime.
    /// </summary>
    public void ClearCache()
    {
        m_CacheInitialized = false;
        m_PrefabCache?.Clear();
        m_ReversePrefabCache?.Clear();
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
        
        System.Text.StringBuilder l_Info = new System.Text.StringBuilder();
        l_Info.AppendLine($"Registry Name: {m_RegistryName}");
        l_Info.AppendLine($"Description: {m_RegistryDescription}");
        l_Info.AppendLine($"Active Prefabs: {m_PrefabCache.Count}");
        l_Info.AppendLine($"Total Entries: {m_PrefabEntries.Count}");
        
        if (m_PrefabCache.Count > 0)
        {
            l_Info.AppendLine("\nActive Prefab Entries:");
            foreach (var l_Entry in GetAllActivePrefabs())
            {
                l_Info.AppendLine($"  ID {l_Entry.PrefabId}: {l_Entry.EntryName} ({l_Entry.Prefab.name})");
                if (!string.IsNullOrEmpty(l_Entry.Description))
                {
                    l_Info.AppendLine($"    Description: {l_Entry.Description}");
                }
            }
        }
        
        return l_Info.ToString();
    }

    #endregion
}
