using System;
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


    public int CurrentTurn
    {
        get => currentTurn;
        set => currentTurn = value;
    }
    
    public bool IsPlayerTurn => isPlayerTurn;

    public delegate void TurnEvent();
    public static event TurnEvent OnPlayerTurnStart;
    public static event TurnEvent OnEnemyTurnStart;
    public static event TurnEvent OnSeaMonsterTurnStart;

     public TreeBase treeBase;

    [SerializeField] private TribeStatsUI tribeStats;

    private void OnEnable()
    {
        EventBus.Subscribe<EnemyAIEvents.EnemyTurnEndEvent>(OnEnemyTurnEnd);
        EventBus.Subscribe<SeaMonsterEvents.SeaMonsterTurnEndEvent>(OnSeaMonsterTurnEnd);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EnemyAIEvents.EnemyTurnEndEvent>(OnEnemyTurnEnd);
        EventBus.Unsubscribe<SeaMonsterEvents.SeaMonsterTurnEndEvent>(OnSeaMonsterTurnEnd);
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

        foreach (var unit in UnitManager.Instance?.GetAllUnits() ?? new List<UnitBase>())
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

    private void StartSeaMonsterTurn()
    {
        isPlayerTurn = false;
        Debug.Log("--- Sea Monster Turn ---");

        OnSeaMonsterTurnStart?.Invoke();
        
        if (endTurnButton != null)
        {
            endTurnButton.interactable = false;
        }

        EventBus.Publish(new SeaMonsterEvents.SeaMonsterTurnStartedEvent(currentTurn));

        //No AI, spawn will still run above
        if (currentTurn < 9)
        {
            Debug.Log("[TurnManager] Before turn 9: Only spawn check, no AI.");
            SkipSeaMonsterTurn();
            return; // Wait for SeaMonsterTurnEndEvent
        }

        //Check if any untamed sea monster exists
        bool hasUntamed = false;

        foreach (var monster in SeaMonsterManager.Instance.ActiveMonsters)
        {
            if (monster.State == SeaMonsterState.Untamed)
            {
                hasUntamed = true;
                break;
            }
        }

        if (hasUntamed)
        {
            Debug.Log("[TurnManager] Untamed monsters found. Running AI.");
            return;
        }

        Debug.Log("[TurnManager] All sea monsters are tamed. Skipping.");
        SkipSeaMonsterTurn();
}

    private void OnEnemyTurnEnd(EnemyAIEvents.EnemyTurnEndEvent evt)
    {
        Debug.Log($"--- Enemy Turn {evt.Turn} End ---");

        EnemyUnitManager.Instance.ClearJustSpawnedUnits();

        if(currentTurn >= 9)
        {
            StartSeaMonsterTurn();
            return;
        }

        SkipSeaMonsterTurn();
    }

    private void SkipSeaMonsterTurn()
    {
        Debug.Log("[TurnManager] Skipping Sea Monster Turn.");

        currentTurn++;
        isProcessingTurn = false;

        if (currentTurn > maxTurns)
        {
            EndGame();
            GameManager.Instance?.CheckEnding();
            return;
        }

        // buildings gain AP
        foreach (var building in allBuildings)
            building.OnTurnStart();

        GameManager.Instance.CheckEnding();
        EventBus.Publish(new ActionMadeEvent());

        StartPlayerTurn();
    }

    private void OnSeaMonsterTurnEnd(SeaMonsterEvents.SeaMonsterTurnEndEvent evt)
    {
        Debug.Log($"--- Sea Monster Turn {evt.Turn} End ---");

        currentTurn++;
        Debug.Log($"[TurnManager] OnSeaMonsterTurnEnd - CurrentTurn incremented to: {currentTurn}");

        isProcessingTurn = false;
        if (currentTurn > maxTurns)
        {
            Debug.Log("[TurnManager] Max turns reached! Calling EndGame or CheckEnding.");
            EndGame();
            GameManager.Instance?.CheckEnding();
            return;
        }

        foreach (var building in allBuildings) // gain AP
            building.OnTurnStart();

        GameManager.Instance.CheckEnding();
        EventBus.Publish(new ActionMadeEvent());
        Debug.Log($"[TurnManager] Called GameManager.Instance?.CheckEnding();");

        StartPlayerTurn();
    }

    //private void OnEnemyTurnEnd(EnemyAIEvents.EnemyTurnEndEvent evt)
    //{
    //    Debug.Log($"[TurnManager] OnEnemyTurnEnd - Enemy Turn {evt.Turn} Ended. CurrentTurn before increment: {currentTurn}");

    //    EnemyUnitManager.Instance.ClearJustSpawnedUnits();
    //    currentTurn++;
    //    Debug.Log($"[TurnManager] OnEnemyTurnEnd - CurrentTurn incremented to: {currentTurn}");

    //    isProcessingTurn = false;

    //    foreach (var building in allBuildings) // gain AP
    //        building.OnTurnStart();

    //    // --- NEW LOGIC: Check for End Game Condition ---
    //    if (currentTurn >= maxTurns)
    //    {
    //        Debug.Log("[TurnManager] Max turns reached! Game Over.");
    //        EndGame(); // Call the EndGame method to show the screen
    //    }
    //    else
    //    {
    //        // If game didn't end, start the next player turn
    //        StartPlayerTurn();
    //    }
    //}

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

    //private void EndGame()
    //{
    //    Debug.Log("Game Over! Max turns reached.");
    //    // Here you can add code to calculate score, show summary UI, etc.
    //    GameManager.Instance.CheckEnding();
    //}

    private void EndGame()
    {
        int playerScore = PlayerTracker.Instance?.getScore() ?? 0;
        int enemyScore = EnemyTracker.Instance?.GetScore() ?? 0;

        Debug.Log($"[TurnManager] EndGame called. Player Score: {playerScore}, Enemy Score: {enemyScore}");

        bool isPlayerVictory = playerScore > enemyScore;

        // Show the victory/defeat screen directly
        if (tribeStats != null)
        {
            tribeStats.ShowAsEndGameResult(isPlayerVictory);
        }
        else
        {
            Debug.LogError("[TurnManager] scoreBoardPanel reference is null! Cannot show end game screen.");
        }

        // Optional: Disable other UI elements like End Turn button
        if (endTurnButton != null)
            endTurnButton.interactable = false;
        
    }

}
