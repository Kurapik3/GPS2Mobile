using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EnemyAIEvents;

public class EnemyAuxiliaryActions : MonoBehaviour
{
    [SerializeField] private float stepDelay = 0.5f;

    private bool actionCompleted = false;
    private bool actionSuccess = false;
    private int unlockTechCount = 0;

    private void OnEnable()
    {
        EventBus.Subscribe<ExecuteAuxiliaryPhaseEvent>(OnAuxiliaryPhase);
        EventBus.Subscribe<EnemyAuxiliaryActionExecutedEvent>(OnAuxiliaryActionExecuted);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<ExecuteAuxiliaryPhaseEvent>(OnAuxiliaryPhase);
        EventBus.Unsubscribe<EnemyAuxiliaryActionExecutedEvent>(OnAuxiliaryActionExecuted);
    }

    private void OnAuxiliaryPhase(ExecuteAuxiliaryPhaseEvent evt)
    {
        StartCoroutine(RunAuxiliaryPhase(evt.Turn, evt.OnCompleted));
    }

    private IEnumerator RunAuxiliaryPhase(int turn, Action onCompleted)
    {
        var eum = EnemyUnitManager.Instance;
        var ebm = EnemyBaseManager.Instance;

        if (eum == null || ebm == null)
        {
            onCompleted?.Invoke();
            yield break;
        }

        //Based on enemy base count, decide the maximum auxiliary actions enemy units can do
        int baseCount = ebm.Bases.Count;
        Debug.Log($"[EnemyAuxiliaryActions] Base count: {ebm.Bases.Count}");

        //If baseCount == 2, maxActions == 1
        //   baseCount == 3, maxActions == 1
        //   baseCount == 4, maxActions == 2
        //   baseCount == 5, maxActions == 2
        int maxActions = baseCount / 2;

        if (maxActions <= 0)
        {
            Debug.Log($"[EnemyAuxiliaryActions] {baseCount} bases ? 0 auxiliary actions allowed.");
            onCompleted?.Invoke();
            yield break;
        }


        //Only units that are not Builder unit and havent act during the current turn can execute auxiliary action
        List<int> priorityUnits = new List<int>();
        List<int> normalUnits = new List<int>();

        foreach (int id in eum.GetOwnedUnitIds())
        {
            if (eum.IsUnitType(id, "Builder") || eum.HasUnitActedThisTurn(id))
                continue;

            HexTile tile = MapManager.Instance.GetTile(eum.GetUnitPosition(id));
            bool hasResource = EnemyTurfManager.Instance.IsInTurf(tile.HexCoords) && tile != null && (tile.fishTile != null || tile.debrisTile != null);

            if (hasResource)
                priorityUnits.Add(id); //Priority units: already on development tile, choose develop action first
            else
                normalUnits.Add(id);
        }

        if (priorityUnits.Count + normalUnits.Count == 0)
        {
            Debug.Log("[EnemyAuxiliaryActions] No eligible units for auxiliary actions.");
            onCompleted?.Invoke();
            yield break;
        }

        int numToSelect = Mathf.Min(maxActions, priorityUnits.Count + normalUnits.Count);
        int actionsDone = 0;
        for (int i = 0; i < numToSelect; i++)
        {
            int unitId;
            if (priorityUnits.Count > 0)
            {
                int idx = UnityEngine.Random.Range(0, priorityUnits.Count);
                unitId = priorityUnits[idx];
                priorityUnits.RemoveAt(idx);

                yield return ExecuteDevelopTileAction(unitId);
            }
            else
            {
                if (normalUnits.Count == 0)
                    break;

                int idx = UnityEngine.Random.Range(0, normalUnits.Count);
                unitId = normalUnits[idx];
                normalUnits.RemoveAt(idx);

                //Weighted random: 70% develop tile, 30% unlock tech
                float roll = UnityEngine.Random.value;

                if (roll < 0.3f && unlockTechCount < 13)
                    yield return ExecuteUnlockTechAction();
                else if (roll < 0.7f && eum.CanUnitMove(unitId))
                    yield return ExecuteDevelopTileAction(unitId);
            }

            actionsDone++;
            Debug.Log($"[EnemyAuxiliaryActions] Turn {turn}: {actionsDone}/{numToSelect} auxiliary actions executed so far.");

            yield return new WaitForSeconds(stepDelay / AIController.AISpeedMultiplier);
        }

        Debug.Log($"[EnemyAuxiliaryActions] Turn {turn} completed. Total auxiliary actions: {actionsDone}");
        onCompleted?.Invoke();
    }

    private IEnumerator ExecuteDevelopTileAction(int unitId)
    {
        var eum = EnemyUnitManager.Instance;
        Vector2Int currentPos = eum.GetUnitPosition(unitId);

        HexTile currentTile = MapManager.Instance.GetTile(currentPos);

        //If unit is on the target tile
        if (currentTile != null && EnemyTurfManager.Instance.IsInTurf(currentPos) && (currentTile.fishTile != null || currentTile.debrisTile != null))
        {
            yield return RequestDevelopTile(unitId, currentPos);
            eum.MarkUnitAsActed(unitId);
            yield break;
        }

        HexTile targetTile = FindClosestUndevelopedResource(currentPos);
        if (targetTile == null)
        {
            Debug.Log($"[EnemyAuxiliaryActions] Unit {unitId}: No undeveloped resource found.");
            yield break;
        }

        //If not, try move to the target tile
        int moveRange = eum.GetUnitMoveRange(unitId);
        Vector2Int? nextStep = AIPathFinder.FindNearestReachable(currentPos, targetTile.HexCoords, moveRange);
        if (!nextStep.HasValue || nextStep.Value == currentPos)
        {
            Debug.Log($"[EnemyAuxiliaryActions] Unit {unitId} cannot move towards {targetTile.HexCoords}, skipping move.");
            yield break;
        }

        EventBus.Publish(new EnemyMoveRequestEvent(unitId, nextStep.Value));
        Debug.Log($"[EnemyAuxiliaryActions] Unit {unitId} moves from {currentPos} to {nextStep.Value}");

        eum.MarkUnitAsActed(unitId);
        yield return null;
    }

    private IEnumerator RequestDevelopTile(int unitId, Vector2Int pos)
    {
        actionCompleted = false;

        Debug.Log($"[EnemyAuxiliaryActions] Request tile develop at {pos}");
        EventBus.Publish(new EnemyAuxiliaryActionRequestEvent(3, unitId, pos));

        // Wait until Executor fires the result event
        while (!actionCompleted)
            yield return null;

        if (actionSuccess)
            Debug.Log($"[EnemyAuxiliaryActions] Tile {pos} successfully developed");
        else
            Debug.Log($"[EnemyAuxiliaryActions] Tile {pos} develop failed");
    }

    private IEnumerator ExecuteUnlockTechAction()
    {
        unlockTechCount ++;
        actionCompleted = false;
        
        Debug.Log("[EnemyAuxiliaryActions] Request unlock tech");
        EventBus.Publish(new EnemyAuxiliaryActionRequestEvent(6, -1, Vector2Int.zero));

        while (!actionCompleted)
            yield return null;

        yield return null;
    }

    private void OnAuxiliaryActionExecuted(EnemyAuxiliaryActionExecutedEvent evt)
    {
        actionCompleted = true;
        actionSuccess = evt.Success;
    }

    private HexTile FindClosestUndevelopedResource(Vector2Int from)
    {
        HexTile closest = null;
        int minDist = int.MaxValue;

        foreach (var tile in MapManager.Instance.GetTiles())
        {
            bool hasResource = tile.fishTile != null || tile.debrisTile != null;

            if (!hasResource)
                continue;

            if (!EnemyTurfManager.Instance.IsInTurf(tile.HexCoords))
                continue;

            int dist = AIPathFinder.GetHexDistance(from, tile.HexCoords);
            if (dist < minDist)
            {
                minDist = dist;
                closest = tile;
            }
        }

        return closest;
    }
}