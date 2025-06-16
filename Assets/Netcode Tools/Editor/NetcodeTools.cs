#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetcodeTools : EditorWindow
{
    private List<INetcodeToolsTab> m_MenuTabs;
    private int selectedMenuIndex = 0;
    private Vector2 scrollPosition;
    private const float SIDE_MENU_WIDTH = 200f;

    [MenuItem("Window/Netcode Tools")]
    private static void ShowWindow()
    {
        NetcodeTools window = GetWindow<NetcodeTools>();
        window.titleContent = new GUIContent("Netcode Tools");
        window.minSize = new Vector2(800, 600);
        window.maxSize = new Vector2(800, 600);
        window.Show();
    }

    private void SetupTabsContents()
    {
        if (m_MenuTabs == null || m_MenuTabs.Count == 0)
            return;

        m_MenuTabs.ForEach(p_Tab =>
        {
            p_Tab.SetupContent();
        });
    }

    private void OnEnable()
    {
        InitializeMenuItems();
    }

    private void InitializeMenuItems()
    {
        m_MenuTabs = new List<INetcodeToolsTab>
        {
            new SceneSettingsTab(),
        };

        SetupTabsContents();
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        // Draw Side Menu
        DrawSideMenu();

        // Draw Main Content Area
        DrawMainContent();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSideMenu()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(SIDE_MENU_WIDTH));

        GUILayout.Label("Netcode Tools", EditorStyles.boldLabel);
        GUILayout.Space(10);

        for (int i = 0; i < m_MenuTabs.Count; i++)
        {
            var menuItem = m_MenuTabs[i];

            // Highlight selected item
            var buttonStyle = selectedMenuIndex == i ?
                new GUIStyle(GUI.skin.button) { normal = { background = EditorGUIUtility.whiteTexture } } :
                GUI.skin.button;

            if (selectedMenuIndex == i)
            {
                GUI.backgroundColor = new Color(0.6f, 0.8f, 1f, 0.8f);
            }

            if (GUILayout.Button(new GUIContent(menuItem.TabName, menuItem.TabTooltip), buttonStyle, GUILayout.Height(30)))
            {
                selectedMenuIndex = i;
            }

            GUI.backgroundColor = Color.white;
            GUILayout.Space(2);
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();
    }

    private void DrawMainContent()
    {
        EditorGUILayout.BeginVertical("box");

        if (selectedMenuIndex >= 0 && selectedMenuIndex < m_MenuTabs.Count)
        {
            var selectedMenuItem = m_MenuTabs[selectedMenuIndex];

            GUILayout.Label(selectedMenuItem.TabName, EditorStyles.largeLabel);
            GUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            selectedMenuItem.DrawContent();
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.EndVertical();
    }
}
#endif