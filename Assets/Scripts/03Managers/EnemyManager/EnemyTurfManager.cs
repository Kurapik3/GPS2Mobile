using System.Collections.Generic;
using UnityEngine;

public class EnemyTurfManager : MonoBehaviour
{
    public static EnemyTurfManager Instance { get; private set; }
    private HashSet<Vector2Int> turfTiles = new();

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterBaseArea(Vector2Int currentTile, int radius)
    {
        var tiles = MapManager.Instance.GetNeighborsWithinRadius(currentTile.x, currentTile.y, radius);
        foreach (var tile in tiles)
        {
            turfTiles.Add(tile.HexCoords);
        }
    }

    public void UnregisterBaseArea(Vector2Int currentTile, int radius)
    {
        var tiles = MapManager.Instance.GetNeighborsWithinRadius(currentTile.x, currentTile.y, radius);
        foreach (var tile in tiles)
        {
            turfTiles.Remove(tile.HexCoords);
        }
    }

    public bool IsInTurf(Vector2Int hex) => turfTiles.Contains(hex);

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (turfTiles == null || turfTiles.Count == 0)
            return;

        Gizmos.color = Color.red * 0.5f;
        foreach (var coord in turfTiles)
        {
            Vector3 worldPos = MapManager.Instance.HexToWorld(coord);
            Gizmos.DrawSphere(worldPos, 0.3f);
        }
    }
#endif 
}
