using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EnemyAIEvents;

/// <summary>
/// Handles executing enemy actions: spawn, move, attack.
/// Publishes corresponding notifications, but delegates all unit data/state to EnemyUnitManager.
/// </summary>
public class EnemyActionExecutor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private float unitHeightOffset = 2f;

    private EnemyUnitManager unitManager => EnemyUnitManager.Instance;

    private void OnEnable()
    {
        EventBus.Subscribe<EnemySpawnRequestEvent>(OnSpawnRequest);
        EventBus.Subscribe<EnemyMoveRequestEvent>(OnMoveRequest);
        EventBus.Subscribe<EnemyAttackRequestEvent>(OnAttackRequest);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EnemySpawnRequestEvent>(OnSpawnRequest);
        EventBus.Unsubscribe<EnemyMoveRequestEvent>(OnMoveRequest);
        EventBus.Unsubscribe<EnemyAttackRequestEvent>(OnAttackRequest);
    }

    #region Spawn
    private void OnSpawnRequest(EnemySpawnRequestEvent evt)
    {
        if (unitManager == null) 
            return;

        unitManager.IsSpawning = true;
        try
        {
            //Find prefab by unit name
            GameObject prefab = unitManager.unitPrefabs.Find(p => p.name == evt.UnitType);
            if (prefab == null)
            {
                Debug.LogWarning($"[EnemyActionExecutor] Prefab '{evt.UnitType}' not found.");
                return;
            }

            //Get spawn hex from base
            Vector2Int spawnHex = GetBaseSpawnHex(evt.BaseId);
            if (spawnHex == Vector2Int.zero)
            {
                Debug.LogWarning($"[EnemyActionExecutor] Base #{evt.BaseId} has invalid spawn location.");
                return;
            }

            Vector3 world = MapManager.Instance.HexToWorld(spawnHex);
            world.y += (unitHeightOffset + 0.5f);

            GameObject unitGO = Instantiate(prefab, world, Quaternion.identity);
            unitGO.name = $"Enemy_{evt.UnitType}_{unitManager.NextUnitId}";

            unitManager.RegisterUnit(unitGO, evt.BaseId, evt.UnitType, spawnHex);
        }
        finally
        {
            unitManager.IsSpawning = false;
        }
    }

    private Vector2Int GetBaseSpawnHex(int baseId)
    {
        var ebm = EnemyBaseManager.Instance;
        if (ebm == null) 
            return Vector2Int.zero;

        if (!ebm.Bases.TryGetValue(baseId, out var enemyBase) || enemyBase == null)
            return Vector2Int.zero;

        return enemyBase.currentTile != null ? enemyBase.currentTile.HexCoords : Vector2Int.zero;
    }
    #endregion

    #region Move
    private void OnMoveRequest(EnemyMoveRequestEvent evt)
    {
        if (unitManager == null || !unitManager.CanUnitMove(evt.UnitId))
            return;

        Vector2Int from = unitManager.GetUnitPosition(evt.UnitId);
        Vector2Int to = evt.Destination;

        //Use A* Path
        List<Vector2Int> path = AIPathFinder.GetPath(from, to);
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"[EnemyActionExecutor] No path for unit {evt.UnitId} from {from} to {to}");
            return;
        }

        int moveRange = unitManager.GetUnitMoveRange(evt.UnitId);
        List<Vector2Int> trimmedPath = path.GetRange(0, Mathf.Min(moveRange + 1, path.Count));
        //Final destination for this turn
        Vector2Int finalHex = trimmedPath[trimmedPath.Count - 1];

        //Stand check
        if (!MapManager.Instance.CanUnitStandHere(finalHex))
            return;
        //Blocked but not final goal tile
        if (MapManager.Instance.IsTileOccupied(finalHex) && finalHex != to)
            return;

        //Start moving visually
        unitManager.StartCoroutine(MoveUnitPath(evt.UnitId, trimmedPath, from, finalHex));
    }

    private IEnumerator MoveUnitPath(int unitId, List<Vector2Int> path, Vector2Int fromHex, Vector2Int finalHex)
    {
        GameObject go = unitManager.UnitObjects[unitId];

        //Release old tile immediately
        MapManager.Instance.SetUnitOccupied(fromHex, false);

        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int prevHex = path[i - 1];
            Vector2Int currHex = path[i];
            yield return SmoothMove(go, prevHex, currHex);
            yield return new WaitForSeconds(0.15f);
        }

        unitManager.UnitPositions[unitId] = finalHex;
        MapManager.Instance.SetUnitOccupied(finalHex, true);

        EventBus.Publish(new EnemyMovedEvent(unitId, fromHex, finalHex));
    }

    private IEnumerator SmoothMove(GameObject unit, Vector2Int startHex, Vector2Int endHex)
    {
        if (unit == null) 
            yield break;

        Vector3 startPos = MapManager.Instance.HexToWorld(startHex);
        startPos.y += unitHeightOffset;

        Vector3 endPos = MapManager.Instance.HexToWorld(endHex);
        endPos.y += unitHeightOffset;

        unit.transform.position = startPos;

        float t = 0f;
        float duration = 0.5f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            unit.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        unit.transform.position = endPos;

        //Test purpose
        //EnemyTracker.Instance.AddScore(20);
    }
    #endregion

    #region Attack
    private void OnAttackRequest(EnemyAttackRequestEvent evt)
    {
        if (unitManager == null) 
            return;

        int damage = unitManager.GetPlayerUnitAttackPower(evt.AttackerId);

        //Attack enemy unit (temp, for testing purpose)
        if (unitManager.UnitObjects.ContainsKey(evt.TargetId))
        {
            unitManager.TakeDamage(evt.TargetId, damage);
            EventBus.Publish(new EnemyAttackedEvent(evt.AttackerId, evt.TargetId));
            return;
        }

        //TODO: handle player units if PlayerUnitManager exists, eg:
        //if(PlayerUnitManager.UnitObjects.ContainsKey(evt.TargetId))
        //{
        //    PlayerUnitManager.TakeDamage(evt.TargetId, damage);
        //    EventBus.Publish(new EnemyAttackedEvent(evt.AttackerId, evt.TargetId));
        //    return;
        //}
        Debug.LogWarning($"[EnemyActionExecutor] Target {evt.TargetId} not found.");
    }
    #endregion
}
