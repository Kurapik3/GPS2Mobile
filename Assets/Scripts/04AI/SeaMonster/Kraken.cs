using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Kraken : SeaMonsterBase
{
    [Header("Kraken Settings")]
    [SerializeField] private int attackCooldown = 1;

    public override void PerformTurnAction()
    {
        if (hasActedThisTurn || CurrentTile == null)
            return;

        StartCoroutine(AttackNearbyTargets());
    }

    private IEnumerator AttackNearbyTargets()
    {
        hasActedThisTurn = true;

        List<HexTile> tilesInRange = GetTilesInRange(CurrentTile, AttackRange);

        foreach (HexTile tile in tilesInRange)
        {
            //Attack player and enemy unit
            if (tile.currentUnit != null)
            {
                EventBus.Publish(new SeaMonsterEvents.KrakenAttacksUnitEvent(this, tile.currentUnit));
                yield break;
            }

            //Attack another sea monster
            if (tile.HasDynamic && tile.dynamicInstance != null)
            {
                SeaMonsterBase other = tile.dynamicInstance.GetComponent<SeaMonsterBase>();
                if (other != null && other != this)
                {
                    EventBus.Publish(new SeaMonsterEvents.KrakenAttacksMonsterEvent(this, other));
                    yield break;
                }
            }
        }

        //If no target found
        yield break;
    }

    private List<HexTile> GetTilesInRange(HexTile center, int range)
    {
        List<HexTile> result = new List<HexTile>();
        if (center == null) return result;

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
