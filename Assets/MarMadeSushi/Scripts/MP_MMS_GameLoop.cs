using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class MP_MMS_GameLoop : NetworkBehaviour
{
    public static MP_MMS_GameLoop Instance { get; private set; }

    [Header("Score Settings")]
    [SerializeField][Range(0f, 5f)] private float m_InicialRating = 0f;
    [SerializeField] private MinMax m_HappyCustomerRatings = new MinMax(3, 5);
    [SerializeField] private MinMax m_AngryCustomerRatings = new MinMax(-2, -1);

    [Header("Tables Settings")]
    [SerializeField] private List<MP2D_TableManager> m_Tables;
    [SerializeField] private MinMax m_TableRespawnInterval = new MinMax(1f, 10f);
    [SerializeField] private MinMax m_TableLeavingHangingInterval = new MinMax(120f, 300f);

    [Header("Score Events")]
    [SerializeField] private UnityEvent<float> m_OnRatingChange;
    [SerializeField] private UnityEvent<string> m_OnRatingChangeString;

    private NetworkVariable<int> m_TotalRating = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> m_SumRating = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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
        m_TotalRating.OnValueChanged += OnTotalRatingChange;
        m_SumRating.OnValueChanged += OnSumRatingChange;

        if (!IsServer) return;

        m_SumRating.Value += m_InicialRating;
        //StartGame();
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnTotalRatingChange(int p_OldValue, int p_NewValue)
    {
        m_OnRatingChange?.Invoke(m_SumRating.Value / p_NewValue);
        m_OnRatingChangeString?.Invoke((Mathf.Floor(m_SumRating.Value / p_NewValue * 100) / 100).ToString());
    }

    private void OnSumRatingChange(float p_OldValue, float p_NewValue)
    {
        if (!IsServer) return;
        m_TotalRating.Value++;
    }

    public void StartGame()
    {
        StartTables();
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

    [ServerRpc(RequireOwnership = false)]
    public void GiveGoodRatingServerRpc()
    {
        m_SumRating.Value += Random.Range(m_HappyCustomerRatings.Min, m_HappyCustomerRatings.Max);
    }

    [ServerRpc(RequireOwnership = false)]
    public void GiveBadRatingServerRpc()
    {
        m_SumRating.Value += Random.Range(m_AngryCustomerRatings.Min, m_AngryCustomerRatings.Max);
    }
}
