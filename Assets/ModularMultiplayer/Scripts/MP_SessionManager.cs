using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class MP_SessionManager : MonoBehaviour
{
    #region Singleton
    public static MP_SessionManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion    // Session properties


    private ISession m_CurrentSession;
    private IHostSession m_HostSession;
    private CancellationTokenSource m_MatchmakerCancellationSource;
    
    public ISession CurrentSession => m_CurrentSession;
    public bool IsHost => m_HostSession != null;
    public bool IsConnected => m_CurrentSession != null;
    public string SessionCode => m_CurrentSession?.Code;
    public string SessionId => m_CurrentSession?.Id;
    public string PlayerId => AuthenticationService.Instance?.PlayerId;

    public event Action<ISession> OnSessionCreated;
    public event Action<ISession> OnSessionJoined;
    public event Action OnSessionDisconnected;
    public event Action<Exception> OnSessionError;    
    
    private async void Start()
    {
        await InitializeUnityServices();
    }

    private async Task InitializeUnityServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"Sign in anonymously succeeded! PlayerID: {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            OnSessionError?.Invoke(e);
        }
    }
    
    #region Session Management
    public async Task<ISession> CreateSessionAsync(int p_MaxPlayers = 4, Dictionary<string, SessionProperty> p_InitialProperties = null)
    {
        try
        {
            var l_Options = new SessionOptions
            {
                MaxPlayers = p_MaxPlayers
            }.WithRelayNetwork();

            if (p_InitialProperties != null)
            {
                //l_Options.Properties = p_InitialProperties;
            }

            m_CurrentSession = await MultiplayerService.Instance.CreateSessionAsync(l_Options);
            m_HostSession = m_CurrentSession as IHostSession;
            
            Debug.Log($"Session {m_CurrentSession.Id} created! Join code: {m_CurrentSession.Code}");
            OnSessionCreated?.Invoke(m_CurrentSession);
            
            return m_CurrentSession;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            OnSessionError?.Invoke(e);
            return null;
        }
    }    
    
    public async Task<bool> JoinSessionByCodeAsync(string p_JoinCode)
    {
        try
        {
            m_CurrentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(p_JoinCode);
            Debug.Log($"Joined session {m_CurrentSession.Id} with code: {p_JoinCode}");
            OnSessionJoined?.Invoke(m_CurrentSession);
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            OnSessionError?.Invoke(e);
            return false;
        }
    }    
    
    public async Task<ISession> QuickJoinAsync(int p_MinAvailableSlots = 1, TimeSpan? p_Timeout = null)
    {
        try
        {
            var l_QuickJoinOptions = new QuickJoinOptions()
            {
                Filters = new List<FilterOption>
                {
                    new(FilterField.AvailableSlots, p_MinAvailableSlots.ToString(), FilterOperation.GreaterOrEqual)
                },
                Timeout = p_Timeout ?? TimeSpan.FromSeconds(10),
                CreateSession = true
            };

            var l_SessionOptions = new SessionOptions()
            {
                MaxPlayers = 4
            }.WithRelayNetwork();

            m_CurrentSession = await MultiplayerService.Instance.MatchmakeSessionAsync(l_QuickJoinOptions, l_SessionOptions);
            
            if (m_CurrentSession != null && m_CurrentSession is IHostSession l_HostSession)
            {
                m_HostSession = l_HostSession;
            }
            
            Debug.Log($"Quick join successful! Session ID: {m_CurrentSession?.Id}, Join code: {m_CurrentSession?.Code}");
            OnSessionJoined?.Invoke(m_CurrentSession);
            
            return m_CurrentSession;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            OnSessionError?.Invoke(e);
            return null;
        }
    }    
    
    public async Task<ISession> JoinQueueAsync(string p_QueueName, int p_MaxPlayers = 2, CancellationToken? p_CancellationToken = null)
    {
        try
        {
            m_MatchmakerCancellationSource = new CancellationTokenSource();
            
            var l_MatchmakerOptions = new MatchmakerOptions
            {
                QueueName = p_QueueName
            };

            var l_SessionOptions = new SessionOptions()
            {
                MaxPlayers = p_MaxPlayers
            }.WithRelayNetwork();

            m_CurrentSession = await MultiplayerService.Instance.MatchmakeSessionAsync(
                l_MatchmakerOptions, 
                l_SessionOptions, 
                p_CancellationToken ?? m_MatchmakerCancellationSource.Token
            );
            
            if (m_CurrentSession != null && m_CurrentSession is IHostSession l_HostSession)
            {
                m_HostSession = l_HostSession;
            }
            
            Debug.Log($"Queue matchmaking successful! Session ID: {m_CurrentSession?.Id}, Join code: {m_CurrentSession?.Code}");
            OnSessionJoined?.Invoke(m_CurrentSession);
            
            return m_CurrentSession;
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Matchmaking was cancelled");
            return null;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            OnSessionError?.Invoke(e);
            return null;
        }
    }    
    
    public void CancelMatchmaking()
    {
        m_MatchmakerCancellationSource?.Cancel();
        m_MatchmakerCancellationSource = null;
    }

    public async Task LeaveSessionAsync()
    {
        if (m_CurrentSession == null)
        {
            return;
        }

        try
        {
            if (m_CurrentSession != null)
            {
                await m_CurrentSession.LeaveAsync();
                Debug.Log($"Left session {m_CurrentSession.Id}");
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            OnSessionError?.Invoke(e);
        }
        finally
        {
            m_CurrentSession = null;
            m_HostSession = null;
            OnSessionDisconnected?.Invoke();
        }
    }


    #endregion    
    
    #region Session Properties
    public async Task<bool> SetSessionPropertyAsync(string p_Key, SessionProperty p_Property)
    {
        if (m_HostSession == null)
        {
            Debug.LogError("Cannot set session property: not the host");
            return false;
        }

        try
        {
            m_HostSession.SetProperty(p_Key, p_Property);
            await m_HostSession.SavePropertiesAsync();
            Debug.Log($"Session property {p_Key} set successfully");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            OnSessionError?.Invoke(e);
            return false;
        }
    }

    public async Task<bool> SetSessionPropertiesAsync(Dictionary<string, SessionProperty> p_Properties)
    {
        if (m_HostSession == null)
        {
            Debug.LogError("Cannot set session properties: not the host");
            return false;
        }

        try
        {
            m_HostSession.SetProperties(p_Properties);
            await m_HostSession.SavePropertiesAsync();
            Debug.Log("Session properties set successfully");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            OnSessionError?.Invoke(e);
            return false;
        }
    }

    public bool TryGetSessionProperty<T>(string p_Key, out T p_Value)
    {
        p_Value = default;
        
        if (m_CurrentSession == null || !m_CurrentSession.Properties.TryGetValue(p_Key, out var l_Property))
        {
            return false;
        }

        try
        {
            p_Value = (T)Convert.ChangeType(l_Property.Value, typeof(T));
            return true;
        }
        catch
        {
            Debug.LogError($"Failed to cast session property {p_Key} to type {typeof(T).Name}");
            return false;
        }
    }
    #endregion    
    
    #region Player Properties
    public async Task<bool> SetPlayerPropertyAsync(string p_Key, PlayerProperty p_Property)
    {
        if (m_CurrentSession == null)
        {
            Debug.LogError("Cannot set player property: no active session");
            return false;
        }

        try
        {
            m_CurrentSession.CurrentPlayer.SetProperty(p_Key, p_Property);
            await m_CurrentSession.SaveCurrentPlayerDataAsync();
            Debug.Log($"Player property {p_Key} set successfully");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            OnSessionError?.Invoke(e);
            return false;
        }
    }

    public async Task<bool> SetPlayerPropertiesAsync(Dictionary<string, PlayerProperty> p_Properties)
    {
        if (m_CurrentSession == null)
        {
            Debug.LogError("Cannot set player properties: no active session");
            return false;
        }

        try
        {
            m_CurrentSession.CurrentPlayer.SetProperties(p_Properties);
            await m_CurrentSession.SaveCurrentPlayerDataAsync();
            Debug.Log("Player properties set successfully");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            OnSessionError?.Invoke(e);
            return false;
        }
    }

    public bool TryGetPlayerProperty<T>(IPlayer p_Player, string p_Key, out T p_Value)
    {
        p_Value = default;
        
        if (p_Player == null || !p_Player.Properties.TryGetValue(p_Key, out var l_Property))
        {
            return false;
        }

        try
        {
            p_Value = (T)Convert.ChangeType(l_Property.Value, typeof(T));
            return true;
        }
        catch
        {
            Debug.LogError($"Failed to cast player property {p_Key} to type {typeof(T).Name}");
            return false;
        }
    }
    #endregion    
    
    private void OnDestroy()
    {
        // Clean up any active session when the manager is destroyed
        if (m_CurrentSession != null)
        {
            m_CurrentSession.LeaveAsync().ContinueWith(task =>
            {
                if (task.Exception != null)
                {
                    Debug.LogException(task.Exception);
                }
            });
        }
        
        m_MatchmakerCancellationSource?.Cancel();
        m_MatchmakerCancellationSource = null;
    }
}
