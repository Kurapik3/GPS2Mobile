using UnityEngine;
using static SeaMonsterEvents;

/// <summary>
/// Defensive sea monster that blocks tiles but can move slowly.
/// </summary>
public class TurtleWall : SeaMonsterBase
{
    private Vector2Int? blockedBackTile = null;

    protected override void Awake()
    {
        base.Awake();
        monsterName = "Turtle Wall";
        attack = 0;
        health = 35;
        killPoints = 2000;
        killAP = 20;
        movementRange = 1;
        attackRange = 0;
        isBlocking = true;
    }

    public override void Initialize(HexTile spawnTile)
    {
        base.Initialize(spawnTile);

        //Block self (spawn tile)
        EventBus.Publish(new TurtleWallBlockEvent(this, spawnTile.HexCoords));

        //Block back tile
        Vector2Int backCoord = spawnTile.HexCoords + GetBackDirection();
        if (MapManager.Instance.IsWalkable(backCoord)) //Only block if it's a valid walkable tile
        {
            EventBus.Publish(new TurtleWallBlockEvent(this, backCoord));
            blockedBackTile = backCoord;
            Debug.Log($"[TurtleWall] Also blocking back tile at {backCoord}");
        }

        Debug.Log($"[TurtleWall] Spawned at {spawnTile.HexCoords}. Blocking self + back.");
    }

    public override void PerformTurnAction()
    {
        if (hasActedThisTurn || CurrentTile == null)
            return;

        HexTile target = AIPathFinder.GetRandomReachableTileForSeaMonster(this); 
        if (target != null && target != CurrentTile)
        {
            MoveTo(target);
            Debug.Log($"[TurtleWall] Moves to {target.HexCoords}");
        }
        else
        {
            Debug.Log("[TurtleWall] No valid move found or chooses to stay.");
        }

        hasActedThisTurn = true;
    }

    protected override void MoveTo(HexTile target)
    {
        //Unblock first
        if (CurrentTile != null)
        {
            EventBus.Publish(new TurtleWallUnblockEvent(this, CurrentTile.HexCoords));
            if (blockedBackTile.HasValue)
                EventBus.Publish(new TurtleWallUnblockEvent(this, blockedBackTile.Value));
        }

        //Move
        base.MoveTo(target);

        //Block at the new position
        EventBus.Publish(new TurtleWallBlockEvent(this, target.HexCoords));
        Vector2Int newBack = target.HexCoords + GetBackDirection();
        if (MapManager.Instance.IsWalkable(newBack))
        {
            EventBus.Publish(new TurtleWallBlockEvent(this, newBack));
            blockedBackTile = newBack;
        }
        else
        {
            blockedBackTile = null;
        }
    }

    protected override void Die()
    {
        //Unblock self
        if (isBlocking && CurrentTile != null)
        {
            EventBus.Publish(new TurtleWallUnblockEvent(this, CurrentTile.HexCoords));
            isBlocking = false;
        }

        //Unblock back tile
        if (blockedBackTile.HasValue)
        {
            EventBus.Publish(new TurtleWallUnblockEvent(this, blockedBackTile.Value));
            blockedBackTile = null;
        }

        base.Die();
    }

    private Vector2Int GetBackDirection()
    {
        return new Vector2Int(0, 1);
    }
}
