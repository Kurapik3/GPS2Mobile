using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EnemyAIEvents;

/// <summary>
/// Aggressive logic for visible units.
/// Prioritizes bases > sea monsters > units when choosing targets.
/// </summary>
public class AggressiveState : MonoBehaviour
{
    [SerializeField] private float stepDelay = 1f;
    private Dictionary<int, Vector2Int> lockedTargetBases = new(); // unitId -> target base hex

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

            if (eum.IsBuilderUnit(id))
            {
                Debug.Log($"[AggressiveAI] Unit {id} is Builder, do nothing.");
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

            if (target != null)
            {
                EventBus.Publish(new EnemyAttackRequestEvent(id, target));
                yield return new WaitForSeconds(stepDelay / AIController.AISpeedMultiplier);
                continue; //End the turn after attack
            }

            //If no targets, move 1 tile toward locked or new base
            Vector2Int targetBasePos;
            if (!lockedTargetBases.TryGetValue(id, out targetBasePos))
            {
                targetBasePos = ChooseClosestPlayerBase(currentPos);
                lockedTargetBases[id] = targetBasePos;
            }

            //Only move if not already on target
            if (targetBasePos != currentPos) 
            {
                Vector2Int? nextMove = AIPathFinder.TryMove(currentPos, targetBasePos, eum.GetUnitMoveRange(id));
                if (nextMove.HasValue)
                    EventBus.Publish(new EnemyMoveRequestEvent(id, nextMove.Value));
            }

            yield return new WaitForSeconds(stepDelay / AIController.AISpeedMultiplier);
            //NO attack after moving
        }

        onCompleted?.Invoke();
    }

    #region Target Gathering
    private void GetTargetsInRange(Vector2Int from, int range, out List<GameObject> baseTargets, out List<GameObject> seaTargets, out List<GameObject> unitTargets)
    {
        baseTargets = new();
        seaTargets = new();
        unitTargets = new();

        //Tree Base
        TreeBase[] playerBases = FindObjectsByType<TreeBase>(FindObjectsSortMode.None);
        foreach (var pb in playerBases)
        {
            if (pb == null || pb.currentTile == null) 
                continue;
            if (AIPathFinder.GetHexDistance(from, pb.currentTile.HexCoords) <= range)
            {
                baseTargets.Add(pb.gameObject);
                Debug.Log($"[AggressiveAI] Found Base target: {pb.name} at {pb.currentTile.HexCoords}");
            }
        }

        //Player Units
        GameObject[] playerUnits = GameObject.FindGameObjectsWithTag("PlayerUnit");
        foreach (var pu in playerUnits)
        {
            if (pu == null) 
                continue;
            Vector2Int hex = MapManager.Instance.WorldToHex(pu.transform.position);
            if (AIPathFinder.GetHexDistance(from, hex) <= range)
            {
                unitTargets.Add(pu);
                Debug.Log($"[AggressiveAI] Found PlayerUnit target: {pu.name} at {hex}");
            }
        }

        // Sea Monsters
        if (SeaMonsterManager.Instance != null)
        {
            foreach (var sm in SeaMonsterManager.Instance.GetAllMonsters())
            {
                if (sm.currentTile == null) 
                    continue;
                if (AIPathFinder.GetHexDistance(from, sm.currentTile.HexCoords) <= range)
                {
                    seaTargets.Add(sm.gameObject);
                    Debug.Log($"[AggressiveAI] Found SeaMonster target: {sm.name} at {sm.currentTile.HexCoords}");
                }
            }
        }
    }

    #endregion

    private Vector2Int ChooseClosestPlayerBase(Vector2Int from)
    {
        TreeBase[] playerBases = FindObjectsByType<TreeBase>(FindObjectsSortMode.None);
        Debug.Log($"[AggressiveAI] Found {playerBases.Length} PlayerBase objects in scene");

        if (playerBases.Length == 0)
        {
            Debug.LogWarning("[AggressiveAI] NO PLAYER BASES FOUND! Aggressive unit will not move toward base.");
            return from;
        }

        TreeBase closestBase = null;
        int minDist = int.MaxValue;

        foreach (TreeBase pb in playerBases)
        {
            if (pb == null || pb.currentTile == null)
                continue;

            Vector2Int pos = pb.currentTile.HexCoords;
            int dist = AIPathFinder.GetHexDistance(from, pos);
            if (dist < minDist)
            {
                minDist = dist;
                closestBase = pb;
            }
        }

        if (closestBase == null || closestBase.currentTile == null)
            return from;

        return closestBase.currentTile.HexCoords;
    }
}
