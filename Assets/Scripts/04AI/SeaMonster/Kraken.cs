using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SeaMonsterEvents;

public class Kraken : SeaMonsterBase
{
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

        StartCoroutine(AttackNearbyTargets());
    }

    private IEnumerator AttackNearbyTargets()
    {
        hasActedThisTurn = true;

        List<HexTile> tilesInRange = GetTilesInRange(CurrentTile, attackRange);

        foreach (HexTile tile in tilesInRange)
        {
            //Priority: Attack player and enemy unit
            if (tile.currentUnit != null)
            {
                Debug.Log($"[Kraken] Attacking unit: {tile.currentUnit.name} at {tile.HexCoords}");
                EventBus.Publish(new KrakenAttacksUnitEvent(this, tile.currentUnit));
                yield break;
            }

            //If no unit around: Attack another sea monster
            if (tile.HasDynamic && tile.dynamicInstance != null)
            {
                SeaMonsterBase other = tile.dynamicInstance.GetComponent<SeaMonsterBase>();
                if (other != null && other != this)
                {
                    Debug.Log($"[Kraken] Attacking monster: {other.MonsterName} at {tile.HexCoords}");
                    EventBus.Publish(new KrakenAttacksMonsterEvent(this, other));
                    yield break;
                }
            }
        }

        //If no target found
        Debug.Log("[Kraken] No targets found within range.");
        yield break;
    }

    private List<HexTile> GetTilesInRange(HexTile center, int range)
    {
        List<HexTile> result = new List<HexTile>();
        if (center == null) 
            return result;

        Queue<HexTile> frontier = new Queue<HexTile>();
        HashSet<HexTile> visited = new HashSet<HexTile>();

        frontier.Enqueue(center);
        visited.Add(center);

        for (int step = 0; step <= range; step++)
        {
            int count = frontier.Count;
            for (int i = 0; i < count; i++)
            {
                HexTile current = frontier.Dequeue();
                if (!result.Contains(current))
                    result.Add(current);

                foreach (var neighbor in current.neighbours)
                {
                    if (!visited.Contains(neighbor))
                    {
                        frontier.Enqueue(neighbor);
                        visited.Add(neighbor);
                    }
                }
            }
        }

        return result;
    }

    public override void TakeDamage(int dmg)
    {
        base.TakeDamage(dmg);
        // TODO: hit animation or sound(?)
    }

    protected override void Die()
    {
        base.Die();
    }
}
