using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EnemyAIEvents;

/// <summary>
/// Dormant movement logic:
/// For units that are not visible to player, move 50% towards origin, 50% away.
/// </summary>
public class DormantState : MonoBehaviour
{
    [SerializeField] private float stepDelay = 1f;

    private void OnEnable()
    {
        EventBus.Subscribe<ExecuteDormantPhaseEvent>(OnExplorePhase);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<ExecuteDormantPhaseEvent>(OnExplorePhase);
    }

    private void OnExplorePhase(ExecuteDormantPhaseEvent evt)
    {
        StartCoroutine(RunDormantPhase(evt.Turn, evt.OnCompleted));
    }

    private IEnumerator RunDormantPhase(int turn, Action onCompleted)
    {
        var eum = EnemyUnitManager.Instance;
        if (eum == null)
        {
            yield return null; //To ensure async
            onCompleted?.Invoke();
            yield break;
        }

        var unitIds = eum.GetOwnedUnitIds();
        if (unitIds == null || unitIds.Count == 0)
        {
            yield return null;
            onCompleted?.Invoke();
            yield break;
        }

        Vector2Int origin = Vector2Int.zero;
        System.Random rng = new System.Random();

        foreach (var id in unitIds)
        {
            eum.LockState(id);

            if (eum.IsBuilderUnit(id))
            {
                Debug.Log($"[DormantAI] Unit {id} is Builder, do nothing.");
                continue;
            }

            //Skip visible (aggressive)
            if (eum.IsUnitVisibleToPlayer(id))
            {
                eum.TrySetState(id, EnemyUnitManager.AIState.Aggressive);
                continue;
            }

            if (!eum.CanUnitMove(id))
            {
                Debug.Log($"[DormantAI] Unit {id} just spawned, skip movement.");
                continue;
            }

            Vector2Int current = eum.GetUnitPosition(id);
            List<Vector2Int> candidates = AIPathFinder.GetReachableHexes(current, eum.GetUnitMoveRange(id));
            //Filter walkable
            candidates.RemoveAll(hex => !MapManager.Instance.CanUnitStandHere(hex));

            if (candidates.Count == 0) 
                continue;

            bool moveTowards = rng.NextDouble() < 0.5;
            Vector2Int chosen = ChooseHexDirection(candidates, current, origin, moveTowards);

            EventBus.Publish(new EnemyMoveRequestEvent(id, chosen));
            yield return new WaitForSeconds(stepDelay / AIController.AISpeedMultiplier);
        }

        onCompleted?.Invoke();
    }

    private Vector2Int ChooseHexDirection(List<Vector2Int> candidates, Vector2Int current, Vector2Int origin, bool moveTowards)
    {
        if (moveTowards)
        {
            var step = AIPathFinder.TryMove(current, origin, 1);
            if (step.HasValue)
                return step.Value;
        }
        else
        {
            int currentDist = AIPathFinder.GetHexDistance(current, origin);
            List<Vector2Int> farther = new();
            foreach (var hex in candidates)
            {
                if (AIPathFinder.GetHexDistance(hex, origin) > currentDist)
                    farther.Add(hex);
            }

            if (farther.Count > 0)
                return AIPathFinder.RandomChoice(farther);
        }

        return AIPathFinder.RandomChoice(candidates);
    }
}
