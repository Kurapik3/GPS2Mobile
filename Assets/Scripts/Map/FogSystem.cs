using UnityEngine;
using System.Collections.Generic;
public class FogSystem : MonoBehaviour
{
    [SerializeField] private GameObject fogPrefab;
    [SerializeField] private int visibleRadiusAtStart = 2;
    //private Dictionary<Vector2Int, HexTile> allTiles;
    private void Start()
    {
        //allTiles = MapGenerator.AllTiles;
    }
    public void InitializeFog()
    {
        GenerateInitialFog();
    }
    private void GenerateInitialFog()
    {
        var allTiles = MapGenerator.AllTiles;
        foreach (var tile in allTiles.Values)
        {
            tile.RemoveFog();
        }
        foreach (var tile in allTiles.Values)
        {
            tile.AddFog(fogPrefab);
        }
        // Reveal starting area
        RevealTilesAround(new Vector2Int(0, 0), visibleRadiusAtStart);
    }

    public void RevealTilesAround(Vector2Int center, int radius)
    {
        var allTiles = MapGenerator.AllTiles;
        foreach (var k in allTiles)
        {
            Vector2Int coord = k.Key;
            HexTile tile = k.Value;
            int dist = HexCoordinates.Distance(center.x, center.y, coord.x, coord.y);
            if (dist <= radius)
            {
                tile.RemoveFog();
            }
        }
    }

    public void ResetFog()
    {
        var allTiles = MapGenerator.AllTiles;
        foreach (var tile in allTiles.Values)
        {
            tile.RemoveFog();
            tile.AddFog(fogPrefab);
        }
        RevealTilesAround(new Vector2Int(0, 0), visibleRadiusAtStart);
    }
}
