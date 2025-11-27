using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EnemyAIEvents;

/// <summary>
/// Global Builder AI module, attached in hierarchy.
/// Manages all Enemy Builder units on the map during Builder phase.
/// </summary>
public class BuilderAction : MonoBehaviour
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

        //Check if current tile having grove structure
        foreach (int unitId in builderIds)
        {
            if (!unitManager.UnitObjects.ContainsKey(unitId)) 
                continue;

            //If builder unit is on a grove tile
            Vector2Int currentPos = unitManager.GetUnitPosition(unitId);
            HexTile currentTile = MapManager.Instance.GetTileAtHexPosition(currentPos);
            if (currentTile != null)
            {
                GroveBase grove = currentTile.currentBuilding?.GetComponent<GroveBase>();
                if (grove != null)
                {
                    EventBus.Publish(new BuilderDevelopGroveEvent(unitId, currentPos));
                    yield return new WaitForSeconds(0.3f);
                    continue;
                }
            }

            //If not, try to move
            //Skip if just spawned and can't move yet
            if (!unitManager.CanUnitMove(unitId))
                continue;

            //Find closest Grove
            Vector2Int target = FindClosestGrove(currentPos);

            //If already at grove, wait for next turn to build base
            if (currentPos == target)
                continue;
            
            Vector2Int? destination = AIPathFinder.FindNearestReachable(currentPos, target, EnemyUnitManager.Instance.GetUnitMoveRange(unitId));
            if (destination == null || destination.Value == currentPos)
                continue;

            EventBus.Publish(new EnemyMoveRequestEvent(unitId, destination.Value));
        }

        onCompleted?.Invoke();
    }

    private Vector2Int FindClosestGrove(Vector2Int from)
    {
        GroveBase[] groves = FindObjectsByType<GroveBase>(FindObjectsSortMode.None);

        if (groves.Length == 0)
            return from;

        if(groves.Length > 0)
        {
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

        //If there were no groves left on the map, move to random walkable tile
        List<HexTile> walkableTiles = new List<HexTile>();
        foreach (var tile in MapManager.Instance.GetAllTiles().Values)
        {
            if (tile.IsWalkableForAI() && !tile.IsBlockedByTurtleWall)
            {
                walkableTiles.Add(tile);
            }
        }

        if (walkableTiles.Count == 0)
            return from;

        HexTile randomTile = walkableTiles[UnityEngine.Random.Range(0, walkableTiles.Count)];
        return randomTile.HexCoords;
    }
}
