using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Kraken : SeaMonsterBase
{
    [Header("Kraken Settings")]
    public int AttackCooldown = 1; //Number of attacks per turn
    private bool hasActedThisTurn = false;

    private void OnEnable()
    {
        EventBus.Subscribe<SeaMonsterEvents.TurnStartedEvent>(OnTurnStarted);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<SeaMonsterEvents.TurnStartedEvent>(OnTurnStarted);
    }

    private void OnTurnStarted(SeaMonsterEvents.TurnStartedEvent evt)
    {
        CurrentTurn = evt.Turn;
        hasActedThisTurn = false;

        StartCoroutine(PerformTurn());
    }

    private IEnumerator PerformTurn()
    {
        if (hasActedThisTurn) 
            yield break;

        if (CurrentTile == null)
        {
            hasActedThisTurn = true;
            yield break;
        }

        //Get all tiles in attack range
        List<HexTile> tilesInRange = GetTilesInRange(CurrentTile, AttackRange);

        foreach (HexTile tile in tilesInRange)
        {
            // Attack player or enemy
            if (tile.currentUnit != null)
            {
                EventBus.Publish(new SeaMonsterEvents.SeaMonsterAttacksUnitEvent(this, tile.currentUnit));
                hasActedThisTurn = true;
                yield break;
            }

            // Attack other sea monsters
            if (tile.HasDynamic && tile.dynamicInstance != null)
            {
                SeaMonsterBase otherMonster = tile.dynamicInstance.GetComponent<SeaMonsterBase>();
                if (otherMonster != null && otherMonster != this)
                {
                    EventBus.Publish(new SeaMonsterEvents.SeaMonsterAttacksMonsterEvent(this, otherMonster));
                    hasActedThisTurn = true;
                    yield break;
                }
            }
        }

        hasActedThisTurn = true;
        yield break;
    }

    private List<HexTile> GetTilesInRange(HexTile centerTile, int range)
    {
        List<HexTile> result = new List<HexTile>();
        if (centerTile == null) 
            return result;

        Queue<HexTile> frontier = new Queue<HexTile>();
        HashSet<HexTile> visited = new HashSet<HexTile>();

        frontier.Enqueue(centerTile);
        visited.Add(centerTile);

        for (int i = 0; i <= range; i++)
        {
            int count = frontier.Count;
            for (int j = 0; j < count; j++)
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
        //Apply damage to the Kraken
        base.TakeDamage(dmg);

        //Future: add hit effects or sound here
    }

    protected override void Die()
    {
        Vector2Int pos = CurrentTile != null ? CurrentTile.HexCoords : Vector2Int.zero;
        EventBus.Publish(new SeaMonsterEvents.SeaMonsterKilledEvent(this, pos));
        Destroy(gameObject);
    }
}
