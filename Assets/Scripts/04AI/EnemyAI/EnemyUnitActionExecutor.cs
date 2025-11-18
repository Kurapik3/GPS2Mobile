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
    private EnemyUnitManager unitManager => EnemyUnitManager.Instance;

    private void OnEnable()
    {
        EventBus.Subscribe<EnemySpawnRequestEvent>(OnSpawnRequest);
        EventBus.Subscribe<EnemyMoveRequestEvent>(OnMoveRequest);
        EventBus.Subscribe<EnemyAttackRequestEvent>(OnAttackRequest);
        EventBus.Subscribe<BuilderDevelopGroveEvent>(OnBuilderDevelopGrove);
        EventBus.Subscribe<EnemyAuxiliaryActionRequestEvent>(OnAuxiliaryActionRequested);
    }


    private void OnDisable()
    {
        EventBus.Unsubscribe<EnemySpawnRequestEvent>(OnSpawnRequest);
        EventBus.Unsubscribe<EnemyMoveRequestEvent>(OnMoveRequest);
        EventBus.Unsubscribe<EnemyAttackRequestEvent>(OnAttackRequest);
        EventBus.Unsubscribe<BuilderDevelopGroveEvent>(OnBuilderDevelopGrove);
        EventBus.Unsubscribe<EnemyAuxiliaryActionRequestEvent>(OnAuxiliaryActionRequested);
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
            world.y += 2.5f;

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
        Vector2Int finalHex = trimmedPath[^1];

        //Start moving visually
        unitManager.StartCoroutine(MoveUnitPath(evt.UnitId, trimmedPath, from, finalHex));
    }

    private IEnumerator MoveUnitPath(int unitId, List<Vector2Int> path, Vector2Int fromHex, Vector2Int finalHex)
    {
        GameObject go = unitManager.UnitObjects[unitId];

        if (!MapManager.Instance.CanUnitStandHere(finalHex))
        {
            Debug.LogWarning($"[EnemyActionExecutor] Move Abort! Unit {unitId} can't move to {finalHex}, which is not standable.");
            yield break;
        }

        //Register new tile immediately
        MapManager.Instance.SetUnitOccupied(finalHex, true);

        //Release old tile
        MapManager.Instance.SetUnitOccupied(fromHex, false);

        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int prevHex = path[i - 1];
            Vector2Int currHex = path[i];
            yield return SmoothMove(go, prevHex, currHex);
            yield return new WaitForSeconds(0.2f);
        }

        //Update Position
        unitManager.UnitPositions[unitId] = finalHex;
        EnemyUnit unit = go.GetComponent<EnemyUnit>();
        if (unit != null)
        {
            HexTile finalTile = MapManager.Instance.GetTile(finalHex);
            unit.UpdatePosition(finalTile);
        }

        EventBus.Publish(new EnemyMovedEvent(unitId, fromHex, finalHex));
    }

    private IEnumerator SmoothMove(GameObject unitGO, Vector2Int startHex, Vector2Int endHex)
    {
        if (unitGO == null) 
            yield break;

        EnemyUnit unit = unitGO.GetComponent<EnemyUnit>();
        if (unit == null) 
            yield break;

        Vector3 startPos = MapManager.Instance.HexToWorld(startHex);
        startPos.y += unit.baseHeightOffset;

        Vector3 endPos = MapManager.Instance.HexToWorld(endHex);
        endPos.y += unit.baseHeightOffset;

        unitGO.transform.position = startPos;

        float t = 0f;
        float duration = 0.5f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            unitGO.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        unit.UpdatePosition(MapManager.Instance.GetTile(endHex));
    }
    #endregion

    #region Attack
    private void OnAttackRequest(EnemyAttackRequestEvent evt)
    {
        if (evt.Target == null)
        {
            Debug.LogWarning($"[EnemyActionExecutor] Target GameObject missing for attacker {evt.AttackerId}!");
            return;
        }

        Debug.Log($"[EnemyActionExecutor] Attacker {evt.AttackerId} attacking target {evt.Target.name}");

        var attackerGO = unitManager.UnitObjects[evt.AttackerId];
        if (attackerGO != null)
        {
            var ar = attackerGO.GetComponentInChildren<Renderer>();
            if (ar != null)
            {
                Color aOriginal = ar.material.color;
                ar.material.color = Color.yellow;
                StartCoroutine(RestoreColor(ar, aOriginal, 0.2f));
            }
        }

        int damage = unitManager.GetPlayerUnitAttackPower(evt.AttackerId);

        var unit = evt.Target.GetComponent<UnitBase>();
        if (unit != null)
        {
            unit.TakeDamage(damage);
            if(unit.unitName == "Tanker")
            {
                attackerGO.GetComponent<EnemyUnit>().TakeDamage(damage);
            }
            Debug.Log($"[EnemyActionExecutor] Dealt {damage} damage to PlayerUnit {unit.name}, HP now {unit.hp}");
            EventBus.Publish(new EnemyAttackedEvent(evt.AttackerId, unit.gameObject));
            return;
        }

        var tb = evt.Target.GetComponent<TreeBase>();
        if (tb != null)
        {
            tb.TakeDamage(damage);
            Debug.Log($"[EnemyActionExecutor] Dealt {damage} damage to TreeBase {tb.name}");
            EventBus.Publish(new EnemyAttackedEvent(evt.AttackerId, tb.gameObject));
            return;
        }

        var sm = evt.Target.GetComponent<SeaMonsterBase>();
        if (sm != null)
        {
            sm.TakeDamage(damage);
            Debug.Log($"[EnemyActionExecutor] Dealt {damage} damage to SeaMonster {sm.name}");
            EventBus.Publish(new EnemyAttackedEvent(evt.AttackerId, sm.gameObject));
            return;
        }

        //Temp, for testing
        Renderer r = evt.Target.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            Color original = r.material.color;
            r.material.color = Color.red;
            StartCoroutine(RestoreColor(r, original, 0.2f));
        }

        Debug.LogWarning($"[EnemyActionExecutor] Target {evt.Target} not found.");
    }

    //Temp, for testing
    private IEnumerator RestoreColor(Renderer r, Color original, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (r != null)
            r.material.color = original;
    }
    #endregion

    #region Builder
    private void OnBuilderDevelopGrove(BuilderDevelopGroveEvent evt)
    {
        //Get Tile Position
        HexTile tile = MapManager.Instance.GetTile(evt.GrovePosition);
        if (tile == null || tile.currentBuilding == null)
        {
            Debug.LogWarning($"[EnemyActionExecutor] Target tile {evt.GrovePosition} invalid for building.");
            return;
        }

        GroveBase grove = tile.currentBuilding.GetComponent<GroveBase>();
        if (grove == null)
        {
            Debug.LogWarning($"[EnemyActionExecutor] No GroveBase at {evt.GrovePosition}");
            return;
        }

        //Store grove level
        int baseLevel = grove.GetFormerLevel();

        //Remove the grove
        Destroy(grove.gameObject);
        tile.currentBuilding = null;

        //Instantiate Enemy Base at the same tile
        if (EnemyBaseManager.Instance == null || EnemyBaseManager.Instance.basePrefab == null)
        {
            Debug.LogWarning("[EnemyActionExecutor] EnemyBase prefab missing!");
            return;
        }

        Vector3 spawnPos = MapManager.Instance.HexToWorld(evt.GrovePosition);
        spawnPos.y += 2f;
        GameObject newEnemyBaseObj = Instantiate(EnemyBaseManager.Instance.basePrefab, spawnPos, Quaternion.identity);

        EnemyBase newEnemyBase = newEnemyBaseObj.GetComponent<EnemyBase>();
        if (newEnemyBase != null)
        {
            newEnemyBase.level = baseLevel;
            newEnemyBase.UpdateModel();
            Debug.Log($"[EnemyActionExecutor] Restored EnemyBase with level {baseLevel}");
        }

        //Successfully build a base, add score to enemy
        EnemyTracker.Instance.AddScore(1000);

        Debug.Log($"[EnemyActionExecutor] Builder {evt.UnitId} developed Grove into Enemy Base at {evt.GrovePosition}");
    }
    #endregion

    #region AuxiliaryActions
    private void OnAuxiliaryActionRequested(EnemyAuxiliaryActionRequestEvent evt)
    {
        switch (evt.ActionId)
        {
            case 3: //Develop tile
                ExecuteDevelopTile(evt.UnitId, evt.TargetPos);
                break;

            case 6: //Unlock tech
                ExecuteUnlockTech();
                break;
        }
    }

    private void ExecuteDevelopTile(int unitId, Vector2Int pos)
    {
        HexTile tile = MapManager.Instance.GetTile(pos);

        if (tile == null)
        {
            EventBus.Publish(new EnemyAuxiliaryActionExecutedEvent(3, false));
            return;
        }

        bool success = false;
        int popToAdd = 0;
        // Fish
        if (tile.fishTile != null)
        {
            Destroy(tile.fishTile.gameObject);
            tile.fishTile = null;
            success = true;
            popToAdd = 1;
        }
        // Debris
        else if (tile.debrisTile != null)
        {
            Destroy(tile.debrisTile.gameObject);
            tile.debrisTile = null;
            success = true;
            popToAdd = 2;
        }

        EventBus.Publish(new EnemyAuxiliaryActionExecutedEvent(3, success));

        if (success)
        {
            EnemyUnitManager.Instance.MarkUnitAsActed(unitId);
            EnemyTracker.Instance?.AddScore(200);

            EnemyBase baseRef = EnemyTurfManager.Instance.GetBaseByTile(pos);
            if (baseRef != null)
            {
                baseRef.AddPopulation(popToAdd);
                Debug.Log($"[EnemyActionExecutor] Added {popToAdd} population to base {baseRef.baseName}");
            }
        }
    }

    private void ExecuteUnlockTech()
    {
        Debug.Log("[EnemyActionExecutor] Unlocked tech");
        EventBus.Publish(new EnemyAuxiliaryActionExecutedEvent(6, true));
        EnemyTracker.Instance?.AddScore(500);
    }
    #endregion
}
