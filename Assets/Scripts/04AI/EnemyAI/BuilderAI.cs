using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EnemyAIEvents;

/// <summary>
/// Global Builder AI module, attached in hierarchy.
/// Manages all Builder units on the map during Builder phase.
/// </summary>
public class BuilderAI : MonoBehaviour
{
    private EnemyUnitManager unitManager => EnemyUnitManager.Instance;

    private void OnEnable()
    {
        EventBus.Subscribe<ExecuteBuilderPhaseEvent>(OnBuilderPhase);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<ExecuteBuilderPhaseEvent>(OnBuilderPhase);
    }

    private void OnBuilderPhase(ExecuteBuilderPhaseEvent evt)
    {
        StartCoroutine(RunBuildersPhase(evt.Turn, evt.OnCompleted));
    }

    public IEnumerator RunBuildersPhase(int turn, Action onCompleted)
    {
        if (unitManager == null)
        {
            yield return null; //To ensure async
            onCompleted?.Invoke();
            yield break;
        }

        //Get all Builder unit IDs
        List<int> builderIds = new List<int>();
        foreach (var id in unitManager.GetOwnedUnitIds())
        {
            if (unitManager.GetUnitType(id) == "Builder")
                builderIds.Add(id);
        }

        //End phase if no builder units
        if (builderIds.Count == 0)
        {
            yield return null;
            onCompleted?.Invoke();
            yield break;
        }

        foreach (int unitId in builderIds)
        {
            if (!unitManager.UnitObjects.ContainsKey(unitId)) 
                continue;

            // Skip if just spawned and can't move yet
            if (!unitManager.CanUnitMove(unitId))
                continue;

            Vector2Int currentPos = unitManager.GetUnitPosition(unitId);

            //Find closest Grove
            Vector2Int target = FindClosestGrove(currentPos);

            //If already at grove, develop
            if (currentPos == target)
            {
                Debug.Log($"[BuilderAI] Builder {unitId} builds at {target}");
                EventBus.Publish(new BuilderDevelopGroveEvent(unitId, target));
                yield return new WaitForSeconds(0.3f);
                continue;
            }

            Vector2Int? step = AIPathFinder.FindNearestReachable(currentPos, target, 2);

            if (step == null)
            {
                Debug.LogWarning($"[BuilderAI] Builder {unitId} cannot find path toward {target}, skipping.");
                continue;
            }

            Vector2Int dest = step.Value;
            if (dest == currentPos)
            {
                Debug.Log($"[BuilderAI] Builder {unitId} stays in place (blocked).");
                continue;
            }

            Debug.Log($"[BuilderAI] Builder {unitId} -> Move request: {currentPos} -> {dest}");

            bool moveFinished = false;
            Action<EnemyMovedEvent> onMoved = (evt) =>
            {
                if (evt.UnitId == unitId)
                    moveFinished = true;
            };

            EventBus.Subscribe<EnemyMovedEvent>(onMoved);
            EventBus.Publish(new EnemyMoveRequestEvent(unitId, dest));

            // Wait max 2 seconds to avoid deadlock
            float wait = 0f;
            while (!moveFinished && wait < 2f)
            {
                wait += Time.deltaTime;
                yield return null;
            }
            EventBus.Unsubscribe<EnemyMovedEvent>(onMoved);

            if (!moveFinished)
            {
                Debug.LogWarning($"[BuilderAI] WARNING: Builder {unitId} never received EnemyMovedEvent!");
            }

            Vector2Int newPos = unitManager.GetUnitPosition(unitId);

            if (newPos == target)
            {
                Debug.Log($"[BuilderAI] Builder {unitId} arrived & builds at {target}");
                EventBus.Publish(new BuilderDevelopGroveEvent(unitId, target));
                yield return new WaitForSeconds(0.3f);
            }

            yield return new WaitForSeconds(0.15f);
        }


        onCompleted?.Invoke();
    }

    private Vector2Int FindClosestGrove(Vector2Int from)
    {
        GroveBase[] groves = FindObjectsByType<GroveBase>(FindObjectsSortMode.None);
        Debug.Log($"[BuilderAI] Found {groves.Length} GroveBase objects in scene");

        if (groves.Length == 0)
        {
            Debug.LogWarning("[BuilderAI] NO GROVES FOUND! Builder will not move.");
            return from;
        }

        GroveBase closestGrove = groves[0];
        int minDist = AIPathFinder.GetHexDistance(from, closestGrove.currentTile?.HexCoords ?? from);

        foreach (GroveBase grove in groves)
        {
            if (grove == null || grove.currentTile == null)
                continue;

            Vector2Int pos = grove.currentTile.HexCoords;
            int dist = AIPathFinder.GetHexDistance(from, pos);
            if (dist < minDist)
            {
                minDist = dist;
                closestGrove = grove;
            }
        }

        if (closestGrove == null || closestGrove.currentTile == null)
            return from;
        return closestGrove.currentTile.HexCoords;
    }
}
