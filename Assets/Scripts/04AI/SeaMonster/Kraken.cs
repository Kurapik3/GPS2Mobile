using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SeaMonsterEvents;

public class Kraken : SeaMonsterBase
{
    private bool isTargeting = false;
    private GameObject currentTarget;

    protected override void Awake()
    {
        base.Awake();

        monsterName = "Kraken";
        attack = 100;
        health = 20;
        killPoints = 2500;
        killAP = 20;
        movementRange = 1;
        attackRange = 1;
        isBlocking = false;
    }

    public override void PerformTurnAction()
    {
        if (hasActedThisTurn || CurrentTile == null)
            return;

        StartCoroutine(TurnRoutine());
    }

    private IEnumerator TurnRoutine()
    {
        Debug.Log("<color=blue>===== [Kraken]  Turn starts. =====</color>");

        hasActedThisTurn = true; //Mark early to avoid duplicate triggers

        //If already targeting, attack it
        if (isTargeting && currentTarget != null)
        {
            yield return AttackTarget();
            yield break;
        }

        //If no target, move to random reachable tile
        HexTile moveTile = AIPathFinder.GetRandomReachableTileForSeaMonster(this);
        if (moveTile != null && moveTile != CurrentTile)
        {
            MoveTo(moveTile);
            Debug.Log($"[Kraken] Moves to {moveTile.HexCoords}");
            yield return new WaitForSeconds(0.5f);
        }

        //After moving, check if there are any targets in range
        List<GameObject> targetsInRange = GetTargetsInRange();
        if (targetsInRange.Count > 0)
        {
            //Randomly choose target
            currentTarget = targetsInRange[Random.Range(0, targetsInRange.Count)];
            isTargeting = true;

            Debug.Log($"[Kraken] Chooses {currentTarget.name} as target!");
            EventBus.Publish(new KrakenTargetsUnitEvent(this, currentTarget)); //For UI to indicate sea monster target
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            Debug.Log("[Kraken] No targets found after moving.");
        }

        Debug.Log("<color=blue>===== [Kraken]  Turn ends. =====</color>");
        yield break;
    }

    private IEnumerator AttackTarget()
    {
        if (currentTarget == null)
        {
            Debug.LogWarning("[Kraken] Target disappeared before attack.");
            isTargeting = false;
            yield break;
        }

        //Where the target located at
        HexTile targetTile = GetTargetTile(currentTarget);
        if (targetTile == null)
        {
            Debug.LogWarning("[Kraken] Target tile not found, clearing target.");
            isTargeting = false;
            currentTarget = null;
            yield break;
        }

        //Check if the target tile in within kraken attack range
        var tilesInRange = GetTilesInRange(CurrentTile, attackRange);
        if (!tilesInRange.Exists(t => t.HexCoords == targetTile.HexCoords))
        {
            Debug.Log("[Kraken] Target moved out of range, stop targeting.");
            isTargeting = false;
            currentTarget = null;
            yield break;
        }

        Debug.Log($"[Kraken] Attacks {currentTarget.name}!");
        yield return new WaitForSeconds(0.5f);

        if (currentTarget.TryGetComponent(out UnitBase playerUnit))
        {
            EventBus.Publish(new KrakenAttacksUnitEvent(this, playerUnit.gameObject, attack));
        }
        else
        {
            int enemyId = -1;

            foreach (var kv in EnemyUnitManager.Instance.UnitObjects)
            {
                if (kv.Value == currentTarget)
                {
                    enemyId = kv.Key;
                    break;
                }
            }

            if (enemyId != -1)
            {
                EventBus.Publish(new KrakenAttacksUnitEvent(this, currentTarget, attack));
            }
            else if (currentTarget.TryGetComponent<SeaMonsterBase>(out var monster))
            {
                EventBus.Publish(new KrakenAttacksMonsterEvent(this, monster, attack));
            }
            else
            {
                Debug.LogWarning("[Kraken] Unknown target type.");
            }
        }

        //Clear target after attacking
        isTargeting = false;
        currentTarget = null;
    }

    private List<GameObject> GetTargetsInRange()
    {
        List<GameObject> result = new List<GameObject>();
        List<HexTile> tiles = GetTilesInRange(CurrentTile, attackRange);

        foreach (HexTile tile in tiles)
        {
            if (tile.currentUnit != null)
                result.Add(tile.currentUnit.gameObject);
            else if (tile.HasDynamic && tile.dynamicInstance != null)
            {
                SeaMonsterBase other = tile.dynamicInstance.GetComponent<SeaMonsterBase>();
                if (other != null && other != this)
                    result.Add(other.gameObject);
            }
        }

        return result;
    }

    private HexTile GetTargetTile(GameObject target)
    {
        if (target == null) 
            return null;

        //Player
        if (target.TryGetComponent<UnitBase>(out var playerUnit))
        {
            return playerUnit.currentTile;
        }

        //Other Sea Monster
        if (target.TryGetComponent<SeaMonsterBase>(out var monster))
        {
            return monster.CurrentTile;
        }

        //Enemy
        foreach (var kv in EnemyUnitManager.Instance.UnitObjects)
        {
            if (kv.Value == target)
            {
                Vector2Int hexPos = EnemyUnitManager.Instance.GetUnitPosition(kv.Key);
                return MapManager.Instance.GetTileAtHexPosition(hexPos);
            }
        }

        //Fallback
        return target.GetComponentInParent<HexTile>();
    }

    private List<HexTile> GetTilesInRange(HexTile center, int range)
    {
        var result = new List<HexTile>();
        if (center == null) return result;

        var frontier = new Queue<(HexTile tile, int dist)>();
        var visited = new HashSet<HexTile>();

        frontier.Enqueue((center, 0));
        visited.Add(center);

        while (frontier.Count > 0)
        {
            var (current, dist) = frontier.Dequeue();
            result.Add(current);

            if (dist >= range)
                continue;

            foreach (var neighbor in current.neighbours)
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    frontier.Enqueue((neighbor, dist + 1));
                }
            }
        }

        return result;
    }

    public override void TakeDamage(int dmg)
    {
        base.TakeDamage(dmg);
    }

    protected override void Die()
    {
        base.Die();
    }
}
