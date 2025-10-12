using UnityEngine;
using System.Collections.Generic;
public class FogSystem : MonoBehaviour
{
    [Header("Fog Settings")]
    [Tooltip("For Debug Purpose")]
    [SerializeField] private bool enableFog = true;
    [SerializeField] private GameObject fogPrefab;
    [SerializeField] private int visibleRadiusAtStart = 2;

    public List<Vector2Int> revealedTiles;


    public void InitializeFog()
    {
        if(!enableFog)
        {
            Debug.Log("[FogSystem] Fog disabled — skipping generation.");
            return;
        }
        GenerateInitialFog();
    }
    private void GenerateInitialFog()
    {
        if (!enableFog)
        {
            return;
        }
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
        if (!enableFog)
        {
            return;
        }
        foreach (var kv in MapManager.Instance.GetAllTiles())
        {
            Vector2Int coord = kv.Key;
            HexTile tile = kv.Value;
            int dist = HexCoordinates.Distance(center.x, center.y, coord.x, coord.y);
            if (dist <= radius)
            {
                tile.RemoveFog();
                //tracks revealed tiles
                if (!revealedTiles.Contains(coord))
                {
                    revealedTiles.Add(coord);
                }
            }
        }
    }


    //only when new game or restart
    public void ResetFog()
    {
        if (!enableFog)
        {
            Debug.Log("[FogSystem] Fog disabled — skipping reset.");
            return;
        }
        revealedTiles.Clear();
        foreach (var tile in MapManager.Instance.GetTiles())
        {
            tile.RemoveFog();
            tile.AddFog(fogPrefab);
        }
        RevealTilesAround(new Vector2Int(0, 0), visibleRadiusAtStart);
    }
}
