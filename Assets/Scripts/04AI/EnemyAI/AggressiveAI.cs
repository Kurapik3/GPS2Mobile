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
        StartCoroutine(RunAggressivePhase(evt.Turn));
    }

    private IEnumerator RunAggressivePhase(int turn)
    {
        var eum = EnemyUnitManager.Instance;
        if (eum == null)
        {
            EventBus.Publish(new AggressivePhaseEndEvent(turn));
            yield break;
        }

        var unitIds = eum.GetOwnedUnitIds();
        if (unitIds == null || unitIds.Count == 0)
        {
            EventBus.Publish(new AggressivePhaseEndEvent(turn));
            yield break;
        }

        foreach (var id in unitIds)
        {
            eum.LockState(id);

            //if (eum.IsBuilderUnit(id))
            //{
            //    Debug.Log($"[AggressiveAI] Unit {id} is Builder, do nothing.");
            //    continue;
            //}

            if (!IsUnitVisibleToPlayer(id))
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
                int targetBaseId = baseTargets[Random.Range(0, baseTargets.Count)];
                EventBus.Publish(new EnemyAttackRequestEvent(id, targetBaseId));
                yield return new WaitForSeconds(stepDelay / AIController.AISpeedMultiplier);
                continue; //Attack ends turn
            }
            else if (seaTargets.Count > 0)
            {
                int targetMonsterId = seaTargets[Random.Range(0, seaTargets.Count)];
                EventBus.Publish(new EnemyAttackRequestEvent(id, targetMonsterId));
                yield return new WaitForSeconds(stepDelay / AIController.AISpeedMultiplier);
                continue; //Attack ends turn
            }
            else if (unitTargets.Count > 0)
            {
                int targetUnitId = unitTargets[Random.Range(0, unitTargets.Count)];
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

        EventBus.Publish(new AggressivePhaseEndEvent(turn));
    }

    private bool IsUnitVisibleToPlayer(int unitId)
    {
        FogSystem fog = FindFirstObjectByType<FogSystem>();
        if (fog == null) 
            return true;
        var pos = EnemyUnitManager.Instance.GetUnitPosition(unitId);
        return fog.revealedTiles.Contains(pos);
    }

    #region Target Gathering
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
    #endregion

    private Vector2Int ChooseClosestPlayerBase(Vector2Int from)
    {
        var playerBases = GameObject.FindGameObjectsWithTag("PlayerBase");
        List<Vector2Int> candidateHexes = new();

        foreach (var go in playerBases)
            candidateHexes.Add(MapManager.Instance.WorldToHex(go.transform.position));

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

        return closest[Random.Range(0, closest.Count)];
    }
}
