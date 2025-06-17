using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class MP_MMS_GameLoop : NetworkBehaviour
{
    public static MP_MMS_GameLoop Instance { get; private set; }

    [Header("Score Settings")]
    [SerializeField] private MinMax m_CustomerPay = new MinMax(100, 300);
    [SerializeField] private float m_InicialGoalScore = 1000f;
    [SerializeField] private float m_PerDayGoalMultiplier = 1.1f;

    [Header("Day Settings")]
    [SerializeField] private float m_DayTimeSpan = 180f;
    [SerializeField] private float m_IntervalBetweenDays = 5f;

    [Header("Tables Settings")]
    [SerializeField] private List<MP2D_TableManager> m_Tables;
    [SerializeField] private MinMax m_TableRespawnInterval = new MinMax(1f, 10f);
    [SerializeField] private MinMax m_TableLeavingHangingInterval = new MinMax(120f, 300f);

    [Header("Score Events")]
    [SerializeField] private UnityEvent<float> m_OnGoalChange;
    [SerializeField] private UnityEvent<string> m_OnGoalChangeString;
    [SerializeField] private UnityEvent<float> m_OnSumChange;
    [SerializeField] private UnityEvent<string> m_OnSumChangeString;

    [Header("Day Events")]
    [SerializeField] private UnityEvent m_OnStartOfTheDay;
    [SerializeField] private UnityEvent m_OnEndOfTheDay;

    private NetworkVariable<float> m_GoalScore = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> m_SumScore = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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

    public override void OnNetworkSpawn()
    {
        m_GoalScore.OnValueChanged += OnGoalChange;
        m_SumScore.OnValueChanged += OnSumChange;

        if (!IsServer) return;
        //StartGame();
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnGoalChange(float p_OldValue, float p_NewValue)
    {
        m_OnGoalChange?.Invoke(p_NewValue);
        m_OnGoalChangeString?.Invoke(Mathf.Floor(p_NewValue).ToString());
    }

    private void OnSumChange(float p_OldValue, float p_NewValue)
    {
        m_OnSumChange?.Invoke(p_NewValue);
        m_OnSumChangeString?.Invoke(Mathf.Floor(p_NewValue).ToString());
    }

    public void StartGame()
    {
        if (!IsServer) return;

        m_GoalScore.Value = m_InicialGoalScore;
        StartDay();
    }

    private void StartTables()
    {
        m_Tables.ForEach(p_Table =>
        {
            p_Table.SpawnInterval = Random.Range(m_TableRespawnInterval.Min, m_TableRespawnInterval.Max);
            p_Table.LeaveHangingInterval = Random.Range(m_TableLeavingHangingInterval.Min, m_TableLeavingHangingInterval.Max);
            p_Table.SpawnNewClientsServerRpc();
        });
    }

    private void StopTables()
    {
        m_Tables.ForEach(p_Table =>
        {
            p_Table.StopClients();
        });
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddScoreServerRpc()
    {
        m_SumScore.Value += Random.Range(m_CustomerPay.Min, m_CustomerPay.Max);
    }


    private void StartDay()
    {
        StartTables();
        StartCoroutine(FinishDay());

        OnDayStartEventClientRpc();
    }

    private void StopDay()
    {
        if (m_SumScore.Value < m_GoalScore.Value)
            Application.Quit();

        StopTables();
        StartCoroutine(NextDay());

        OnDayEndEventClientRpc();
        m_SumScore.Value -= m_GoalScore.Value;
        m_GoalScore.Value += m_InicialGoalScore * m_PerDayGoalMultiplier;
    }

    private IEnumerator FinishDay()
    {
        yield return new WaitForSeconds(m_DayTimeSpan);

        StopDay();
    }

    private IEnumerator NextDay()
    {
        yield return new WaitForSeconds(m_IntervalBetweenDays);

        StartDay();
    }

    [ClientRpc]
    private void OnDayStartEventClientRpc()
    {
        m_OnStartOfTheDay?.Invoke();
    }

    [ClientRpc]
    private void OnDayEndEventClientRpc()
    {
        m_OnEndOfTheDay?.Invoke();
    }
}
