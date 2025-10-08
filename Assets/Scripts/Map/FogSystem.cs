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
        foreach (var tile in MapManager.Instance.GetTiles())
        {
            tile.RemoveFog();
            tile.AddFog(fogPrefab);
        }
        // Reveal starting area
        RevealTilesAround(new Vector2Int(0, 0), visibleRadiusAtStart);
    }

    public void RevealTilesAround(Vector2Int center, int radius)
    {
        foreach (var (coord,tile) in MapManager.Instance.GetAllTiles())
        {
            int dist = HexCoordinates.Distance(center.x, center.y, coord.x, coord.y);
            if (dist <= radius)
            {
                tile.RemoveFog();
            }
        }
    }

    public void ResetFog()
    {
        foreach (var tile in MapManager.Instance.GetTiles())
        {
            tile.RemoveFog();
            tile.AddFog(fogPrefab);
        }
        RevealTilesAround(new Vector2Int(0, 0), visibleRadiusAtStart);
    }
}
