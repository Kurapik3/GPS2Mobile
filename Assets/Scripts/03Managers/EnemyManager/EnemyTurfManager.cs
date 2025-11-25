using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTurfManager : MonoBehaviour
{
    public static EnemyTurfManager Instance { get; private set; }
    private Dictionary<Vector2Int, EnemyBase> turfTileMap = new();
    public static event Action OnEnemyTurfChanged;

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterBaseArea(Vector2Int currentTile, int radius, EnemyBase baseRef)
    {
        var tiles = MapManager.Instance.GetNeighborsWithinRadius(currentTile.x, currentTile.y, radius);
        foreach (var tile in tiles)
        {
            if (!MapManager.Instance.IsTileClaimed(tile.HexCoords)) //Only register when the tile is empty to prevent turf overlapping
                turfTileMap[tile.HexCoords] = baseRef;
        }

        OnEnemyTurfChanged?.Invoke();
    }

    public void UnregisterBaseArea(Vector2Int currentTile, int radius)
    {
        var tiles = MapManager.Instance.GetNeighborsWithinRadius(currentTile.x, currentTile.y, radius);
        foreach (var tile in tiles)
        {
            turfTileMap.Remove(tile.HexCoords);
        }
        OnEnemyTurfChanged?.Invoke();
    }

    public EnemyBase GetBaseByTile(Vector2Int hex)
    {
        turfTileMap.TryGetValue(hex, out var baseRef);
        return baseRef;
    }

    public bool IsInTurf(Vector2Int hex) => turfTileMap.ContainsKey(hex);

    public IEnumerable<Vector2Int> GetAllEnemyTurfCoords()
    {
        return turfTileMap.Keys;
    }
}
