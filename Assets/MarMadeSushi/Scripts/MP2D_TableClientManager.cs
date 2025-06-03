using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MP2D_TableClientManager : NetworkBehaviour
{
    private enum TableClientState
    {
        None,
        Choosing,
        ReadyToAsk,
        Waiting,
        Happy,
        Mad,
        Eating,
        ReadyToCheck,
    }

    [Serializable]
    private struct TableClientStateEmote
    {
        public TableClientState EmoteClientState;
        public Sprite EmoteSprite;
    }

    [Serializable]
    private struct TableClientFoodChoice
    {
        public GameObject FoodGameObject;
        public Sprite FoodSprite;
    }

    [Header("Components Settings")]
    [SerializeField] private SpriteRenderer m_TableClientSprite;
    [SerializeField] private Animator m_TableClientAnimator;
    [SerializeField] private SpriteRenderer m_TableClientEmoteSprite;
    [SerializeField] private MP2D_PlayerItemCollector m_ClientItemCollector;

    [Header("Client Settings")]
    [SerializeField] private List<Sprite> m_ClientSprites;
    [Tooltip("Interval in seconds between spawning clients.")][SerializeField] private float m_IntervalBetweenClients = 10f;
    [SerializeField] private List<TableClientStateEmote> m_TableClientEmotes;

    [Header("Food Settings")]
    [SerializeField] private List<TableClientFoodChoice> m_FoodChoices;
    [SerializeField] private float m_ChooseFoodDelay = 5f;
    [SerializeField] private float m_TimeToEatFood = 10f;

    private NetworkVariable<TableClientState> m_CurrentState = new NetworkVariable<TableClientState>(TableClientState.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> m_CurrentClientSprite = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> m_CurrentFood = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        m_CurrentState.OnValueChanged += OnTableClientStateChange;
        m_CurrentClientSprite.OnValueChanged += OnTableClientSpriteChange;
        m_CurrentFood.OnValueChanged += OnTableClientFoodChange;
        m_ClientItemCollector.OnCollectCorrectItem.AddListener(CorrectFoodDelivered);
        m_ClientItemCollector.OnCollectWrongItem.AddListener(WrongFoodDelivered);

        if (!IsServer) return;

        StartCoroutine(SpawnTableClient());
    }

    private void OnTableClientStateChange(TableClientState p_OldValue, TableClientState p_NewValue)
    {
        if (p_NewValue == TableClientState.None)
        {
            m_TableClientSprite.enabled = false;
            m_TableClientEmoteSprite.enabled = false;
            return;
        }

        Sprite l_EmoteSprite = m_TableClientEmotes.Find(emote => emote.EmoteClientState == p_NewValue).EmoteSprite;
        if (l_EmoteSprite == null)
        {
            m_TableClientEmoteSprite.enabled = false;
            return;
        }

        m_TableClientEmoteSprite.sprite = l_EmoteSprite;
        m_TableClientEmoteSprite.enabled = true;
    }

    private void OnTableClientSpriteChange(int p_OldValue, int p_NewValue)
    {
        if (p_NewValue == -1)
        {
            m_TableClientSprite.enabled = false;
            return;
        }

        m_TableClientSprite.sprite = m_ClientSprites[p_NewValue];
        m_TableClientSprite.enabled = true;
    }

    private void OnTableClientFoodChange(int p_OldValue, int p_NewValue)
    {
        if (p_NewValue == -1)
        {
            m_ClientItemCollector.ItemToCollect = null;
            return;
        }

        m_ClientItemCollector.ItemToCollect = m_FoodChoices[p_NewValue].FoodGameObject;
        m_TableClientEmoteSprite.sprite = m_FoodChoices[p_NewValue].FoodSprite;
        m_TableClientEmoteSprite.enabled = true;
    }

    private IEnumerator SpawnTableClient()
    {
        yield return new WaitForSeconds(m_IntervalBetweenClients);

        m_CurrentClientSprite.Value = UnityEngine.Random.Range(0, m_ClientSprites.Count);
        m_CurrentState.Value = TableClientState.Choosing;

        yield return new WaitForSeconds(m_ChooseFoodDelay);

        m_CurrentState.Value = TableClientState.ReadyToAsk;
    }

    public void OnInteract()
    {
        switch (m_CurrentState.Value)
        {
            case TableClientState.ReadyToAsk:
                SetTableClientFoodServerRpc(UnityEngine.Random.Range(0, m_FoodChoices.Count));
                break;

            case TableClientState.Waiting:
                m_ClientItemCollector.CollectItem();
                break;

            default:
                break;
        }
    }

    private IEnumerator DelayTableClientState(float p_Delay, TableClientState p_State)
    {
        yield return new WaitForSeconds(p_Delay);

        m_CurrentState.Value = p_State;
    }


    public void CorrectFoodDelivered()
    {
        SetTableClientFoodServerRpc(-1);
        SetTableClientStateServerRpc(TableClientState.Happy);
        SetTableClientStateWithDelayServerRpc(3f, TableClientState.Eating);
        SetTableClientStateWithDelayServerRpc(3f + m_TimeToEatFood, TableClientState.None);
    }

    public void WrongFoodDelivered()
    {
        SetTableClientStateServerRpc(TableClientState.Mad);
        SetTableClientStateWithDelayServerRpc(3f, TableClientState.Waiting);
    }



    [ServerRpc(RequireOwnership = false)]
    private void SetTableClientFoodServerRpc(int p_NewFoodNumber)
    {
        m_CurrentFood.Value = p_NewFoodNumber;

        StartCoroutine(DelayTableClientState(3f, TableClientState.Waiting));
    }


    [ServerRpc(RequireOwnership = false)]
    private void SetTableClientStateServerRpc(TableClientState p_ClientState)
    {
        m_CurrentState.Value = p_ClientState;
    }


    [ServerRpc(RequireOwnership = false)]
    private void SetTableClientStateWithDelayServerRpc(float p_DelayInSeconds, TableClientState p_ClientState)
    {
        StartCoroutine(DelayTableClientState(p_DelayInSeconds, p_ClientState));
    }
                         
}
