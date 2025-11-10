using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EnemyAIEvents;

/// <summary>
/// Handles auxiliary actions: Develop Tile, Base Upgrade, Tech Tree Unlock
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
            Debug.Log($"[AuxiliaryAI] {baseCount} bases ? 0 auxiliary actions allowed.");
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
            Debug.Log("[AuxiliaryAI] No eligible units for auxiliary actions.");
            onCompleted?.Invoke();
            yield break;
        }

        int numToSelect = Mathf.Min(maxActions, candidateUnitIds.Count);
        for (int i = 0; i < numToSelect; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, candidateUnitIds.Count);
            int unitId = candidateUnitIds[randomIndex];
            candidateUnitIds.RemoveAt(randomIndex); //Prevent repetition 

            yield return ExecuteDevelopTileAction(unitId);

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
            Debug.Log($"[AuxiliaryAI] Unit {unitId}: No undeveloped resource found.");
            yield break;
        }

        // ? ???????? ? ?????????
        if (currentPos == target.Value)
        {
            if (DevelopResourceAt(target.Value)) // ????
            {
                Debug.Log($"[AuxiliaryAI] ? Unit {unitId} developed resource at {target.Value}");
                EventBus.Publish(new EnemyAuxiliaryActionExecutedEvent(3, true));
                EnemyTracker.Instance?.AddScore(200);

                // ? ????????“???”?Dormant/Aggressive ??????
                eum.MarkUnitAsActed(unitId);
            }
            yield break;
        }

        // ?? ?????????????????????
        Vector2Int? nextStep = AIPathFinder.FindNearestReachable(currentPos, target.Value, 1);
        if (nextStep.HasValue && nextStep.Value != currentPos)
        {
            Debug.Log($"[AuxiliaryAI] Unit {unitId} moves toward resource: {currentPos} ? {nextStep.Value}");
            EventBus.Publish(new EnemyMoveRequestEvent(unitId, nextStep.Value));

            // ?? ???????**?????**???**????????**?
            // ??????? Dormant/Aggressive Phase ?????????????
        }
    }

    // ———————— Helper Methods ————————

    private Vector2Int? FindClosestUndevelopedResource(Vector2Int from)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();

        // ?? FishTile ? DebrisTile?????
        foreach (var tile in FindObjectsByType<FishTile>(FindObjectsSortMode.None))
            if (tile != null && !tile.isDeveloped) candidates.Add(tile.HexCoords);

        foreach (var tile in FindObjectsByType<DebrisTile>(FindObjectsSortMode.None))
            if (tile != null && !tile.isDeveloped) candidates.Add(tile.HexCoords);

        if (candidates.Count == 0) return null;

        // ?????
        candidates.Sort((a, b) => AIPathFinder.GetHexDistance(from, a).CompareTo(AIPathFinder.GetHexDistance(from, b)));
        return candidates[0];
    }

    private bool DevelopResourceAt(Vector2Int hex)
    {
        FishTile fish = MapManager.Instance.GetTile(hex)?.GetComponent<FishTile>();
        if (fish != null && !fish.isDeveloped)
        {
            fish.Develop();
            return true;
        }

        DebrisTile debris = MapManager.Instance.GetTile(hex)?.GetComponent<DebrisTile>();
        if (debris != null && !debris.isDeveloped)
        {
            debris.Develop();
            return true;
        }

        return false;
    }
}