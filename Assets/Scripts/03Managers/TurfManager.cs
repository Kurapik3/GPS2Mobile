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


    public List<HexTile> AddTurfArea(HexTile centerTile, int radius)
    {
        List<HexTile> claimedTiles = new List<HexTile>();

        if (centerTile == null)
        {
            Debug.LogError("AddTurfArea: centerTile is null!");
            return claimedTiles;
        }

        var tiles = MapManager.Instance.GetNeighborsWithinRadius(centerTile.q, centerTile.r, radius);
        tiles.Add(centerTile);

        foreach (var t in tiles)
        {
            if (!MapManager.Instance.IsTileClaimed(t.HexCoords))
            {
                turfTiles.Add(t);
                t.SetTurf(true);
                claimedTiles.Add(t);   
            }
        }

        OnTurfChanged?.Invoke();
        return claimedTiles;
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

    public void RemoveTurfArea(List<HexTile> tiles)
    {
        foreach (var t in tiles)
        {
            t.SetTurf(false);
            turfTiles.Remove(t);
        }

        OnTurfChanged?.Invoke();
    }

}
