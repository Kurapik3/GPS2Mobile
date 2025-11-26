using System.Collections.Generic;
using UnityEngine;
using static SeaMonsterEvents;

/// <summary>
/// Defensive sea monster that blocks tiles but can move slowly.
/// </summary>
public class TurtleWall : SeaMonsterBase
{
    private Vector2Int? blockedBackTile = null;
    private HexTile cachedNextMove = null;
    private Vector2Int lastMoveDirection = new Vector2Int(0, 1);

    [SerializeField] private GameObject blockIndicatorPrefab;
    private List<(Vector2Int tileCoord, GameObject indicator)> blockedIndicators = new List<(Vector2Int, GameObject)>();

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

        //Block self
        BlockTile(spawnTile.HexCoords);
        isBlocking = true;

        //Block back tile
        Vector2Int backCoord = spawnTile.HexCoords - lastMoveDirection;
        if (MapManager.Instance.IsWalkable(backCoord)) //Only block if it's a valid walkable tile
        {
            BlockTile(backCoord);
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
        if (hasActedThisTurn || currentTile == null || State == SeaMonsterState.Tamed)
            return;

        HexTile target = GetNextMoveTile(); 
        if (target != null && target != currentTile)
        {
            MoveTo(target);
            cachedNextMove = null;
        }

        hasActedThisTurn = true;
    }

    protected override void MoveTo(HexTile target)
    {
        //Unblock first
        if (currentTile != null)
        {
            UnblockTile(currentTile.HexCoords);
            if (blockedBackTile.HasValue)
                UnblockTile(blockedBackTile.Value);
        }

        lastMoveDirection = target.HexCoords - currentTile.HexCoords;

        //Move
        base.MoveTo(target);

        //Block at the new position
        BlockTile(target.HexCoords);
        isBlocking = true;

        Vector2Int newBack = target.HexCoords - NormalizeHexDirection(lastMoveDirection);
        if (MapManager.Instance.IsWalkable(newBack))
        {
            BlockTile(newBack);
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
            UnblockTile(currentTile.HexCoords);
            isBlocking = false;
        }

        //Unblock back tile
        if (blockedBackTile.HasValue)
            UnblockTile(blockedBackTile.Value);

        base.Die();
    }

    private void BlockTile(Vector2Int coord)
    {
        EventBus.Publish(new TurtleWallBlockEvent(this, coord));
        ShowBlockIndicator(coord);

        HexTile tile = MapManager.Instance.GetTileAtHexPosition(coord);
        if (tile != null) 
            tile.SetBlockedByTurtleWall(true);
    }

    private void UnblockTile(Vector2Int coord)
    {
        EventBus.Publish(new TurtleWallUnblockEvent(this, coord));
        RemoveBlockIndicator(coord);

        HexTile tile = MapManager.Instance.GetTileAtHexPosition(coord);
        if (tile != null) 
            tile.SetBlockedByTurtleWall(false);
    }

    private Vector2Int NormalizeHexDirection(Vector2Int dir)
    {
        Vector2Int bestDir = HexCoordinates.Directions[0];
        int bestDot = int.MinValue;
        foreach (var d in HexCoordinates.Directions)
        {
            int dot = dir.x * d.x + dir.y * d.y;
            if (dot > bestDot)
            {
                bestDot = dot;
                bestDir = d;
            }
        }
        return bestDir;
    }

    private void ShowBlockIndicator(Vector2Int tileCoord)
    {
        if (blockedIndicators.Exists(x => x.tileCoord == tileCoord))
            return;

        HexTile tile = MapManager.Instance.GetTileAtHexPosition(tileCoord);
        if (tile != null && blockIndicatorPrefab != null)
        {
            Vector3 spawnPos = new Vector3(tile.transform.position.x, 2.0f, tile.transform.position.z);
            GameObject indicator = Instantiate(blockIndicatorPrefab, spawnPos, Quaternion.Euler(90f, 0f, 0f));
            indicator.transform.SetParent(tile.transform);
            blockedIndicators.Add((tileCoord, indicator));
        }
    }

    private void RemoveBlockIndicator(Vector2Int tileCoord)
    {
        var item = blockedIndicators.Find(x => x.tileCoord == tileCoord);
        if (item.indicator != null)
            Destroy(item.indicator);

        blockedIndicators.Remove(item);
    }
}
