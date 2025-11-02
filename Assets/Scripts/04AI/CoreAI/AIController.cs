using System.Collections;
using UnityEngine;
using static EnemyAIEvents;

/// <summary>
/// Coordinates AI turn phases by publishing phase events.
/// Not a singleton; subscribes to EventBus for turn start.
/// </summary>
public class AIController : MonoBehaviour
{
    [SerializeField] private float phaseDelay = 0.5f;
    [SerializeField] private float aiSpeedMultiplier = 2.5f;
    public static float AISpeedMultiplier { get; private set; }

    private int currentTurn = 0;

    private void Awake()
    {
        AISpeedMultiplier = aiSpeedMultiplier;
    }

    private void OnEnable()
    {
        EventBus.Subscribe<EnemyTurnStartEvent>(OnEnemyTurnStart);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EnemyTurnStartEvent>(OnEnemyTurnStart);
    }

    private void OnEnemyTurnStart(EnemyTurnStartEvent evt)
    {
        currentTurn = evt.Turn;
        foreach (var id in EnemyUnitManager.Instance.GetOwnedUnitIds())
        {
            EnemyUnitManager.Instance.UnlockState(id);
        }
        StartCoroutine(RunAITurn());
    }

    private IEnumerator RunAITurn()
    {
        //Enemy base phase
        EventBus.Publish(new ExecuteBasePhaseEvent(currentTurn));
        yield return WaitForPhaseEnd<BasePhaseEndEvent>();

        ////Builder phase (move towards grove/build base on top of grove)
        //EventBus.Publish(new ExecuteBuilderPhaseEvent(currentTurn));
        //yield return WaitForPhaseEnd<BuilderPhaseEndEvent>();

        //Dormant phase (dormant units move)
        EventBus.Publish(new ExecuteDormantPhaseEvent(currentTurn));
        yield return WaitForPhaseEnd<DormantPhaseEndEvent>();

        //Aggressive phase (aggressive units action)
        EventBus.Publish(new ExecuteAggressivePhaseEvent(currentTurn));
        yield return WaitForPhaseEnd<AggressivePhaseEndEvent>();

        //End turn
        EventBus.Publish(new EnemyTurnEndEvent(currentTurn));
        Debug.Log($"<color=yellow>=== Enemy Turn {currentTurn} Finished ===</color>");
    }

    private IEnumerator WaitForPhaseEnd<T>() where T : struct
    {
        bool phaseEnded = false;
        void OnPhaseEnd(T evt) => phaseEnded = true;

        EventBus.Subscribe<T>(OnPhaseEnd);
        yield return new WaitUntil(() => phaseEnded);
        EventBus.Unsubscribe<T>(OnPhaseEnd);
    }
}


