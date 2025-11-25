using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static EnemyAIEvents;

/// <summary>
/// Aggressive logic for visible units.
/// Prioritizes bases > sea monsters > units when choosing targets.
/// </summary>
public class AggressiveState : MonoBehaviour
{
    [SerializeField] private float stepDelay = 1f;

    private void OnEnable()
    {
        EventBus.Subscribe<ExecuteAggressivePhaseEvent>(OnAggressivePhase);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<ExecuteAggressivePhaseEvent>(OnAggressivePhase);
    }

    private void OnAggressivePhase(ExecuteAggressivePhaseEvent evt)
    {
        Debug.Log($"[OnAggressivePhase Starting aggressive phase for turn {evt.Turn}.");
        StartCoroutine(RunAggressivePhase(evt.Turn, evt.OnCompleted));
    }

    private IEnumerator RunAggressivePhase(int turn, Action onCompleted)
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

        foreach (var id in unitIds)
        {
            eum.LockState(id);

            if (eum.IsUnitType(id, "Builder"))
            {
                Debug.Log($"[AggressiveAI] Unit {id} is Builder, do nothing.");
                continue;
            }

            if (eum.HasUnitActedThisTurn(id))
            {
                Debug.Log($"[AggressiveAI] Unit {id} just acted an action, do nothing.");
                continue;
            }

            if (!eum.IsUnitVisibleToPlayer(id))
            {
                eum.TrySetState(id, EnemyUnitManager.AIState.Dormant);
                continue;
            }

            if (!eum.CanUnitAttack(id))
            {
                Debug.Log($"[AggressiveAI] Unit {id} just spawned, skip movement.");
                continue;
            }

            Vector2Int currentPos = eum.GetUnitPosition(id);
            int atkRange = eum.GetUnitAttackRange(id);

            //Gather targets in range
            List<GameObject> baseTargets, seaTargets, unitTargets;
            GetTargetsInRange(currentPos, atkRange, out baseTargets, out seaTargets, out unitTargets);

            GameObject target = null;

            //Choose target by priority (better to have player unit/base manager that records unitID / baseID / basePosition
            if (baseTargets.Count > 0)
            {
                target = baseTargets[UnityEngine.Random.Range(0, baseTargets.Count)];
                Debug.Log($"[AggressiveAI] Unit {id} will attack BASE: {target.name} at {target.transform.position}");
            }
            else if (seaTargets.Count > 0)
            {
                target = seaTargets[UnityEngine.Random.Range(0, seaTargets.Count)];
                Debug.Log($"[AggressiveAI] Unit {id} will attack SEA MONSTER: {target.name} at {target.transform.position}");
            }
            else if (unitTargets.Count > 0)
            {
                target = unitTargets[UnityEngine.Random.Range(0, unitTargets.Count)];
                Debug.Log($"[AggressiveAI] Unit {id} will attack PLAYER UNIT: {target.name} at {target.transform.position}");
            }

            if (target != null && IsTargetValid(target))
            {
                EventBus.Publish(new EnemyAttackRequestEvent(id, target));
                eum.MarkUnitAsActed(id);
                yield return new WaitForSeconds(stepDelay / AIController.AISpeedMultiplier);
                continue; //End the turn after attack
            }

            //If no targets, move 1 tile toward locked or new base
            Vector2Int targetBasePos = ChooseClosestPlayerBase(currentPos);
            Debug.Log($"[AggressiveAI] Unit {id} target base position: {targetBasePos}");

            //Only move if not already on target
            if (targetBasePos != currentPos)
            {
                int moveRange = eum.GetUnitMoveRange(id);
                Debug.Log($"[AggressiveAI] Unit {id} move range: {moveRange}");

                Vector2Int? nextMove = AIPathFinder.TryMove(currentPos, targetBasePos, moveRange);

                if (nextMove.HasValue)
                {
                    Debug.Log($"[AggressiveAI] Unit {id} MOVING from {currentPos} to {nextMove.Value}");
                    EventBus.Publish(new EnemyMoveRequestEvent(id, nextMove.Value));
                }
                else
                {
                    Debug.Log($"[AggressiveAI] Unit {id} CANNOT MOVE - TryMove returned NULL");
                }
            }
            else
            {
                Debug.Log($"[AggressiveAI] Unit {id} already at target base position, no movement needed.");
            }


            eum.MarkUnitAsActed(id);
            yield return new WaitForSeconds(stepDelay / AIController.AISpeedMultiplier);
            //NO attack after moving
        }

        onCompleted?.Invoke();
    }

    #region Target
    private void GetTargetsInRange(Vector2Int from, int range, out List<GameObject> baseTargets, out List<GameObject> seaTargets, out List<GameObject> unitTargets)
    {
        baseTargets = new();
        seaTargets = new();
        unitTargets = new();

        var allTiles = MapManager.Instance.GetAllTiles();
        foreach (var kvp in allTiles)
        {
            Vector2Int hex = kvp.Key;
            HexTile tile = kvp.Value;

            //Check target tile is within the attack range
            if (AIPathFinder.GetHexDistance(from, hex) > range)
                continue;

            //Tree Base
            if (tile.HasTreeBase && tile.currentBuilding != null)
            {
                baseTargets.Add(tile.currentBuilding.gameObject);
                Debug.Log($"[AggressiveAI] Found PLAYER Base target at {hex}");
            }

            //Player units
            if (tile.currentUnit != null)
            {
                if (TechTree.instance.IsCamouflage && tile.currentUnit.unitName == "Scout")
                    continue;
                unitTargets.Add(tile.currentUnit.gameObject);
                Debug.Log($"[AggressiveAI] Found PlayerUnit target at {hex}");
            }

            //Sea monsters
            if (tile.currentSeaMonster != null)
            {
                seaTargets.Add(tile.currentSeaMonster.gameObject);
                Debug.Log($"[AggressiveAI] Found SeaMonster target at {hex}");
            }
        }
    }

    private bool IsTargetValid(GameObject target)
    {
        if (target == null) 
            return false;

        var unit = target.GetComponent<UnitBase>();
        if (unit != null && unit.hp <= 0) 
            return false;

        var tree = target.GetComponent<TreeBase>();
        if (tree != null && tree.health <= 0) 
            return false;

        var sea = target.GetComponent<SeaMonsterBase>();
        if (sea != null && sea.health <= 0) return false;

        return true;
    }

    private Vector2Int ChooseClosestPlayerBase(Vector2Int from)
    {
        TreeBase closestBase = null;
        int minDist = int.MaxValue;

        var allTiles = MapManager.Instance.GetAllTiles();
        foreach (var kvp in allTiles)
        {
            Vector2Int hex = kvp.Key;
            HexTile tile = kvp.Value;

            if (!tile.HasTreeBase || tile.currentBuilding == null)
                continue;

            TreeBase treeBase = tile.currentBuilding as TreeBase;
            if (treeBase == null)
                continue;

            int dist = AIPathFinder.GetHexDistance(from, hex);
            if (dist < minDist)
            {
                minDist = dist;
                closestBase = treeBase;
            }
        }

        if (closestBase == null || closestBase.currentTile == null)
        {
            Debug.LogWarning("[AggressiveAI] NO PLAYER BASES FOUND! Aggressive unit will not move toward base.");
            return from;
        }

        Debug.Log($"[AggressiveAI] Closest player base at {closestBase.currentTile.HexCoords}, distance: {minDist}");
        return closestBase.currentTile.HexCoords;
    }
    #endregion
}
