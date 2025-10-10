using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ResourceEntry
{
    public string id;
    public GameObject prefab;
    public int count = 5;
    public int minDistanceFromOrigin = 0; // e.g. don't spawn too close to center/base
    public bool blocksIfStructure = true;  // skip tiles with structures
}
public class DynamicTileGenerator : MonoBehaviour
{
    [Header("Development types")]
    public List<ResourceEntry> resources = new();

    [Header("Global rules")]
    public int maxAttemptsPerResource = 100;

    public void GenerateDynamicElements()
    {
        var map = MapManager.Instance;
        if (map == null)
        {
            Debug.LogWarning("MapManager not found. Can't generate dynamic tiles.");
            return;
        }

        // Build list of candidate tiles (scene)
        List<HexTile> candidates = new List<HexTile>(map.GetTiles());
        // remove tiles that already have structures or dynamic objects
        candidates.RemoveAll(t => t == null || t.HasStructure || t.dynamicInstance != null);

        foreach (var res in resources)
        {
            if (res.prefab == null || res.count <= 0) continue;
            int placed = 0;
            int attempts = 0;

            while (placed < res.count && candidates.Count > 0 && attempts < maxAttemptsPerResource)
            {
                attempts++;
                int idx = Random.Range(0, candidates.Count);
                HexTile tile = candidates[idx];

                // distance rule (origin 0,0)
                int dist = HexCoordinates.Distance(tile.q, tile.r, 0, 0);
                if (dist < res.minDistanceFromOrigin)
                {
                    // skip tile, but don't remove it globally
                    candidates.RemoveAt(idx);
                    continue;
                }

                // optionally skip tiles with structures
                if (res.blocksIfStructure && tile.HasStructure)
                {
                    candidates.RemoveAt(idx);
                    continue;
                }

                // also skip tiles that already got a dynamic item
                if (tile.dynamicInstance != null)
                {
                    candidates.RemoveAt(idx);
                    continue;
                }

                // Place resource
                GameObject inst = Instantiate(res.prefab, tile.transform);
                inst.name = res.prefab.name;
                inst.transform.localPosition = Vector3.zero;

                // flag it on tile so we don't double-place
                tile.dynamicInstance = inst;

                placed++;
                // remove tile from candidates so we don't pick it again
                candidates.RemoveAt(idx);
            }

            Debug.Log($"DynamicTileGenerator: Placed {placed}/{res.count} of '{res.id}'");
        }
    }

    public void GenerateDynamicTiles(Dictionary<Vector2Int, HexTile> tiles)
    {
        if (tiles == null)
        {
            return;
        }
        // Create a temporary MapManager-like list and call the above
        // (copy to a list so code is shared)
        var list = new List<HexTile>(tiles.Values);
        // remove unusable tiles
        list.RemoveAll(t => t == null || t.HasStructure || t.dynamicInstance != null);

        // Use same logic as above but with 'list' local
        foreach (var res in resources)
        {
            if (res.prefab == null || res.count <= 0) continue;
            int placed = 0;
            int attempts = 0;

            var candidates = new List<HexTile>(list);

            while (placed < res.count && candidates.Count > 0 && attempts < maxAttemptsPerResource)
            {
                attempts++;
                int idx = Random.Range(0, candidates.Count);
                HexTile tile = candidates[idx];

                int dist = HexCoordinates.Distance(tile.q, tile.r, 0, 0);
                if (dist < res.minDistanceFromOrigin)
                {
                    candidates.RemoveAt(idx);
                    continue;
                }
                if (res.blocksIfStructure && tile.HasStructure)
                {
                    candidates.RemoveAt(idx);
                    continue;
                }
                if (tile.dynamicInstance != null)
                {
                    candidates.RemoveAt(idx);
                    continue;
                }

                GameObject inst = Instantiate(res.prefab, tile.transform);
                inst.name = res.prefab.name;
                inst.transform.localPosition = Vector3.zero;
                tile.dynamicInstance = inst;

                placed++;
                candidates.RemoveAt(idx);
            }

            Debug.Log($"DynamicTileGenerator: Placed {placed}/{res.count} of '{res.id}' (dictionary mode)");
        }
    }

    //[Header("Dynamic Tile Prefabs")]
    //public GameObject[] resourcePrefabs;
    //public int count = 10;
    //public int minDistanceFromBase = 2;


    //[SerializeField] private GameObject fishPrefab;
    //[SerializeField] private GameObject debrisPrefab;
    //[Range(0f, 1f)] public float fishChance = 0.2f;
    //[Range(0f, 1f)] public float debrisChance = 0.1f;

    //public void GenerateDynamicElements()
    //{
    //    foreach (var tile in MapManager.Instance.GetTiles())
    //    {
    //        //Checks if tiles already have a structure on it
    //        if(tile.HasStructure)
    //        {
    //            continue;
    //        }
    //        float rand = Random.value;
    //        GameObject toSpawn = null;
    //        if (rand < fishChance)
    //        {
    //            toSpawn = fishPrefab;
    //        }
    //        else if(rand <fishChance +debrisChance)
    //        {
    //            toSpawn = debrisPrefab;
    //        }

    //        if(toSpawn != null)
    //        {
    //            var obj = Instantiate(toSpawn, tile.transform);
    //            obj.transform.localPosition = Vector3.zero;
    //            obj.name = toSpawn.name;
    //        }
    //    }
    //}


    //public void GenerateDynamicTiles(Dictionary<Vector2Int, HexTile> tiles)
    //{
    //    if (resourcePrefabs.Length == 0)
    //    {
    //        return;
    //    }
    //    List<HexTile> allTiles = new(tiles.Values);
    //    int placed = 0;

    //    while (placed < count && allTiles.Count > 0)
    //    {
    //        HexTile tile = allTiles[Random.Range(0, allTiles.Count)];
    //        allTiles.Remove(tile);

    //        if (tile.HasStructure)
    //        {
    //            continue;
    //        }
    //        // simple example: place only if far enough from (0,0)
    //        int dist = HexCoordinates.Distance(tile.q, tile.r, 0, 0);
    //        if (dist < minDistanceFromBase)
    //        {
    //            continue;
    //        }
    //        GameObject res = Instantiate(resourcePrefabs[Random.Range(0, resourcePrefabs.Length)], tile.transform);
    //        res.transform.localPosition = Vector3.zero;
    //        placed++;
    //    }

    //    Debug.Log($"Spawned {placed} dynamic tiles!");
    //}
}

