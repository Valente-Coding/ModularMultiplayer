using UnityEngine;
using UnityEditor;
using Unity.Netcode;

#if UNITY_EDITOR
/// <summary>
/// Custom editor for NetworkedObjectRegistry to provide better Unity Editor integration.
/// Allows for easy management of networked prefabs with validation and utilities.
/// </summary>
[CustomEditor(typeof(NetworkedObjectRegistry))]
public class NetworkedObjectRegistryEditor : Editor
{
    private NetworkedObjectRegistry registry;
    private GameObject prefabToAdd;
    private string newEntryName = "";
    private string newEntryDescription = "";
    private bool showUtilities = false;
    private bool showInfo = false;

    private void OnEnable()
    {
        registry = (NetworkedObjectRegistry)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Header
        EditorGUILayout.Space();
        GUILayout.Label("Networked Object Registry", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Registry info
        EditorGUILayout.PropertyField(serializedObject.FindProperty("registryName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("registryDescription"));
        
        EditorGUILayout.Space();

        // Add new prefab section
        GUILayout.Label("Add New Prefab", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        newEntryName = EditorGUILayout.TextField("Entry Name", newEntryName);
        prefabToAdd = (GameObject)EditorGUILayout.ObjectField("Prefab", prefabToAdd, typeof(GameObject), false);
        newEntryDescription = EditorGUILayout.TextField("Description", newEntryDescription);
        
        EditorGUI.BeginDisabledGroup(prefabToAdd == null || string.IsNullOrEmpty(newEntryName));
        if (GUILayout.Button("Add Prefab to Registry"))
        {
            AddPrefabToRegistry();
        }
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Current prefabs list
        GUILayout.Label("Current Prefabs", EditorStyles.boldLabel);
        SerializedProperty prefabEntries = serializedObject.FindProperty("prefabEntries");
        
        if (prefabEntries.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No prefabs in registry. Add some prefabs above.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.BeginVertical("box");
            
            for (int i = 0; i < prefabEntries.arraySize; i++)
            {
                SerializedProperty entry = prefabEntries.GetArrayElementAtIndex(i);
                DrawPrefabEntry(entry, i);
            }
            
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();

        // Utilities section
        showUtilities = EditorGUILayout.Foldout(showUtilities, "Utilities", true);
        if (showUtilities)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("Registry Management", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate Registry"))
            {
                registry.ValidateRegistry();
                EditorUtility.SetDirty(registry);
            }
            
            if (GUILayout.Button("Regenerate IDs"))
            {
                if (EditorUtility.DisplayDialog("Regenerate IDs", 
                    "This will reassign all prefab IDs sequentially. This may break existing references. Continue?", 
                    "Yes", "Cancel"))
                {
                    registry.RegenerateIds();
                    EditorUtility.SetDirty(registry);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Cache"))
            {
                registry.ClearCache();
            }
            
            if (GUILayout.Button("Sort by Name"))
            {
                SortEntriesByName();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        // Info section
        showInfo = EditorGUILayout.Foldout(showInfo, "Registry Information", true);
        if (showInfo)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total Entries: {prefabEntries.arraySize}");
            EditorGUILayout.LabelField($"Active Prefabs: {registry.GetActivePrefabCount()}");
            
            if (GUILayout.Button("Show Detailed Info"))
            {
                Debug.Log(registry.GetRegistryInfo());
            }
            
            EditorGUILayout.EndVertical();
        }

        // Validation warnings
        ValidateAndShowWarnings();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPrefabEntry(SerializedProperty entry, int index)
    {
        SerializedProperty entryName = entry.FindPropertyRelative("entryName");
        SerializedProperty prefabId = entry.FindPropertyRelative("prefabId");
        SerializedProperty prefab = entry.FindPropertyRelative("prefab");
        SerializedProperty description = entry.FindPropertyRelative("description");
        SerializedProperty isActive = entry.FindPropertyRelative("isActive");

        EditorGUILayout.BeginVertical("box");
        
        // Header with active toggle and remove button
        EditorGUILayout.BeginHorizontal();
        
        isActive.boolValue = EditorGUILayout.Toggle(isActive.boolValue, GUILayout.Width(20));
        
        EditorGUILayout.LabelField($"ID: {prefabId.intValue}", GUILayout.Width(60));
        
        EditorGUI.BeginDisabledGroup(!isActive.boolValue);
        entryName.stringValue = EditorGUILayout.TextField(entryName.stringValue);
        EditorGUI.EndDisabledGroup();
        
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("×", GUILayout.Width(25)))
        {
            if (EditorUtility.DisplayDialog("Remove Entry", 
                $"Remove '{entryName.stringValue}' from registry?", 
                "Remove", "Cancel"))
            {
                SerializedProperty prefabEntries = serializedObject.FindProperty("prefabEntries");
                prefabEntries.DeleteArrayElementAtIndex(index);
                EditorUtility.SetDirty(registry);
                return;
            }
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();

        // Prefab field and description
        EditorGUI.BeginDisabledGroup(!isActive.boolValue);
        
        GameObject currentPrefab = (GameObject)prefab.objectReferenceValue;
        GameObject newPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", currentPrefab, typeof(GameObject), false);
        
        if (newPrefab != currentPrefab)
        {
            // Validate the new prefab
            if (newPrefab != null && ValidatePrefab(newPrefab))
            {
                prefab.objectReferenceValue = newPrefab;
            }
            else if (newPrefab != null)
            {
                EditorUtility.DisplayDialog("Invalid Prefab", 
                    "The selected prefab does not have a NetworkObject component.", 
                    "OK");
            }
        }
        
        description.stringValue = EditorGUILayout.TextField("Description", description.stringValue);
        
        EditorGUI.EndDisabledGroup();

        // Show warnings for this entry
        if (prefab.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Prefab is missing!", MessageType.Error);
        }
        else if (!ValidatePrefab((GameObject)prefab.objectReferenceValue))
        {
            EditorGUILayout.HelpBox("Prefab does not have a NetworkObject component!", MessageType.Error);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    private void AddPrefabToRegistry()
    {
        if (prefabToAdd == null || string.IsNullOrEmpty(newEntryName))
            return;

        if (!ValidatePrefab(prefabToAdd))
        {
            EditorUtility.DisplayDialog("Invalid Prefab", 
                "The selected prefab does not have a NetworkObject component.", 
                "OK");
            return;
        }

        if (registry.AddPrefabEntry(newEntryName, prefabToAdd, newEntryDescription))
        {
            // Clear fields
            newEntryName = "";
            newEntryDescription = "";
            prefabToAdd = null;
            
            EditorUtility.SetDirty(registry);
        }
    }

    private bool ValidatePrefab(GameObject prefab)
    {
        if (prefab == null) return false;
        return prefab.GetComponent<NetworkObject>() != null;
    }

    private void ValidateAndShowWarnings()
    {
        SerializedProperty prefabEntries = serializedObject.FindProperty("prefabEntries");
        
        // Check for duplicates
        var prefabIds = new System.Collections.Generic.HashSet<int>();
        var prefabObjects = new System.Collections.Generic.HashSet<GameObject>();
        bool hasDuplicateIds = false;
        bool hasDuplicatePrefabs = false;
        bool hasInvalidPrefabs = false;

        for (int i = 0; i < prefabEntries.arraySize; i++)
        {
            SerializedProperty entry = prefabEntries.GetArrayElementAtIndex(i);
            SerializedProperty prefabId = entry.FindPropertyRelative("prefabId");
            SerializedProperty prefab = entry.FindPropertyRelative("prefab");
            SerializedProperty isActive = entry.FindPropertyRelative("isActive");

            if (!isActive.boolValue) continue;

            // Check for duplicate IDs
            if (prefabIds.Contains(prefabId.intValue))
            {
                hasDuplicateIds = true;
            }
            else
            {
                prefabIds.Add(prefabId.intValue);
            }

            GameObject prefabObj = (GameObject)prefab.objectReferenceValue;
            
            // Check for duplicate prefabs
            if (prefabObj != null)
            {
                if (prefabObjects.Contains(prefabObj))
                {
                    hasDuplicatePrefabs = true;
                }
                else
                {
                    prefabObjects.Add(prefabObj);
                }

                // Check for invalid prefabs
                if (!ValidatePrefab(prefabObj))
                {
                    hasInvalidPrefabs = true;
                }
            }
            else
            {
                hasInvalidPrefabs = true;
            }
        }

        // Show warnings
        if (hasDuplicateIds)
        {
            EditorGUILayout.HelpBox("Duplicate prefab IDs detected! Use 'Regenerate IDs' to fix.", MessageType.Error);
        }

        if (hasDuplicatePrefabs)
        {
            EditorGUILayout.HelpBox("Duplicate prefab objects detected!", MessageType.Warning);
        }

        if (hasInvalidPrefabs)
        {
            EditorGUILayout.HelpBox("Some prefabs are missing or don't have NetworkObject components! Use 'Validate Registry' to clean up.", MessageType.Warning);
        }
    }

    private void SortEntriesByName()
    {
        SerializedProperty prefabEntries = serializedObject.FindProperty("prefabEntries");
        
        // Create a list to sort
        var entries = new System.Collections.Generic.List<(string name, SerializedProperty prop)>();
        
        for (int i = 0; i < prefabEntries.arraySize; i++)
        {
            SerializedProperty entry = prefabEntries.GetArrayElementAtIndex(i);
            SerializedProperty entryName = entry.FindPropertyRelative("entryName");
            entries.Add((entryName.stringValue, entry.Copy()));
        }
        
        // Sort by name
        entries.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
        
        // Clear and repopulate the array
        prefabEntries.ClearArray();
        
        for (int i = 0; i < entries.Count; i++)
        {
            prefabEntries.InsertArrayElementAtIndex(i);
            SerializedProperty newEntry = prefabEntries.GetArrayElementAtIndex(i);
            
            // Copy properties
            newEntry.FindPropertyRelative("entryName").stringValue = entries[i].prop.FindPropertyRelative("entryName").stringValue;
            newEntry.FindPropertyRelative("prefabId").intValue = entries[i].prop.FindPropertyRelative("prefabId").intValue;
            newEntry.FindPropertyRelative("prefab").objectReferenceValue = entries[i].prop.FindPropertyRelative("prefab").objectReferenceValue;
            newEntry.FindPropertyRelative("description").stringValue = entries[i].prop.FindPropertyRelative("description").stringValue;
            newEntry.FindPropertyRelative("isActive").boolValue = entries[i].prop.FindPropertyRelative("isActive").boolValue;
        }
        
        EditorUtility.SetDirty(registry);
    }
}

/// <summary>
/// Custom property drawer for NetworkedPrefabEntry to show it nicely in lists.
/// </summary>
[CustomPropertyDrawer(typeof(NetworkedObjectRegistry.NetworkedPrefabEntry))]
public class NetworkedPrefabEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty entryName = property.FindPropertyRelative("entryName");
        SerializedProperty prefabId = property.FindPropertyRelative("prefabId");
        SerializedProperty prefab = property.FindPropertyRelative("prefab");
        SerializedProperty isActive = property.FindPropertyRelative("isActive");

        float lineHeight = EditorGUIUtility.singleLineHeight;
        Rect currentRect = new Rect(position.x, position.y, position.width, lineHeight);

        // First line: Active toggle, ID, Name
        float toggleWidth = 20f;
        float idWidth = 60f;
        
        Rect toggleRect = new Rect(currentRect.x, currentRect.y, toggleWidth, currentRect.height);
        Rect idRect = new Rect(toggleRect.xMax + 2, currentRect.y, idWidth, currentRect.height);
        Rect nameRect = new Rect(idRect.xMax + 2, currentRect.y, currentRect.width - toggleWidth - idWidth - 4, currentRect.height);

        isActive.boolValue = EditorGUI.Toggle(toggleRect, isActive.boolValue);
        EditorGUI.LabelField(idRect, $"ID: {prefabId.intValue}");
        
        EditorGUI.BeginDisabledGroup(!isActive.boolValue);
        entryName.stringValue = EditorGUI.TextField(nameRect, entryName.stringValue);
        EditorGUI.EndDisabledGroup();

        // Second line: Prefab field
        currentRect.y += lineHeight + 2;
        EditorGUI.BeginDisabledGroup(!isActive.boolValue);
        prefab.objectReferenceValue = EditorGUI.ObjectField(currentRect, "Prefab", prefab.objectReferenceValue, typeof(GameObject), false);
        EditorGUI.EndDisabledGroup();

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 2 + 2; // Two lines with spacing
    }
}
#endif
