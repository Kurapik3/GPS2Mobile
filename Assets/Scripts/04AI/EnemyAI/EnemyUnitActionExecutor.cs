using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using static EnemyAIEvents;

/// <summary>
/// Handles executing enemy actions: spawn, move, attack.
/// Publishes corresponding notifications, but delegates all unit data/state to EnemyUnitManager.
/// </summary>
public class EnemyActionExecutor : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;

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
            ManagerAudio.instance.PlaySFX("UnitSpawn");
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

    private IEnumerator MoveUnitPath(int unitId, List<Vector2Int> path, Vector2Int fromHex, Vector2Int toHex)
    {
        GameObject go = unitManager.UnitObjects[unitId];

        if (!MapManager.Instance.CanUnitStandHere(toHex))
        {
            Debug.LogWarning($"[EnemyActionExecutor] Move Abort! Unit {unitId} can't move to {toHex}, which is not standable.");
            yield break;
        }

        //Register new tile immediately
        MapManager.Instance.SetUnitOccupied(toHex, true);

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
        unitManager.UnitPositions[unitId] = toHex;
        EnemyUnit unit = go.GetComponent<EnemyUnit>();
        if (unit != null)
        {
            HexTile finalTile = MapManager.Instance.GetTile(toHex);
            unit.UpdatePosition(finalTile);
        }

        EventBus.Publish(new EnemyMovedEvent(unitId, fromHex, toHex));
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

        float angle = GetRotationAngleTo(startPos, endPos);
        //Face movement direction on Y axis
        unitGO.transform.rotation = Quaternion.Euler(0f, angle, 0f);

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

    private float GetRotationAngleTo(Vector3 worldStart, Vector3 worldEnd)
    {
        Vector3 dir = worldEnd - worldStart;
        dir.y = 0; //Ignore vertical move

        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        return angle;
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
        if (attackerGO == null)
            return;

        bool isMelee = EnemyUnitManager.Instance.GetUnitType(evt.AttackerId) == "Tanker";

        int damage = unitManager.GetPlayerUnitAttackPower(evt.AttackerId);

        if (isMelee)
            StartCoroutine(MeleeAttackAnimation(attackerGO, evt.Target, damage, evt.AttackerId));
        else
            StartCoroutine(RangedAttackAnimation(attackerGO, evt.Target, damage, evt.AttackerId));
    }

    private IEnumerator MeleeAttackAnimation(GameObject attacker, GameObject target, int damage, int attackerId)
    {
        EnemyUnit attackerUnit = attacker.GetComponent<EnemyUnit>();
        Vector2Int attackerHex = attackerUnit.currentTile.HexCoords;
        Vector2Int targetHex = MapManager.Instance.WorldToHex(target.transform.position);

        Vector3 startPos = MapManager.Instance.HexToWorld(attackerHex);
        startPos.y += attackerUnit.baseHeightOffset;

        Vector3 targetPos = MapManager.Instance.HexToWorld(targetHex);
        targetPos.y = startPos.y;

        float dashDuration = 0.2f;

        //Dash to target position
        yield return LerpPosition(attacker.transform, startPos, targetPos, dashDuration);

        //Hit Target
        Knockback(target, attacker.transform.forward, () =>
        {
            ApplyDamage(target, damage, attackerId);
        });

        //Back to original hex 
        yield return LerpPosition(attacker.transform, targetPos, startPos, dashDuration);


        EventBus.Publish(new EnemyAttackedEvent(attackerId, target));
    }

    private IEnumerator RangedAttackAnimation(GameObject attacker, GameObject target, int damage, int attackerId)
    {
        EnemyUnit attackerUnit = attacker.GetComponent<EnemyUnit>();
        Vector2Int attackerHex = attackerUnit.currentTile.HexCoords;
        Vector2Int targetHex = MapManager.Instance.WorldToHex(target.transform.position);

        Vector3 start = MapManager.Instance.HexToWorld(attackerHex);
        start.y += attackerUnit.baseHeightOffset + 1.5f;

        Vector3 end = MapManager.Instance.HexToWorld(targetHex);
        end.y += 1f;

        GameObject projectile = Instantiate(projectilePrefab, start, Quaternion.identity);

        float duration = 1f;
        float time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime / duration;

            //Midpoint of the arc
            Vector3 mid = (start + end) / 2 + Vector3.up * 2.5f;

            Vector3 m1 = Vector3.Lerp(start, mid, time);
            Vector3 m2 = Vector3.Lerp(mid, end, time);
            projectile.transform.position = Vector3.Lerp(m1, m2, time);

            yield return null;
        }

        Destroy(projectile);

        Knockback(target, attacker.transform.forward, ()=>
        {
            ApplyDamage(target, damage, attackerId);
        });

        EventBus.Publish(new EnemyAttackedEvent(attackerId, target));
    }

    private IEnumerator LerpPosition(Transform t, Vector3 from, Vector3 to, float duration)
    {
        float time = 0f;
        Vector3 direction = to - from;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg; //Facing target direction
            t.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        while (time < 1f)
        {
            time += Time.deltaTime / duration;
            t.position = Vector3.Lerp(from, to, time);
            yield return null;
        }
    }

    public void Knockback(GameObject target, Vector3 direction, System.Action onComplete = null)
    {
        if (target == null)
            return;

        Vector3 hitPos = target.transform.position + direction.normalized * 0.3f;

        target.transform
        .DOMove(hitPos, 0.1f)
        .SetLoops(2, LoopType.Yoyo)
        .SetEase(Ease.OutQuad)
        .OnComplete(() =>
        {
            onComplete?.Invoke(); //Wait until animation completed then apply damage
        });
    }

    private void ApplyDamage(GameObject target,int damage, int attackerId)
    {
        var unit = target.GetComponent<UnitBase>();
        if (unit != null)
        {
            unit.TakeDamage(damage);
            if (EnemyUnitManager.Instance.GetUnitType(attackerId) == "Shooter")
            {
                ManagerAudio.instance.PlaySFX("ShooterShooting");
            }
            else if (EnemyUnitManager.Instance.GetUnitType(attackerId) == "Bomber")
            {
                ManagerAudio.instance.PlaySFX("BomberBombing");
            }

            if (unit.unitName == "Tanker")
            {
                var attackerGO = EnemyUnitManager.Instance.UnitObjects[attackerId];
                attackerGO.GetComponent<EnemyUnit>().TakeDamage(damage);
            }
            Debug.Log($"[EnemyActionExecutor] Dealt {damage} damage to PlayerUnit {unit.name}, HP now {unit.hp}");
            EventBus.Publish(new EnemyAttackedEvent(attackerId, unit.gameObject));
            return;
        }

        var tb = target.GetComponent<TreeBase>();
        if (tb != null)
        {
            tb.TakeDamage(damage);
            Debug.Log($"[EnemyActionExecutor] Dealt {damage} damage to TreeBase {tb.name}");
            EventBus.Publish(new EnemyAttackedEvent(attackerId, tb.gameObject));
            return;
        }

        var sm = target.GetComponent<SeaMonsterBase>();
        if (sm != null)
        {
            sm.TakeDamage(damage);
            Debug.Log($"[EnemyActionExecutor] Dealt {damage} damage to SeaMonster {sm.name}");
            EventBus.Publish(new EnemyAttackedEvent(attackerId, sm.gameObject));
            return;
        }

        Debug.LogWarning($"[EnemyActionExecutor] Target {target} not found.");
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
        ManagerAudio.instance.PlaySFX("BuilderBuilding");

        HexTile spawnTile = MapManager.Instance.GetTileAtHexPosition(evt.GrovePosition);
        spawnTile.SetContentsVisible(false);

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
