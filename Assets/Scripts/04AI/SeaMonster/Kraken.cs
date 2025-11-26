using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SeaMonsterEvents;

public class Kraken : SeaMonsterBase
{
    private bool isTargeting = false;
    private GameObject currentTarget;
    private HexTile cachedNextMove = null;
    [SerializeField] private Animator anim;
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

    public override void OnPlayerClickTile(HexTile tile)
    {
        if (tile.currentEnemyUnit != null || tile.currentEnemyBase != null || tile.currentSeaMonster != null)
        {
            PerformAttackOnTile(tile);
        }
        else if (GetAvailableTiles().Contains(tile))
        {
            TryMove(tile);
        }
    }

    public override void PerformTurnAction()
    {
        if (hasActedThisTurn || currentTile == null)
            return;

        if (State == SeaMonsterState.Tamed)
            return;

        StartCoroutine(TurnRoutine());
    }

    public override HexTile GetNextMoveTile()
    {
        if (isTargeting && currentTarget != null)
            return null;

        if (State == SeaMonsterState.Tamed)
            return null;

        if (cachedNextMove != null)
            return cachedNextMove;

        cachedNextMove = AIPathFinder.GetRandomReachableTileForSeaMonster(this);
        return cachedNextMove;
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
        HexTile moveTile = GetNextMoveTile();
        if (moveTile != null && moveTile != currentTile)
        {
            MoveTo(moveTile);
            Debug.Log($"[Kraken] Moves to {moveTile.HexCoords}");
            cachedNextMove = null;
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
        var tilesInRange = GetTilesInRange(currentTile, attackRange);
        if (!tilesInRange.Exists(t => t.HexCoords == targetTile.HexCoords))
        {
            Debug.Log("[Kraken] Target moved out of range, stop targeting.");
            isTargeting = false;
            currentTarget = null;
            yield break;
        }

        Debug.Log($"[Kraken] Attacks {currentTarget.name}!");

        //Animation here
        //animator.SetTrigger();
        
        OnAttackHit(); //Need to remove after adding animation 
    }

    //Event for animator to call when playing the animation
    public void OnAttackHit()
    {
        if (currentTarget == null) 
            return;

        if (currentTarget.TryGetComponent(out UnitBase playerUnit))
            EventBus.Publish(new KrakenAttacksUnitEvent(this, playerUnit.gameObject, attack));
        else if (currentTarget.TryGetComponent(out EnemyUnit enemyUnit))
            EventBus.Publish(new KrakenAttacksUnitEvent(this, enemyUnit.gameObject, attack));
        else if (currentTarget.TryGetComponent(out SeaMonsterBase monster))
            EventBus.Publish(new KrakenAttacksMonsterEvent(this, monster, attack));
        else
            Debug.LogWarning("[Kraken] Unknown target type.");


        //Clear target after attacking
        cachedNextMove = null;
        isTargeting = false;
        currentTarget = null;
    }

    private List<GameObject> GetTargetsInRange()
    {
        List<GameObject> result = new List<GameObject>();
        List<HexTile> tiles = GetTilesInRange(currentTile, attackRange);

        foreach (HexTile tile in tiles)
        {
            //Player Unit
            if (tile.currentUnit != null)
            {
                if (TechTree.instance.IsCamouflage && tile.currentUnit.unitName == "Scout")
                    continue;
                result.Add(tile.currentUnit.gameObject);
            }

            //Enemy Unit
            if (tile.currentEnemyUnit != null)
                result.Add(tile.currentEnemyUnit.gameObject);

            //Other sea monster
            if (tile.currentSeaMonster != null && tile.currentSeaMonster != this)
                result.Add(tile.currentSeaMonster.gameObject);
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
        else if (target.TryGetComponent<SeaMonsterBase>(out var monster)) //Other Sea Monster
        {
            return monster.currentTile;
        }
        else if (target.TryGetComponent<EnemyUnit>(out var enemyUnit)) //Enemy
        {
            return enemyUnit.currentTile;
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
