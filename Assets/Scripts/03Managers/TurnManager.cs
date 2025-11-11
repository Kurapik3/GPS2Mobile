using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    [Header("Turn Settings")]
    [Tooltip("Maximum number of turns before game ends")]
    [SerializeField] private int maxTurns = 30;

    [Tooltip("Reference to End Turn button in the UI")]
    [SerializeField] private Button endTurnButton;

    private List<BuildingBase> allBuildings = new List<BuildingBase>();

    private bool isProcessingTurn = false;
    private int currentTurn = 0;
    private bool isPlayerTurn = true;

    public int CurrentTurn => currentTurn;
    public bool IsPlayerTurn => isPlayerTurn;

    public delegate void TurnEvent();
    public static event TurnEvent OnPlayerTurnStart;
    public static event TurnEvent OnEnemyTurnStart;

     public TreeBase treeBase;

    private void OnEnable()
    {
        EventBus.Subscribe<EnemyAIEvents.EnemyTurnEndEvent>(OnEnemyTurnEnd);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EnemyAIEvents.EnemyTurnEndEvent>(OnEnemyTurnEnd);
    }

    private void Start()
    {

        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(EndTurn);
        }

        EventBus.Publish(new TurnUpdatedEvent(0, maxTurns));
        StartPlayerTurn();
        
    }

    private void StartPlayerTurn()
    {

        isPlayerTurn = true;
        Debug.Log($"--- Player Turn {currentTurn} ---");

        foreach (var unit in UnitManager.Instance.GetAllUnits())
        {
            unit.ResetMove();
        }


        EventBus.Publish(new TurnUpdatedEvent(currentTurn, maxTurns));
        treeBase.OnTurnStart();

        OnPlayerTurnStart?.Invoke();

        if (endTurnButton != null)
        {
            endTurnButton.interactable = true;
        }
    }

    private void StartEnemyTurn()
    {
        isPlayerTurn = false;
        Debug.Log("--- Enemy Turn ---");

        OnEnemyTurnStart?.Invoke();

        if (endTurnButton != null)
        {
            endTurnButton.interactable = false;
        }

        EventBus.Publish(new EnemyAIEvents.EnemyTurnStartEvent(currentTurn));

        // to simulate a short delay for enemy actions
        //Invoke(nameof(EndEnemyTurn), 2f);
    }

    private void OnEnemyTurnEnd(EnemyAIEvents.EnemyTurnEndEvent evt)
    {
        Debug.Log($"--- Enemy Turn {evt.Turn} End ---");

        EnemyUnitManager.Instance.ClearJustSpawnedUnits();
        currentTurn++;
        isProcessingTurn = false;
        if (currentTurn > maxTurns)
        {
            EndGame();
            return;
        }

        foreach (var building in allBuildings) // gain AP
            building.OnTurnStart();

        GameManager.Instance.CheckEnding();

        StartPlayerTurn();
    }

    public void EndTurn()
    {
        if (!isPlayerTurn || isProcessingTurn)
        {
            return; // prevent double-clicks or AI triggers
        }
        isProcessingTurn = true;
        Debug.Log("Player ended turn.");
        if (endTurnButton != null)
        {
            endTurnButton.interactable = false;
        }
        StartEnemyTurn();
    }

    private void EndGame()
    {
        Debug.Log("Game Over! Max turns reached.");
        // Here you can add code to calculate score, show summary UI, etc.
        GameManager.Instance.CheckEnding();
    }
}
