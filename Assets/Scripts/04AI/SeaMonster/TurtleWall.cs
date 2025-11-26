using UnityEngine;
using static SeaMonsterEvents;

/// <summary>
/// Defensive sea monster that blocks tiles but can move slowly.
/// </summary>
public class TurtleWall : SeaMonsterBase
{
    private Vector2Int? blockedBackTile = null;
    private HexTile cachedNextMove = null;

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

    public override HexTile GetNextMoveTile()
    {
        if (State == SeaMonsterState.Tamed)
            return null;

        if (cachedNextMove != null)
            return cachedNextMove;

        cachedNextMove = AIPathFinder.GetRandomReachableTileForSeaMonster(this);
        return cachedNextMove;
    }

    public override void OnPlayerClickTile(HexTile tile)
    {
        if (GetAvailableTiles().Contains(tile))
            TryMove(tile);
        else
            Debug.Log("TurtleWall can't attack!");
    }

    public override void PerformTurnAction()
    {
        if (hasActedThisTurn || currentTile == null)
            return;

        if (State == SeaMonsterState.Tamed)
            return;

        HexTile target = GetNextMoveTile(); 
        if (target != null && target != currentTile)
        {
            MoveTo(target);
            Debug.Log($"[TurtleWall] Moves to {target.HexCoords}");

            cachedNextMove = null;
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
        if (currentTile != null)
        {
            EventBus.Publish(new TurtleWallUnblockEvent(this, currentTile.HexCoords));
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
        if (isBlocking && currentTile != null)
        {
            EventBus.Publish(new TurtleWallUnblockEvent(this, currentTile.HexCoords));
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
