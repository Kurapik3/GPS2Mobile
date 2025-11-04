using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EnemyAIEvents;

/// <summary>
/// Aggressive logic for visible units.
/// Prioritizes bases > sea monsters > units when choosing targets.
/// </summary>
public class AggressiveAI : MonoBehaviour
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

            Vector2Int currentPos = eum.GetUnitPosition(id);
            int range = eum.GetUnitAttackRange(id);

            //Gather targets in range
            List<int> baseTargets = GetBasesInRange(currentPos, range);
            List<int> seaTargets = GetSeaMonstersInRange(currentPos, range);
            List<int> unitTargets = GetPlayerUnitsInRange(currentPos, range);

            //Choose target by priority (better to have player unit/base manager that records unitID / baseID / basePosition
            if (baseTargets.Count > 0)
            {
                int targetBaseId = baseTargets[UnityEngine.Random.Range(0, baseTargets.Count)];
                EventBus.Publish(new EnemyAttackRequestEvent(id, targetBaseId));
                yield return new WaitForSeconds(stepDelay / AIController.AISpeedMultiplier);
                continue; //Attack ends turn
            }
            else if (seaTargets.Count > 0)
            {
                int targetMonsterId = seaTargets[UnityEngine.Random.Range(0, seaTargets.Count)];
                EventBus.Publish(new EnemyAttackRequestEvent(id, targetMonsterId));
                yield return new WaitForSeconds(stepDelay / AIController.AISpeedMultiplier);
                continue; //Attack ends turn
            }
            else if (unitTargets.Count > 0)
            {
                int targetUnitId = unitTargets[UnityEngine.Random.Range(0, unitTargets.Count)];
                EventBus.Publish(new EnemyAttackRequestEvent(id, targetUnitId));
                yield return new WaitForSeconds(stepDelay / AIController.AISpeedMultiplier);
                continue; //Attack ends turn
            }

            //If no targets, move 1 tile toward locked or new base
            Vector2Int targetBasePos;
            if (!lockedTargetBases.TryGetValue(id, out targetBasePos))
            {
                targetBasePos = ChooseClosestPlayerBase(currentPos);
                lockedTargetBases[id] = targetBasePos; //Lock this base
            }

            List<Vector2Int> reachable = AIPathFinder.GetReachableHexes(currentPos, 1);
            reachable.RemoveAll(h => !MapManager.Instance.CanUnitStandHere(h));

            if (reachable.Count > 0)
            {
                reachable.Sort((a, b) => AIPathFinder.GetHexDistance(a, targetBasePos).CompareTo(AIPathFinder.GetHexDistance(b, targetBasePos)));
                Vector2Int chosen = reachable[0];
                EventBus.Publish(new EnemyMoveRequestEvent(id, chosen));
            }

            yield return new WaitForSeconds(stepDelay / AIController.AISpeedMultiplier);
            //NO attack after moving
        }

        onCompleted?.Invoke();
    }

    #region Target Gathering
    private List<int> GetPlayerUnitsInRange(Vector2Int from, int range)
    {
        List<int> result = new();
        var players = GameObject.FindGameObjectsWithTag("PlayerUnit");
        foreach (var go in players)
        {
            Vector2Int hex = MapManager.Instance.WorldToHex(go.transform.position);
            if (AIPathFinder.GetHexDistance(from, hex) <= range)
                result.Add(go.GetComponent<UnitBase>().UnitID);
        }
        return result;
    }
    private List<int> GetBasesInRange(Vector2Int from, int range)
    {
        List<int> result = new();
        var playerBases = GameObject.FindGameObjectsWithTag("PlayerBase");
        foreach (var go in playerBases)
        {
            Vector2Int hex = MapManager.Instance.WorldToHex(go.transform.position);
            if (AIPathFinder.GetHexDistance(from, hex) <= range)
                result.Add(go.GetInstanceID());
        }
        return result;
    }

    private List<int> GetSeaMonstersInRange(Vector2Int from, int range)
    {
        List<int> result = new();
        if (SeaMonsterManager.Instance == null)
            return result;
        var monsters = SeaMonsterManager.Instance.GetAllMonsters();
        foreach (var m in monsters)
        {
            int dist = AIPathFinder.GetHexDistance(from, m.CurrentTile.HexCoords);
            if (dist <= range)
                result.Add(m.MonsterId);
        }
        return result;
    }
    #endregion

    private Vector2Int ChooseClosestPlayerBase(Vector2Int from)
    {
        var playerBases = GameObject.FindGameObjectsWithTag("PlayerBase");
        if (playerBases.Length == 0)
        {
            Debug.LogWarning("[AggressiveAI] No PlayerBase found with tag 'PlayerBase'!");
            return from;
        }
        List<Vector2Int> candidateHexes = new();

        foreach (var go in playerBases)
        {
            if (go == null) 
                continue;
            candidateHexes.Add(MapManager.Instance.WorldToHex(go.transform.position));
        }

        if (candidateHexes.Count == 0)
        {
            Debug.LogWarning("[AggressiveAI] No valid player bases found.");
            return from;
        }

        int minDist = int.MaxValue;
        List<Vector2Int> closest = new();

        foreach (var hex in candidateHexes)
        {
            int dist = AIPathFinder.GetHexDistance(from, hex);
            if (dist < minDist)
            {
                minDist = dist;
                closest.Clear();
                closest.Add(hex);
            }
            else if (dist == minDist)
            {
                closest.Add(hex);
            }
        }

        if (closest.Count == 0)
            return from;

        return closest[UnityEngine.Random.Range(0, closest.Count)];
    }
}
