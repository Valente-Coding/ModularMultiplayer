using UnityEngine;
using UnityEditor;
using Unity.Netcode;

#if UNITY_EDITOR
/// <summary>
/// Custom editor for MP_NetworkedObjectRegistry to provide better Unity Editor integration.
/// Allows for easy management of networked prefabs with validation and utilities.
/// </summary>
[CustomEditor(typeof(MP_NetworkedObjectRegistry))]
public class MP_NetworkedObjectRegistryEditor : Editor
{
    private MP_NetworkedObjectRegistry m_Registry;
    private GameObject m_PrefabToAdd;
    private string m_NewEntryName = "";
    private string m_NewEntryDescription = "";
    private bool m_ShowUtilities = false;
    private bool m_ShowInfo = false;

    private void OnEnable()
    {
        m_Registry = (MP_NetworkedObjectRegistry)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Header
        EditorGUILayout.Space();
        GUILayout.Label("Networked Object Registry", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Registry info
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RegistryName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RegistryDescription"));
        
        EditorGUILayout.Space();

        // Add new prefab section
        GUILayout.Label("Add New Prefab", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        m_NewEntryName = EditorGUILayout.TextField("Entry Name", m_NewEntryName);
        m_PrefabToAdd = (GameObject)EditorGUILayout.ObjectField("Prefab", m_PrefabToAdd, typeof(GameObject), false);
        m_NewEntryDescription = EditorGUILayout.TextField("Description", m_NewEntryDescription);
        
        EditorGUI.BeginDisabledGroup(m_PrefabToAdd == null || string.IsNullOrEmpty(m_NewEntryName));
        if (GUILayout.Button("Add Prefab to Registry"))
        {
            AddPrefabToRegistry();
        }
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Current prefabs list
        GUILayout.Label("Current Prefabs", EditorStyles.boldLabel);
        SerializedProperty l_PrefabEntries = serializedObject.FindProperty("m_PrefabEntries");
        
        if (l_PrefabEntries.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No prefabs in registry. Add some prefabs above.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.BeginVertical("box");
            
            for (int l_I = 0; l_I < l_PrefabEntries.arraySize; l_I++)
            {
                SerializedProperty l_Entry = l_PrefabEntries.GetArrayElementAtIndex(l_I);
                DrawPrefabEntry(l_Entry, l_I);
            }
            
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();

        // Utilities section
        m_ShowUtilities = EditorGUILayout.Foldout(m_ShowUtilities, "Utilities", true);
        if (m_ShowUtilities)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("Registry Management", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate Registry"))
            {
                m_Registry.ValidateRegistry();
                EditorUtility.SetDirty(m_Registry);
            }
            
            if (GUILayout.Button("Regenerate IDs"))
            {
                if (EditorUtility.DisplayDialog("Regenerate IDs", 
                    "This will reassign all prefab IDs sequentially. This may break existing references. Continue?", 
                    "Yes", "Cancel"))
                {
                    m_Registry.RegenerateIds();
                    EditorUtility.SetDirty(m_Registry);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Cache"))
            {
                m_Registry.ClearCache();
            }
            
            if (GUILayout.Button("Sort by Name"))
            {
                SortEntriesByName();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        // Info section
        m_ShowInfo = EditorGUILayout.Foldout(m_ShowInfo, "Registry Information", true);
        if (m_ShowInfo)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total Entries: {l_PrefabEntries.arraySize}");
            EditorGUILayout.LabelField($"Active Prefabs: {m_Registry.GetActivePrefabCount()}");
            
            if (GUILayout.Button("Show Detailed Info"))
            {
                Debug.Log(m_Registry.GetRegistryInfo());
            }
            
            EditorGUILayout.EndVertical();
        }

        // Validation warnings
        ValidateAndShowWarnings();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPrefabEntry(SerializedProperty p_Entry, int p_Index)
    {
        SerializedProperty l_EntryName = p_Entry.FindPropertyRelative("m_EntryName");
        SerializedProperty l_PrefabId = p_Entry.FindPropertyRelative("m_PrefabId");
        SerializedProperty l_Prefab = p_Entry.FindPropertyRelative("m_Prefab");
        SerializedProperty l_Description = p_Entry.FindPropertyRelative("m_Description");
        SerializedProperty l_IsActive = p_Entry.FindPropertyRelative("m_IsActive");

        EditorGUILayout.BeginVertical("box");
        
        // Header with active toggle and remove button
        EditorGUILayout.BeginHorizontal();
        
        l_IsActive.boolValue = EditorGUILayout.Toggle(l_IsActive.boolValue, GUILayout.Width(20));
        
        EditorGUILayout.LabelField($"ID: {l_PrefabId.intValue}", GUILayout.Width(60));
        
        EditorGUI.BeginDisabledGroup(!l_IsActive.boolValue);
        l_EntryName.stringValue = EditorGUILayout.TextField(l_EntryName.stringValue);
        EditorGUI.EndDisabledGroup();
        
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("×", GUILayout.Width(25)))
        {
            if (EditorUtility.DisplayDialog("Remove Entry", 
                $"Remove '{l_EntryName.stringValue}' from registry?", 
                "Remove", "Cancel"))
            {
                SerializedProperty l_PrefabEntries = serializedObject.FindProperty("m_PrefabEntries");
                l_PrefabEntries.DeleteArrayElementAtIndex(p_Index);
                EditorUtility.SetDirty(m_Registry);
                return;
            }
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();

        // Prefab field and description
        EditorGUI.BeginDisabledGroup(!l_IsActive.boolValue);
        
        GameObject l_CurrentPrefab = (GameObject)l_Prefab.objectReferenceValue;
        GameObject l_NewPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", l_CurrentPrefab, typeof(GameObject), false);
        
        if (l_NewPrefab != l_CurrentPrefab)
        {
            // Validate the new prefab
            if (l_NewPrefab != null && ValidatePrefab(l_NewPrefab))
            {
                l_Prefab.objectReferenceValue = l_NewPrefab;
            }
            else if (l_NewPrefab != null)
            {
                EditorUtility.DisplayDialog("Invalid Prefab", 
                    "The selected prefab does not have a NetworkObject component.", 
                    "OK");
            }
        }
        
        l_Description.stringValue = EditorGUILayout.TextField("Description", l_Description.stringValue);
        
        EditorGUI.EndDisabledGroup();

        // Show warnings for this entry
        if (l_Prefab.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Prefab is missing!", MessageType.Error);
        }
        else if (!ValidatePrefab((GameObject)l_Prefab.objectReferenceValue))
        {
            EditorGUILayout.HelpBox("Prefab does not have a NetworkObject component!", MessageType.Error);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    private void AddPrefabToRegistry()
    {
        if (m_PrefabToAdd == null || string.IsNullOrEmpty(m_NewEntryName))
            return;

        if (!ValidatePrefab(m_PrefabToAdd))
        {
            EditorUtility.DisplayDialog("Invalid Prefab", 
                "The selected prefab does not have a NetworkObject component.", 
                "OK");
            return;
        }

        if (m_Registry.AddPrefabEntry(m_NewEntryName, m_PrefabToAdd, m_NewEntryDescription))
        {
            // Clear fields
            m_NewEntryName = "";
            m_NewEntryDescription = "";
            m_PrefabToAdd = null;
            
            EditorUtility.SetDirty(m_Registry);
        }
    }

    private bool ValidatePrefab(GameObject p_Prefab)
    {
        if (p_Prefab == null) return false;
        return p_Prefab.GetComponent<NetworkObject>() != null;
    }

    private void ValidateAndShowWarnings()
    {
        SerializedProperty l_PrefabEntries = serializedObject.FindProperty("m_PrefabEntries");
        
        // Check for duplicates
        var l_PrefabIds = new System.Collections.Generic.HashSet<int>();
        var l_PrefabObjects = new System.Collections.Generic.HashSet<GameObject>();
        bool l_HasDuplicateIds = false;
        bool l_HasDuplicatePrefabs = false;
        bool l_HasInvalidPrefabs = false;

        for (int l_I = 0; l_I < l_PrefabEntries.arraySize; l_I++)
        {
            SerializedProperty l_Entry = l_PrefabEntries.GetArrayElementAtIndex(l_I);
            SerializedProperty l_PrefabId = l_Entry.FindPropertyRelative("m_PrefabId");
            SerializedProperty l_Prefab = l_Entry.FindPropertyRelative("m_Prefab");
            SerializedProperty l_IsActive = l_Entry.FindPropertyRelative("m_IsActive");

            if (!l_IsActive.boolValue) continue;

            // Check for duplicate IDs
            if (l_PrefabIds.Contains(l_PrefabId.intValue))
            {
                l_HasDuplicateIds = true;
            }
            else
            {
                l_PrefabIds.Add(l_PrefabId.intValue);
            }

            GameObject l_PrefabObj = (GameObject)l_Prefab.objectReferenceValue;
            
            // Check for duplicate prefabs
            if (l_PrefabObj != null)
            {
                if (l_PrefabObjects.Contains(l_PrefabObj))
                {
                    l_HasDuplicatePrefabs = true;
                }
                else
                {
                    l_PrefabObjects.Add(l_PrefabObj);
                }

                // Check for invalid prefabs
                if (!ValidatePrefab(l_PrefabObj))
                {
                    l_HasInvalidPrefabs = true;
                }
            }
            else
            {
                l_HasInvalidPrefabs = true;
            }
        }

        // Show warnings
        if (l_HasDuplicateIds)
        {
            EditorGUILayout.HelpBox("Duplicate prefab IDs detected! Use 'Regenerate IDs' to fix.", MessageType.Error);
        }

        if (l_HasDuplicatePrefabs)
        {
            EditorGUILayout.HelpBox("Duplicate prefab objects detected!", MessageType.Warning);
        }

        if (l_HasInvalidPrefabs)
        {
            EditorGUILayout.HelpBox("Some prefabs are missing or don't have NetworkObject components! Use 'Validate Registry' to clean up.", MessageType.Warning);
        }
    }

    private void SortEntriesByName()
    {
        SerializedProperty l_PrefabEntries = serializedObject.FindProperty("m_PrefabEntries");
        
        // Create a list to sort
        var l_Entries = new System.Collections.Generic.List<(string name, SerializedProperty prop)>();
        
        for (int l_I = 0; l_I < l_PrefabEntries.arraySize; l_I++)
        {
            SerializedProperty l_Entry = l_PrefabEntries.GetArrayElementAtIndex(l_I);
            SerializedProperty l_EntryName = l_Entry.FindPropertyRelative("m_EntryName");
            l_Entries.Add((l_EntryName.stringValue, l_Entry.Copy()));
        }
        
        // Sort by name
        l_Entries.Sort((p_A, p_B) => string.Compare(p_A.name, p_B.name, System.StringComparison.OrdinalIgnoreCase));
        
        // Clear and repopulate the array
        l_PrefabEntries.ClearArray();
        
        for (int l_I = 0; l_I < l_Entries.Count; l_I++)
        {
            l_PrefabEntries.InsertArrayElementAtIndex(l_I);
            SerializedProperty l_NewEntry = l_PrefabEntries.GetArrayElementAtIndex(l_I);
            
            // Copy properties
            l_NewEntry.FindPropertyRelative("m_EntryName").stringValue = l_Entries[l_I].prop.FindPropertyRelative("m_EntryName").stringValue;
            l_NewEntry.FindPropertyRelative("m_PrefabId").intValue = l_Entries[l_I].prop.FindPropertyRelative("m_PrefabId").intValue;
            l_NewEntry.FindPropertyRelative("m_Prefab").objectReferenceValue = l_Entries[l_I].prop.FindPropertyRelative("m_Prefab").objectReferenceValue;
            l_NewEntry.FindPropertyRelative("m_Description").stringValue = l_Entries[l_I].prop.FindPropertyRelative("m_Description").stringValue;
            l_NewEntry.FindPropertyRelative("m_IsActive").boolValue = l_Entries[l_I].prop.FindPropertyRelative("m_IsActive").boolValue;
        }
        
        EditorUtility.SetDirty(m_Registry);
    }
}

/// <summary>
/// Custom property drawer for NetworkedPrefabEntry to show it nicely in lists.
/// </summary>
[CustomPropertyDrawer(typeof(MP_NetworkedObjectRegistry.NetworkedPrefabEntry))]
public class NetworkedPrefabEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect p_Position, SerializedProperty p_Property, GUIContent p_Label)
    {
        EditorGUI.BeginProperty(p_Position, p_Label, p_Property);

        SerializedProperty l_EntryName = p_Property.FindPropertyRelative("m_EntryName");
        SerializedProperty l_PrefabId = p_Property.FindPropertyRelative("m_PrefabId");
        SerializedProperty l_Prefab = p_Property.FindPropertyRelative("m_Prefab");
        SerializedProperty l_IsActive = p_Property.FindPropertyRelative("m_IsActive");

        float l_LineHeight = EditorGUIUtility.singleLineHeight;
        Rect l_CurrentRect = new Rect(p_Position.x, p_Position.y, p_Position.width, l_LineHeight);

        // First line: Active toggle, ID, Name
        float l_ToggleWidth = 20f;
        float l_IdWidth = 60f;
        
        Rect l_ToggleRect = new Rect(l_CurrentRect.x, l_CurrentRect.y, l_ToggleWidth, l_CurrentRect.height);
        Rect l_IdRect = new Rect(l_ToggleRect.xMax + 2, l_CurrentRect.y, l_IdWidth, l_CurrentRect.height);
        Rect l_NameRect = new Rect(l_IdRect.xMax + 2, l_CurrentRect.y, l_CurrentRect.width - l_ToggleWidth - l_IdWidth - 4, l_CurrentRect.height);

        l_IsActive.boolValue = EditorGUI.Toggle(l_ToggleRect, l_IsActive.boolValue);
        EditorGUI.LabelField(l_IdRect, $"ID: {l_PrefabId.intValue}");
        
        EditorGUI.BeginDisabledGroup(!l_IsActive.boolValue);
        l_EntryName.stringValue = EditorGUI.TextField(l_NameRect, l_EntryName.stringValue);
        EditorGUI.EndDisabledGroup();

        // Second line: Prefab field
        l_CurrentRect.y += l_LineHeight + 2;
        EditorGUI.BeginDisabledGroup(!l_IsActive.boolValue);
        l_Prefab.objectReferenceValue = EditorGUI.ObjectField(l_CurrentRect, "Prefab", l_Prefab.objectReferenceValue, typeof(GameObject), false);
        EditorGUI.EndDisabledGroup();

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty p_Property, GUIContent p_Label)
    {
        return EditorGUIUtility.singleLineHeight * 2 + 2; // Two lines with spacing
    }
}
#endif
