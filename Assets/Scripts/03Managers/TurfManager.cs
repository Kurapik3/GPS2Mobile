using System.Collections.Generic;
using UnityEngine;

public class TurfManager : MonoBehaviour
{
    public static TurfManager Instance;

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
        var tiles = MapManager.Instance.GetNeighborsWithinRadius(centerTile.q, centerTile.r, radius);

        foreach (var t in tiles)
        {
            turfTiles.Add(t);
            t.SetTurf(true);  // mark the tile as part of turf
        }

        // also add the center tile
        turfTiles.Add(centerTile);
        centerTile.SetTurf(true);

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
    }

    //Added By Ashley to access Turf Tiles
    public IEnumerable<HexTile> GetAllTurfTiles()
    {
        return turfTiles;
    }

}
