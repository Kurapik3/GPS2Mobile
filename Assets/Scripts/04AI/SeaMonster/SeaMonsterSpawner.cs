using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles spawning of neutral Sea Monsters (Kraken, TurtleWall).
/// Ensures spawn rules: only water tiles, no nearby bases.
/// </summary>
public class SeaMonsterSpawner : MonoBehaviour
{
    public static SeaMonsterSpawner Instance { get; private set; }

    [Header("Setup")]
    [SerializeField] private List<GameObject> monsterPrefabs; // Kraken / TurtleWall prefabs
    [SerializeField] private float unitHeightOffset = 1.5f;

    private void Awake()
    {
        Instance = this;
    }

    public SeaMonsterBase SpawnRandomMonster()
    {
        if (monsterPrefabs == null || monsterPrefabs.Count == 0)
        {
            Debug.LogWarning("[SeaMonsterSpawner] No monster prefabs assigned!");
            return null;
        }

        //Pick a random prefab
        GameObject prefab = monsterPrefabs[Random.Range(0, monsterPrefabs.Count)];

        //Find a valid water tile
        HexTile tile = FindValidSpawnTile();
        if (tile == null)
        {
            Debug.LogWarning("[SeaMonsterSpawner] No valid spawn tile found!");
            return null;
        }

        //Convert hex to world position
        Vector2Int coords = tile.HexCoords;
        Vector3 worldPos = MapManager.Instance.HexToWorld(coords);
        worldPos.y += unitHeightOffset;

        //Instantiate the monster
        GameObject go = Instantiate(prefab, worldPos, Quaternion.identity);
        go.name = $"SeaMonster_{prefab.name}_{coords.x}_{coords.y}";

        //Get SeaMonsterBase component
        SeaMonsterBase monster = go.GetComponent<SeaMonsterBase>();
        if (monster == null)
        {
            Debug.LogError($"[SeaMonsterSpawner] Prefab {prefab.name} is missing SeaMonsterBase!");
            Destroy(go);
            return null;
        }

        //Link monster and tile
        monster.CurrentTile = tile;
        MapManager.Instance.SetUnitOccupied(tile.HexCoords, true);

        return monster;
    }

    private HexTile FindValidSpawnTile()
    {
        List<HexTile> validTiles = new();

        foreach (var tile in MapManager.Instance.GetTiles())
        {
            if (tile.IsOccupied) 
                continue;
            if (HasBaseNearby(tile)) 
                continue;

            validTiles.Add(tile);
        }

        if (validTiles.Count == 0)
            return null;

        return validTiles[Random.Range(0, validTiles.Count)];
    }

    private bool HasBaseNearby(HexTile tile)
    {
        List<HexTile> neighbors = MapManager.Instance.GetNeighborsWithinRadius(tile.HexCoords.x, tile.HexCoords.y, 1);
        foreach (HexTile n in neighbors)
        {
            if (n == null) 
                continue;
            if (n.HasTreeBase || n.HasEnemyBase)
                return true;
        }
        return false;
    }
}
