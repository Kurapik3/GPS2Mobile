using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    [Header("Turn Settings")]
    [Tooltip("Maximum number of turns before game ends")]
    [SerializeField] private int maxTurns = 30;

    [Tooltip("Reference to End Turn button in the UI")]
    [SerializeField] private Button endTurnButton;

    private int currentTurn = 1;
    private bool isPlayerTurn = true;

    public int CurrentTurn => currentTurn;
    public bool IsPlayerTurn => isPlayerTurn;

    public delegate void TurnEvent();
    public static event TurnEvent OnPlayerTurnStart;
    public static event TurnEvent OnEnemyTurnStart;

    [Header("AI")]
    [SerializeField] private AIController aiController;

    private void Start()
    {
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(EndTurn);
        }

        if (aiController != null)
        {
            aiController.OnAITurnFinished += EndEnemyTurn;
        }

        StartPlayerTurn();
    }

    private void StartPlayerTurn()
    {
        isPlayerTurn = true;
        Debug.Log($"--- Player Turn {currentTurn} ---");

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

        aiController?.ExecuteTurn();

        // to simulate a short delay for enemy actions
        //Invoke(nameof(EndEnemyTurn), 2f);
    }

    private void EndEnemyTurn()
    {
        currentTurn++;
        if (currentTurn > maxTurns)
        {
            EndGame();
        }
        else
        {
            StartPlayerTurn();
        }
    }

    public void EndTurn()
    {
        if (!isPlayerTurn)
        {
            return; // prevent double-clicks or AI triggers
        }
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
    }
}
