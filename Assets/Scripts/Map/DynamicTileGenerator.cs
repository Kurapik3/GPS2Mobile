using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ResourceEntry
{
    [Header("Basic Settings")]
    public string id;
    public GameObject prefab;
    public float yOffset = 2f;

    [Header("Spawn Rules")]
    public bool spawnAroundBase = false;
    public bool spawnAroundGrove = false;
    public bool spawnAroundEnemyBase = false;

    [Tooltip("Maximum number of this resource allowed per structure (e.g. per base/grove)")]
    public int maxPerStructure = 1;

    [Tooltip("Maximum number of this resource allowed on the entire map")]
    public int maxPerMap = 10;
}
public class DynamicTileGenerator : MonoBehaviour
{
    [Header("Dynamic Placement Settings")]
    [Tooltip("How many dynamic objects to place around each structure (e.g. base/grove)")]
    public int objectsPerStructure = 4;

    [Tooltip("Radius (in tiles) around structure to place objects")]
    public int radius = 1;

    [Header("Possible Resource Types (choose one randomly for each spawn)")]
    public List<ResourceEntry> resources = new();

    // Track how many of each resource spawned globally
    private Dictionary<string, int> globalResourceCount = new();

    public void GenerateDynamicElements()
    {
        var map = MapManager.Instance;
        if (map == null)
        {
            Debug.LogWarning("MapManager not found. Can't generate dynamic tiles.");
            return;
        }

        globalResourceCount.Clear();

        // find the specific structures
        List<HexTile> structureTiles = new List<HexTile>();
        foreach (var tile in map.GetTiles())
        {
            if (!tile.HasStructure)
            {
                continue;
            }
            string name = tile.StructureName?.ToLower();
            if (name == null)
            {
                continue;
            }
            if (name.Contains("base") || name.Contains("grove") || name.Contains("enemybase"))
            {
                structureTiles.Add(tile);
            }
        }

        // for each base/grove tile, spawn around it
        foreach (var structureTile in structureTiles)
        {
            Debug.Log($"Generating for structure at {structureTile.q},{structureTile.r}");

            string structureName = structureTile.StructureName.ToLower();

            // find resources that are allowed near this structure
            List<ResourceEntry> validResources = new();
            foreach(var entry in resources)
            {
                if(structureName.Contains("base") && entry.spawnAroundBase)
                {
                    validResources.Add(entry);
                }
                else if(structureName.Contains("grove") && entry.spawnAroundGrove)
                {
                    validResources.Add(entry);
                }
                else if(structureName.Contains("enemybase") && entry.spawnAroundEnemyBase)
                {
                    validResources.Add(entry);
                }
            }

            if(validResources.Count == 0)
            {
                continue;
            }

            List<HexTile> nearbyTiles = map.GetNeighborsWithinRadius(structureTile.q, structureTile.r, radius);
            nearbyTiles.RemoveAll(t => t.q == structureTile.q && t.r == structureTile.r); // exclude center

            for (int i = 0; i < nearbyTiles.Count; i++)
            {
                var temp = nearbyTiles[i];
                int rand = Random.Range(i, nearbyTiles.Count);
                nearbyTiles[i] = nearbyTiles[rand];
                nearbyTiles[rand] = temp;
            }

            //local count for this structure
            Dictionary<string, int> perStructureCount = new(); 

            int placed = 0;
            foreach (var t in nearbyTiles)
            {
                if (t == null || t.HasStructure || t.dynamicInstance != null)
                {
                    continue;
                }
                if (placed >= objectsPerStructure)
                {
                    break;
                }
                
                //choose random valid resource
                ResourceEntry entry = validResources[Random.Range(0, validResources.Count)];
                if(entry.prefab == null)
                {
                    continue;
                }

                //check perstructure limit
                perStructureCount.TryGetValue(entry.id, out int localCount);
                if(localCount >= entry.maxPerStructure)
                {
                    continue;
                }

                //check global limit
                globalResourceCount.TryGetValue(entry.id, out int globalCount);
                if(globalCount >= entry.maxPerMap)
                {
                    continue;
                }
                //spawn
                GameObject obj = Instantiate(entry.prefab, t.transform);
                obj.transform.localPosition = new Vector3(0, entry.yOffset, 0);
                obj.name = entry.id;
                t.dynamicInstance = obj;

                // Update counts
                perStructureCount[entry.id] = localCount + 1;
                globalResourceCount[entry.id] = globalCount + 1;
                placed++;
                if (placed >= objectsPerStructure)
                {
                    break;
                }
            }

            Debug.Log($"DynamicTileGenerator: Placed {placed}/{objectsPerStructure} dynamics around {structureTile.StructureName}");
        }

    }

    public void SaveDynamicObjects(GameSaveData data)
    {
        if (data == null)
        { 
            return;
        }
        data.dynamicObjects.Clear();

        foreach (var tile in MapManager.Instance.GetTiles())
        {
            if (tile.dynamicInstance != null)
            {
                data.dynamicObjects.Add(new GameSaveData.DynamicObjectData
                {
                    q = tile.q,
                    r = tile.r,
                    resourceId = tile.dynamicInstance.name
                });
            }
        }

        Debug.Log($"DynamicTileGenerator: Saved {data.dynamicObjects.Count} dynamic objects.");
    }

    public void LoadDynamicObjects(GameSaveData data)
    {
        if (data == null || data.dynamicObjects.Count == 0)
        {
            return;
        }
        foreach (var objData in data.dynamicObjects)
        {
            if (!MapManager.Instance.TryGetTile(new Vector2Int(objData.q, objData.r), out HexTile tile))
            {
                continue;
            }
            // Find matching resource prefab
            ResourceEntry entry = resources.Find(r => r.id == objData.resourceId);
            if (entry == null || entry.prefab == null)
            {
                continue;
            }

            // Rebuild object
            GameObject obj = Instantiate(entry.prefab, tile.transform);
            obj.transform.localPosition = new Vector3(0, entry.yOffset, 0);
            obj.name = entry.id;
            tile.dynamicInstance = obj;
        }

        Debug.Log($"DynamicTileGenerator: Loaded {data.dynamicObjects.Count} dynamic objects.");
    }
}
