#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class SceneSettingsTab : INetcodeToolsTab
{
    public string TabName => "Scene Settings";
    public string TabTooltip => "Configure scene parameters";

    // Scene objects
    private GameObject m_SceneNetworkGameObject;
    private NetworkManager m_SceneNetworkManager;
    private UnityTransport m_SceneUnityTransport;

    // Network configuration (needed for creating new objects)
    private string m_CurrentIpAddress = "127.0.0.1";
    private int m_CurrentPort = 7777;
    private GameObject m_PlayerPrefab;

    private string m_LastIpAddress = "127.0.0.1";
    private int m_LastPort = 7777;
    private GameObject m_LastPlayerPrefab;

    public void DrawContent()
    {
        ApplySettings();

        GUILayout.Label("Scene Configuration", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox("Configure your scene settings here.", MessageType.Info);

        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();

        if (m_SceneNetworkGameObject == null)
            m_SceneNetworkGameObject = FindNetworkGameObject();

        m_SceneNetworkGameObject = (GameObject)EditorGUILayout.ObjectField("Network Gameobject", m_SceneNetworkGameObject, typeof(GameObject), false);

        if (m_SceneUnityTransport == null)
            if (GUILayout.Button(new GUIContent("Create", "Spawn a Network Manager & Network Transport."), GUI.skin.button, GUILayout.Height(17)))
            {
                CreateNetworkGameObject();
            }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Server Settings", EditorStyles.boldLabel);
        m_CurrentIpAddress = EditorGUILayout.TextField("Server Address", m_CurrentIpAddress);
        m_CurrentPort = EditorGUILayout.IntField("Port", m_CurrentPort);
        m_PlayerPrefab = (GameObject)EditorGUILayout.ObjectField("Player Prefab", m_PlayerPrefab, typeof(GameObject), false);
    }

    private GameObject FindNetworkGameObject()
    {
        m_SceneUnityTransport = Object.FindAnyObjectByType<UnityTransport>();
        m_SceneNetworkManager = Object.FindAnyObjectByType<NetworkManager>();

        if (m_SceneUnityTransport == null && m_SceneNetworkManager == null)
            return null;

        if (m_SceneUnityTransport && m_SceneNetworkManager == null)
        {
            m_SceneNetworkManager = m_SceneNetworkGameObject.AddComponent<NetworkManager>();
            m_SceneNetworkManager.NetworkConfig.NetworkTransport = m_SceneUnityTransport;
            m_SceneNetworkManager.NetworkConfig.PlayerPrefab = m_PlayerPrefab;

            return m_SceneUnityTransport.gameObject;
        }

        if (m_SceneNetworkManager && m_SceneUnityTransport == null)
        {
            m_SceneUnityTransport = m_SceneNetworkGameObject.AddComponent<UnityTransport>();
            m_SceneUnityTransport.ConnectionData.Address = m_CurrentIpAddress;
            m_SceneUnityTransport.ConnectionData.Port = (ushort)m_CurrentPort;

            return m_SceneNetworkManager.gameObject;
        }

        return m_SceneUnityTransport.gameObject;
    }

    private void CreateNetworkGameObject()
    {
        m_SceneNetworkGameObject = new GameObject();
        m_SceneNetworkGameObject.name = "Network Manager";

        m_SceneUnityTransport = m_SceneNetworkGameObject.AddComponent<UnityTransport>();
        m_SceneUnityTransport.ConnectionData.Address = m_CurrentIpAddress;
        m_SceneUnityTransport.ConnectionData.Port = (ushort)m_CurrentPort;

        m_SceneNetworkManager = m_SceneNetworkGameObject.AddComponent<NetworkManager>();
        m_SceneNetworkManager.NetworkConfig.NetworkTransport = m_SceneUnityTransport;
        m_SceneNetworkManager.NetworkConfig.PlayerPrefab = m_PlayerPrefab;
    }

    private void ApplySettings()
    {
        if (m_CurrentIpAddress != m_LastIpAddress)
        {
            m_SceneUnityTransport.ConnectionData.Address = m_CurrentIpAddress;
            m_LastIpAddress = m_CurrentIpAddress;
        }

        if (m_CurrentPort != m_LastPort)
        {
            m_SceneUnityTransport.ConnectionData.Port = (ushort)m_CurrentPort;
            m_LastPort = m_CurrentPort;
        }

        if (m_PlayerPrefab != m_LastPlayerPrefab)
        {
            m_SceneNetworkManager.NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            m_LastPlayerPrefab = m_PlayerPrefab;
        }
    }

    public void SetupContent()
    {
        m_SceneUnityTransport = Object.FindAnyObjectByType<UnityTransport>();
        m_SceneNetworkManager = Object.FindAnyObjectByType<NetworkManager>();

        if (m_SceneUnityTransport != null)
        {
            m_CurrentIpAddress = m_SceneUnityTransport.ConnectionData.Address;
            m_CurrentPort = m_SceneUnityTransport.ConnectionData.Port;
        }

        if (m_SceneNetworkManager != null)
        {
            m_PlayerPrefab = m_SceneNetworkManager.NetworkConfig.PlayerPrefab;
        }

    }
}

#endif
