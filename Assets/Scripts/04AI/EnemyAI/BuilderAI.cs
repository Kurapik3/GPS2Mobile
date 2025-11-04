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
            Vector2Int closestGrove = FindClosestGrove(currentPos);

            //If already at grove, develop
            if (currentPos == closestGrove)
            {
                EventBus.Publish(new BuilderDevelopGroveEvent(unitId, closestGrove));
                continue;
            }

            //Move up to 2 tiles towards Grove
            //Vector2Int? moveTarget = AIPathFinder.FindNearestReachable(currentPos, closestGrove, 2);
            Vector2Int? moveTarget = new Vector2Int(3, 3);

            if (moveTarget == null)
            {
                Debug.LogWarning($"[BuilderAI] Builder {unitId} cannot reach any hex toward grove {closestGrove}.");
                continue;
            }

            if (moveTarget.Value != currentPos)
            {
                EventBus.Publish(new EnemyMoveRequestEvent(unitId, moveTarget.Value));
                yield return new WaitForSeconds(0.5f);
            }

            //Check if reached the grove after move
            if (moveTarget.Value == closestGrove)
            {
                EventBus.Publish(new BuilderDevelopGroveEvent(unitId, closestGrove));
            }
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
