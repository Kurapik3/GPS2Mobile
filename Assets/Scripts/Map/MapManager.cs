using UnityEngine;
using System.Collections.Generic;

// Central hub for all map-related queries during gameplay.
// Attach this to a GameObject in your scene (only one instance allowed).

// USE THIS INSTEAD OF MapGenerator.AllTiles!

// Common use cases:
// - Pathfinding: check if a tile is walkable.
// - AI/Units: find nearby tiles or get tile at position.
// - Fog of War: iterate over all tiles to hide/reveal.
[DefaultExecutionOrder(-50)]
public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    private Dictionary<Vector2Int, HexTile> _tiles = new();


    // Returns all HexTile objects (no coordinates). Use when you only care about the tile itself.
    // Example: "Add fog to every tile"
    public IEnumerable<HexTile> GetTiles() => _tiles.Values;   //Gets the tile objects without coords


    // Returns all tiles with their (q, r) coordinates. Use when you need to know WHERE a tile is.
    // Example: "Reveal tiles within radius of a unit"
    public IReadOnlyDictionary<Vector2Int, HexTile> GetAllTiles() => _tiles; //readonly tiles with coords
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Called by MapGenerator after building the grid, dont need to call this
    public void RegisterTiles(Dictionary<Vector2Int, HexTile> tiles)
    {
        _tiles = tiles;
    }


    // Get the HexTile at axial coordinates (q, r).
    // Returns null if no tile exists at that position (e.g., outside map bounds).

    // Example:
    //   HexTile target = MapManager.Instance.GetTile(2, -1);
    //   if (target != null) { ... }
    public HexTile GetTile(int q, int r) => GetTile(new Vector2Int(q, r));
    public HexTile GetTile(Vector2Int coord)
    {
        _tiles.TryGetValue(coord, out HexTile tile);
        return tile;
    }


    // Safely try to get a tile, use this if you're not sure whether the coordinate is valid
    // Example:
    //   if (MapManager.Instance.TryGetTile(new Vector2Int(5, 3), out HexTile tile))
    //   {
    //       // tile is valid — do something
    //   }
    public bool TryGetTile(Vector2Int coord, out HexTile tile)
    {
        return _tiles.TryGetValue(coord, out tile);
    }

    // Check if a tile exists AND is walkable
    // Example:
    //   if (MapManager.Instance.IsWalkable(new Vector2Int(1, 0)))
    //   {
    //       unit.MoveTo(1, 0);
    //   }

    public void SetUnitOccupied(Vector2Int coord, bool occupied)
    {
        if (_tiles.TryGetValue(coord, out var tile))
        {
            tile.SetOccupiedByUnit(occupied);
        }
    }

    public bool CanUnitStandHere(Vector2Int coord)
    {
        if (_tiles.TryGetValue(coord, out var tile))
            return tile.CanUnitStandHere();
        return false;
    }

    public bool IsTileOccupied(Vector2Int coord)
    {
        if (_tiles.TryGetValue(coord, out var tile))
        {
            return tile.IsOccupiedByUnit;
        }
        return false;
    }

    public bool IsWalkable(Vector2Int coord)
    {
        return _tiles.TryGetValue(coord, out var tile) && tile.IsWalkableForAI();
    }

    //to get nearby tiles
    public List<HexTile> GetNeighborsWithinRadius(int q, int r, int radius)
    {
        List<HexTile> result = new();
        for (int dq = -radius; dq <= radius; dq++)
        {
            for (int dr = -radius; dr <= radius; dr++)
            {
                int newQ = q + dq;
                int newR = r + dr;

                // skip tiles beyond the true hex radius
                if (HexCoordinates.Distance(q, r, newQ, newR) > radius)
                {
                    continue;
                }
                Vector2Int key = new(newQ, newR);
                if (_tiles.TryGetValue(key, out HexTile tile))
                {
                    result.Add(tile);
                }
            }
        }
        return result;
    }

    public void Clear()
    {
        _tiles.Clear();
    }
}