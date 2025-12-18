using System;
using System.Collections;
using UnityEngine;
using static EnemyAIEvents;

public class AIController : MonoBehaviour
{
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
        EnemyUnitManager.Instance.ClearActedUnits();
        foreach (var id in EnemyUnitManager.Instance.GetOwnedUnitIds())
        {
            EnemyUnitManager.Instance.UnlockState(id);
        }

        StartCoroutine(RunAITurn());
    }

    private IEnumerator RunAITurn()
    {
        Debug.Log($"<color=orange>=== Enemy Turn {currentTurn} Started ===</color>");

        //Enemy base phase
        bool baseDone = false;
        Action onBaseComplete = () => baseDone = true;
        EventBus.Publish(new ExecuteBasePhaseEvent(currentTurn, onBaseComplete));
        yield return new WaitUntil(() => baseDone);

        //Builder phase(move towards grove/ build base on top of grove)
        bool builderDone = false;
        Action onBuilderComplete = () => builderDone = true;
        EventBus.Publish(new ExecuteBuilderPhaseEvent(currentTurn, onBuilderComplete));
        yield return new WaitUntil(() => builderDone);

        bool auxiliaryDone = false;
        Action onAuxiliaryComplete = () => auxiliaryDone = true;
        EventBus.Publish(new ExecuteAuxiliaryPhaseEvent(currentTurn, onAuxiliaryComplete));
        yield return new WaitUntil(() => auxiliaryDone);

        //Dormant phase (dormant units move)
        bool dormantDone = false;
        Action onDormantComplete = () => dormantDone = true;
        EventBus.Publish(new ExecuteDormantPhaseEvent(currentTurn, onDormantComplete));
        yield return new WaitUntil(() => dormantDone);

        //Aggressive phase (aggressive units action)
        bool aggressiveDone = false;
        Action onAggressiveComplete = () => aggressiveDone = true;
        EventBus.Publish(new ExecuteAggressivePhaseEvent(currentTurn, onAggressiveComplete));
        yield return new WaitUntil(() => aggressiveDone);

        //End turn
        EventBus.Publish(new EnemyTurnEndEvent(currentTurn));
        Debug.Log($"<color=yellow>=== Enemy Turn {currentTurn} Finished ===</color>");
    }
}


