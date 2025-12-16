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
    [SerializeField] private float unitHeightOffset = 2f;

    private System.Random rng;

    private void Awake()
    {
        Instance = this;
        rng = new System.Random();
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

        //Instantiate the monster
        GameObject go = Instantiate(prefab, MapManager.Instance.HexToWorld(tile.HexCoords), Quaternion.identity);
        go.name = $"SeaMonster_{prefab.name}_({tile.HexCoords.x}, {tile.HexCoords.y})";

        //Get SeaMonsterBase component
        SeaMonsterBase monster = go.GetComponent<SeaMonsterBase>();
        if (monster == null)
        {
            Debug.LogError($"[SeaMonsterSpawner] Prefab {prefab.name} is missing SeaMonsterBase!");
            Destroy(go);
            return null;
        }

        Vector3 worldPos = MapManager.Instance.HexToWorld(tile.HexCoords);
        worldPos.y += monster.heightOffset;
        go.transform.position = worldPos;

        //Link monster and tile
        monster.currentTile = tile;
        MapManager.Instance.SetUnitOccupied(tile.HexCoords, true);

        return monster;
    }

    private HexTile FindValidSpawnTile()
    {
        List<HexTile> validTiles = new();

        foreach (var tile in MapManager.Instance.GetTiles())
        {
            if (tile.IsOccupied || tile.IsOccupiedByUnit || tile.IsBlockedByTurtleWall)
                continue;
            if (HasBaseNearby(tile)) 
                continue;

            validTiles.Add(tile);
        }

        if (validTiles.Count == 0)
            return null;

        int index = rng.Next(validTiles.Count);
        return validTiles[index];
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
    public GameObject GetPrefabByName(string name)
    {
        return monsterPrefabs.Find(p => p.name == name);
    }

}
