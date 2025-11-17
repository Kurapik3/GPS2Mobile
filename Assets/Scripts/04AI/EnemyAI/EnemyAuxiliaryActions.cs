using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EnemyAIEvents;

/// <summary>
/// Handles auxiliary actions: 
///  - ID 3: Developing 1 tile (fish/debris)
///  - ID 6: Unlocking Tech (score only)
/// Each successful action gives score via EnemyTracker.
/// </summary>
public class EnemyAuxiliaryActions : MonoBehaviour
{
    [SerializeField] private float stepDelay = 0.5f;

    private bool actionCompleted = false;
    private bool actionSuccess = false;

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

        //If baseCount == 2, maxActions == 1
        //   baseCount == 3, maxActions == 2
        //   baseCount == 4, maxActions == 2
        //   baseCount == 5, maxActions == 3
        int maxActions = Mathf.FloorToInt((baseCount + 1) / 2f);
        if (maxActions <= 0)
        {
            Debug.Log($"[EnemyAuxiliaryActions] {baseCount} bases ? 0 auxiliary actions allowed.");
            onCompleted?.Invoke();
            yield break;
        }

        //Only units that are not Builder unit and havent act during the current turn can execute auxiliary action
        List<int> candidateUnitIds = new List<int>();
        foreach (int id in eum.GetOwnedUnitIds())
        {
            if (!eum.IsBuilderUnit(id) && !eum.HasUnitActedThisTurn(id))
            {
                candidateUnitIds.Add(id);
            }
        }

        if (candidateUnitIds.Count == 0)
        {
            Debug.Log("[EnemyAuxiliaryActions] No eligible units for auxiliary actions.");
            onCompleted?.Invoke();
            yield break;
        }

        int numToSelect = Mathf.Min(maxActions, candidateUnitIds.Count);
        for (int i = 0; i < numToSelect; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, candidateUnitIds.Count);
            int unitId = candidateUnitIds[randomIndex];
            candidateUnitIds.RemoveAt(randomIndex); //Prevent repetition 

            //Weighted random: 70% develop tile, 30% unlock tech
            float roll = UnityEngine.Random.value;
            if (roll < 0.7f)
                yield return ExecuteDevelopTileAction(unitId);
            else
                yield return ExecuteUnlockTechAction();

            yield return new WaitForSeconds(stepDelay / AIController.AISpeedMultiplier);
        }

        onCompleted?.Invoke();
    }

    private IEnumerator ExecuteDevelopTileAction(int unitId)
    {
        var eum = EnemyUnitManager.Instance;
        Vector2Int currentPos = eum.GetUnitPosition(unitId);

        HexTile targetTile = FindClosestUndevelopedResource(currentPos);
        if (targetTile == null)
        {
            Debug.Log($"[EnemyAuxiliaryActions] Unit {unitId}: No undeveloped resource found.");
            yield break;
        }

        //If unit is on the target tile
        if (currentPos == targetTile.HexCoords)
        {
            yield return RequestDevelopTile(unitId, targetTile.HexCoords);
            eum.MarkUnitAsActed(unitId);
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