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

    private void OnEnable()
    {
        EventBus.Subscribe<ExecuteAuxiliaryPhaseEvent>(OnAuxiliaryPhase);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<ExecuteAuxiliaryPhaseEvent>(OnAuxiliaryPhase);
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

            //Choose action based on weighted chance
            float roll = UnityEngine.Random.value;
            if (roll < 0.7f)
                yield return ExecuteDevelopTileAction(unitId); // 70%
            else
                yield return ExecuteUnlockTechAction(); // 30%

            yield return new WaitForSeconds(stepDelay / AIController.AISpeedMultiplier);
        }

        onCompleted?.Invoke();
    }

    private IEnumerator ExecuteDevelopTileAction(int unitId)
    {
        var eum = EnemyUnitManager.Instance;
        Vector2Int currentPos = eum.GetUnitPosition(unitId);

        Vector2Int? target = FindClosestUndevelopedResource(currentPos);
        if (!target.HasValue)
        {
            Debug.Log($"[EnemyAuxiliaryActions] Unit {unitId}: No undeveloped resource found.");
            yield break;
        }

        //If unit already on the resource tile
        if (currentPos == target.Value)
        {
            if (DevelopResourceAt(target.Value))
            {
                Debug.Log($"[EnemyAuxiliaryActions] Unit {unitId} developed resource at {target.Value}");
                EventBus.Publish(new EnemyAuxiliaryActionExecutedEvent(3, true));
                EnemyTracker.Instance?.AddScore(200);
                eum.MarkUnitAsActed(unitId);
            }
            yield break;
        }

        //Otherwise move toward it
        Vector2Int? nextStep = AIPathFinder.FindNearestReachable(currentPos, target.Value, 1);
        if (nextStep.HasValue && nextStep.Value != currentPos)
        {
            EventBus.Publish(new EnemyMoveRequestEvent(unitId, nextStep.Value));
        }
    }

    private IEnumerator ExecuteUnlockTechAction()
    {
        Debug.Log("[EnemyAuxiliaryActions] Successfully unlocked a tech branch.");
        EventBus.Publish(new EnemyAuxiliaryActionExecutedEvent(6, true));
        EnemyTracker.Instance?.AddScore(500);

        yield return null;
    }

    private Vector2Int? FindClosestUndevelopedResource(Vector2Int from)
    {
        List<Vector2Int> candidates = new();

        //foreach (var tile in FindObjectsByType<FishTile>(FindObjectsSortMode.None))
        //    if (tile != null && !tile.isDeveloped) candidates.Add(tile.HexCoords);

        //foreach (var tile in FindObjectsByType<DebrisTile>(FindObjectsSortMode.None))
        //    if (tile != null && !tile.isDeveloped) candidates.Add(tile.HexCoords);

        if (candidates.Count == 0)
            return null;

        candidates.Sort((a, b) => AIPathFinder.GetHexDistance(from, a).CompareTo(AIPathFinder.GetHexDistance(from, b)));
        return candidates[0];
    }

    private bool DevelopResourceAt(Vector2Int hex)
    {
        var map = MapManager.Instance;
        var tileObj = map.GetTile(hex);
        if (tileObj == null) return false;

        //var fish = tileObj.GetComponent<FishTile>();
        //if (fish != null && !fish.isDeveloped)
        //{
        //    fish.OnTileTapped();
        //    return true;
        //}

        //var debris = tileObj.GetComponent<DebrisTile>();
        //if (debris != null && !debris.isDeveloped)
        //{
        //    debris.OnTileTapped();
        //    return true;
        //}

        return false;
    }
}