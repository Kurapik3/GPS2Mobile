using UnityEngine;
using System.Collections.Generic;
public class FogSystem : MonoBehaviour
{
    [Header("Fog Settings")]
    [Tooltip("For Debug Purpose")]
    [SerializeField] private bool enableFog = true;
    [SerializeField] private GameObject fogPrefab;
    [SerializeField] private int visibleRadiusAtStart = 2;

    [Header("Fog Placement Offset")]
    [Tooltip("Vertical offset for fog prefab placement")]
    [SerializeField] private float fogYOffset = 1f;

    [Header("Starting Visibility Origin")]
    [Tooltip("Center tile where fog is revealed at the start.")]
    [SerializeField] private Vector2Int startingOrigin = new Vector2Int(0, 0);

    [HideInInspector] public List<Vector2Int> revealedTiles = new List<Vector2Int>();
    private bool mapReady = false;
    private void OnEnable()
    {
        MapGenerator.OnMapReady += HandleMapReady;
    }

    private void OnDisable()
    {
        MapGenerator.OnMapReady -= HandleMapReady;
    }
    private void HandleMapReady(MapGenerator map)
    {
        mapReady = true;
    }
    public void SetStartingOrigin(Vector2Int origin)
    {
        startingOrigin = origin;
    }
    public void InitializeFog()
    {
        if(!enableFog)
        {
            Debug.Log("[FogSystem] Fog disabled — skipping generation.");
            revealedTiles.Clear();
            foreach (var kv in MapManager.Instance.GetAllTiles())
            {
                Vector2Int coord = kv.Key;
                HexTile tile = kv.Value;

                tile.RemoveFog();
                revealedTiles.Add(coord);
            }
            return;
        }
        if(mapReady)
        {
            GenerateInitialFog();
        }
        
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
            tile.AddFog(fogPrefab, fogYOffset);
        }
        // Reveal starting area
        RevealTilesAround(startingOrigin, visibleRadiusAtStart);

        EnemyUnitManager.Instance?.UpdateEnemyVisibility();
        EnemyBaseManager.Instance?.UpdateEnemyBaseVisibility();
        SeaMonsterManager.Instance?.UpdateSeaMonsterVisibility();
    }

    public void RevealTilesAround(Vector2Int center, int radius)
    {
        if (!enableFog)
        {
            return;
        }
        bool anyNewRevealed =false;
        foreach (var kv in MapManager.Instance.GetAllTiles())
        {
            Vector2Int coord = kv.Key;
            HexTile tile = kv.Value;
            int dist = HexCoordinates.Distance(center.x, center.y, coord.x, coord.y);
            if (dist <= radius)
            {               
                //tracks revealed tiles
                if (!revealedTiles.Contains(coord))
                {
                    revealedTiles.Add(coord);
                    tile.RemoveFog();
                    PlayerTracker.Instance.addScore(50);
                    anyNewRevealed = true;
                }
            }
        }

        // Update enemy visibility only if new tiles were revealed
        if (anyNewRevealed && EnemyUnitManager.Instance != null)
        {
            EnemyUnitManager.Instance?.UpdateEnemyVisibility();
            EnemyBaseManager.Instance?.UpdateEnemyBaseVisibility();
            SeaMonsterManager.Instance?.UpdateSeaMonsterVisibility();
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
        RevealTilesAround(startingOrigin, visibleRadiusAtStart);

        EnemyUnitManager.Instance?.UpdateEnemyVisibility();
        SeaMonsterManager.Instance?.UpdateSeaMonsterVisibility();
    }

}
