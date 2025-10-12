using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ResourceEntry
{
    public string id;
    public GameObject prefab;
    public float yOffset = 2f;
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

    public void GenerateDynamicElements()
    {
        var map = MapManager.Instance;
        if (map == null)
        {
            Debug.LogWarning("MapManager not found. Can't generate dynamic tiles.");
            return;
        }

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
            List<HexTile> nearbyTiles = map.GetNeighborsWithinRadius(structureTile.q, structureTile.r, radius);
            nearbyTiles.RemoveAll(t => t.q == structureTile.q && t.r == structureTile.r); // exclude center

            for (int i = 0; i < nearbyTiles.Count; i++)
            {
                var temp = nearbyTiles[i];
                int rand = Random.Range(i, nearbyTiles.Count);
                nearbyTiles[i] = nearbyTiles[rand];
                nearbyTiles[rand] = temp;
            }

            int placed = 0;
            foreach (var t in nearbyTiles)
            {
                if (placed >= objectsPerStructure) break;
                if (t == null || t.HasStructure || t.dynamicInstance != null)
                    continue;

                // choose random prefab
                if (resources.Count == 0)
                    continue;

                ResourceEntry entry = resources[Random.Range(0, resources.Count)];
                if (entry.prefab == null)
                    continue;

                GameObject obj = Instantiate(entry.prefab, t.transform);
                obj.transform.localPosition = new Vector3(0, entry.yOffset, 0);
                obj.name = entry.prefab.name;
                t.dynamicInstance = obj;

                placed++;
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
