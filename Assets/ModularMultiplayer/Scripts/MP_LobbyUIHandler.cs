using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;

public class MP_LobbyUIHandler : MonoBehaviour
{    [Header("UI References")]
    [SerializeField] private GameObject m_MainMenuPanel;
    [SerializeField] private GameObject m_LobbyPanel;
    [SerializeField] private GameObject m_WaitingPanel;
    
    [Header("Main Menu Components")]
    [SerializeField] private Button m_CreateButton;
    [SerializeField] private Button m_JoinButton;
    [SerializeField] private Button m_QuickJoinButton;
    [SerializeField] private TMP_InputField m_JoinCodeInput;
    
    [Header("Lobby Components")]
    [SerializeField] private TextMeshProUGUI m_SessionCodeText;
    [SerializeField] private TextMeshProUGUI m_PlayerCountText;
    [SerializeField] private Button m_StartGameButton;
    [SerializeField] private Button m_LeaveLobbyButton;
    
    [Header("Waiting Panel Components")]
    [SerializeField] private Button m_CancelButton;
    [SerializeField] private TextMeshProUGUI m_StatusText;
      [Header("Session Settings")]
    [SerializeField] private int m_MaxPlayers = 4;
    [SerializeField] private string m_MatchmakingQueue = "Default";
    
    [Header("Player List")]
    [SerializeField] private GameObject m_PlayerListGO;
    [SerializeField] private GameObject m_PlayerRowPrefab;

    private bool m_IsJoining = false;    
    
    private void Start()
    {
        // Initialize UI
        SetupButtonListeners();
        ShowMainMenu();
        
        // Subscribe to session events
        MP_SessionManager.Instance.OnSessionCreated += OnSessionCreated;
        MP_SessionManager.Instance.OnSessionJoined += OnSessionJoined;
        MP_SessionManager.Instance.OnSessionDisconnected += OnSessionDisconnected;
        MP_SessionManager.Instance.OnSessionError += OnSessionError;
    }

    private void OnDestroy()
    {
        // Unsubscribe from session events
        if (MP_SessionManager.Instance != null)
        {
            MP_SessionManager.Instance.OnSessionCreated -= OnSessionCreated;
            MP_SessionManager.Instance.OnSessionJoined -= OnSessionJoined;
            MP_SessionManager.Instance.OnSessionDisconnected -= OnSessionDisconnected;
            MP_SessionManager.Instance.OnSessionError -= OnSessionError;
        }
    }
    
    private void SetupButtonListeners()
    {
        // Main Menu
        if (m_CreateButton != null) m_CreateButton.onClick.AddListener(OnCreateButtonClicked);
        if (m_JoinButton != null) m_JoinButton.onClick.AddListener(OnJoinButtonClicked);
        if (m_QuickJoinButton != null) m_QuickJoinButton.onClick.AddListener(OnQuickJoinButtonClicked);
        
        // Lobby
        if (m_StartGameButton != null) m_StartGameButton.onClick.AddListener(OnStartGameButtonClicked);
        if (m_LeaveLobbyButton != null) m_LeaveLobbyButton.onClick.AddListener(OnLeaveLobbyButtonClicked);
        
        // Waiting Panel
        if (m_CancelButton != null) m_CancelButton.onClick.AddListener(OnCancelButtonClicked);
    }

    #region UI State Management
    private void ShowMainMenu()
    {
        if (m_MainMenuPanel != null) m_MainMenuPanel.SetActive(true);
        if (m_LobbyPanel != null) m_LobbyPanel.SetActive(false);
        if (m_WaitingPanel != null) m_WaitingPanel.SetActive(false);
    }

    private void ShowLobby()
    {
        if (m_MainMenuPanel != null) m_MainMenuPanel.SetActive(false);
        if (m_LobbyPanel != null) m_LobbyPanel.SetActive(true);
        if (m_WaitingPanel != null) m_WaitingPanel.SetActive(false);
        
        UpdateLobbyInfo();
    }

    private void ShowWaitingPanel(string p_Status = "Connecting...")
    {
        if (m_MainMenuPanel != null) m_MainMenuPanel.SetActive(false);
        if (m_LobbyPanel != null) m_LobbyPanel.SetActive(false);
        if (m_WaitingPanel != null) m_WaitingPanel.SetActive(true);
        
        if (m_StatusText != null) m_StatusText.text = p_Status;
    }    private void UpdateLobbyInfo()
    {
        var l_Session = MP_SessionManager.Instance.CurrentSession;
        if (l_Session == null) return;
        
        if (m_SessionCodeText != null) m_SessionCodeText.text = $"Session Code: {l_Session.Code}";
        if (m_PlayerCountText != null) m_PlayerCountText.text = $"Players: {l_Session.Players.Count}/{l_Session.MaxPlayers}";
        
        if (m_StartGameButton != null)
        {
            m_StartGameButton.gameObject.SetActive(MP_SessionManager.Instance.IsHost);
        }
        
        UpdatePlayerList(l_Session);
    }
    
    private void UpdatePlayerList(Unity.Services.Multiplayer.ISession p_Session)
    {
        if (m_PlayerListGO == null || m_PlayerRowPrefab == null)
        {
            Debug.LogWarning("Player list GameObject or player row prefab not assigned.");
            return;
        }
        
        // Clear any existing player rows
        foreach (Transform l_Child in m_PlayerListGO.transform)
        {
            Destroy(l_Child.gameObject);
        }
        
        // Create a row for each player in the session
        foreach (var l_Player in p_Session.Players)
        {
            GameObject l_PlayerRow = Instantiate(m_PlayerRowPrefab, m_PlayerListGO.transform);
            
            // Find the first TextMeshProUGUI component in the instantiated prefab
            TextMeshProUGUI l_PlayerNameText = l_PlayerRow.GetComponentInChildren<TextMeshProUGUI>();
            
            if (l_PlayerNameText != null)
            {
                // Get the player's name
                string l_PlayerName = "Player";
                
                // Try to get nickname from player properties (direct access)
                if (l_Player.Properties.TryGetValue("nickname", out var l_NicknameProperty) && 
                    l_NicknameProperty.Value != null && 
                    l_NicknameProperty.Value is string l_Nickname && 
                    !string.IsNullOrEmpty(l_Nickname))
                {
                    l_PlayerName = l_Nickname;
                }
                else
                {
                    // Use player ID (last 6 chars) if nickname is not available
                    l_PlayerName = $"Player {l_Player.Id.Substring(Math.Max(0, l_Player.Id.Length - 6))}";
                }
                
                // Indicate if this player is the host
                bool l_IsHost = p_Session is Unity.Services.Multiplayer.IHostSession && l_Player.Id == p_Session.CurrentPlayer.Id;
                if (l_IsHost)
                {
                    l_PlayerName += " (Host)";
                }
                
                // Set the player name in the text field
                l_PlayerNameText.text = l_PlayerName;
            }
            else
            {
                Debug.LogWarning("TextMeshProUGUI component not found in the player row prefab.");
            }
        }
    }
    #endregion

    #region Button Handlers
    private async void OnCreateButtonClicked()
    {
        ShowWaitingPanel("Creating session...");
        
        try
        {
            await MP_SessionManager.Instance.CreateSessionAsync(m_MaxPlayers);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create session: {e.Message}");
            ShowMainMenu();
        }
    }

    private async void OnJoinButtonClicked()
    {
        if (m_JoinCodeInput == null || string.IsNullOrEmpty(m_JoinCodeInput.text))
        {
            Debug.LogError("Please enter a valid join code");
            return;
        }
        
        ShowWaitingPanel("Joining session...");
        m_IsJoining = true;
        
        try
        {
            bool l_Success = await MP_SessionManager.Instance.JoinSessionByCodeAsync(m_JoinCodeInput.text);
            
            if (!l_Success && !m_IsJoining)
            {
                ShowMainMenu();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join session: {e.Message}");
            ShowMainMenu();
        }
    }

    private async void OnQuickJoinButtonClicked()
    {
        ShowWaitingPanel("Finding a session...");
        m_IsJoining = true;
        
        try
        {
            await MP_SessionManager.Instance.QuickJoinAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"Quick join failed: {e.Message}");
            
            if (m_IsJoining)
            {
                ShowMainMenu();
            }
        }
    }

    private async void OnStartGameButtonClicked()
    {
        // Here you would implement starting the game for all players
        Debug.Log("Starting game...");
        
        // Example: Set a session property to signal game start
        await MP_SessionManager.Instance.SetSessionPropertyAsync("gameStarted", new Unity.Services.Multiplayer.SessionProperty("true"));
    }

    private async void OnLeaveLobbyButtonClicked()
    {
        ShowWaitingPanel("Leaving session...");
        
        try
        {
            await MP_SessionManager.Instance.LeaveSessionAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to leave session: {e.Message}");
            ShowMainMenu();
        }
    }

    private async void OnCancelButtonClicked()
    {
        m_IsJoining = false;
        
        MP_SessionManager.Instance.CancelMatchmaking();
        
        if (MP_SessionManager.Instance.IsConnected)
        {
            await MP_SessionManager.Instance.LeaveSessionAsync();
        }
        else
        {
            ShowMainMenu();
        }
    }
    #endregion

    #region Session Event Callbacks
    private void OnSessionCreated(Unity.Services.Multiplayer.ISession p_Session)
    {
        Debug.Log($"Session created with code: {p_Session.Code}");
        ShowLobby();
    }

    private void OnSessionJoined(Unity.Services.Multiplayer.ISession p_Session)
    {
        m_IsJoining = false;
        Debug.Log($"Joined session with code: {p_Session.Code}");
        ShowLobby();
    }

    private void OnSessionDisconnected()
    {
        Debug.Log("Disconnected from session");
        ShowMainMenu();
    }

    private void OnSessionError(Exception p_Exception)
    {
        Debug.LogError($"Session error: {p_Exception.Message}");
        
        // If we were in the process of joining, return to main menu
        if (m_IsJoining)
        {
            m_IsJoining = false;
            ShowMainMenu();
        }
    }
    #endregion
}
