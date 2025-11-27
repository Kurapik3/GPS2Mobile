using System;
using System.Collections.Generic;
using UnityEngine;

public class TurfManager : MonoBehaviour
{
    public static TurfManager Instance;
    public static event Action OnTurfChanged;
    private HashSet<HexTile> turfTiles = new HashSet<HexTile>();

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


    public void AddTurfArea(HexTile centerTile, int radius)
    {
        if (centerTile == null)
        {
            Debug.LogError("AddTurfArea: centerTile is null!");
            return;
        }

        var tiles = MapManager.Instance.GetNeighborsWithinRadius(centerTile.q, centerTile.r, radius);
        tiles.Add(centerTile); // also add the center tile

        foreach (var t in tiles)
        {
            if (!MapManager.Instance.IsTileClaimed(t.HexCoords))
            {
                turfTiles.Add(t);
                t.SetTurf(true);  // mark the tile as part of turf
            }
            else
            {
                Debug.Log($"Tile ({t.q},{t.r}) skipped, already claimed.");
            }
        }

        OnTurfChanged?.Invoke();
        Debug.Log($"Turf claimed at center ({centerTile.q},{centerTile.r}) with radius {radius}");
    }

    public bool IsInsideTurf(HexTile tile)
    {
        return tile != null && turfTiles.Contains(tile);
    }

    public void ClearTurf()
    {
        foreach (var tile in turfTiles)
        {
            tile.SetTurf(false);
        }
        turfTiles.Clear();
        OnTurfChanged?.Invoke();
    }

    //Added By Ashley to access Turf Tiles
    public IEnumerable<HexTile> GetAllTurfTiles()
    {
        return turfTiles;
    }

}
