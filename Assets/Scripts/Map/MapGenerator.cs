
// - Every HexTile has .q and .r (axial coordinates)
// - All tiles are in MapGenerator.AllTiles[new Vector2Int(q, r)]
// - World position: HexCoordinates.ToWorld(q, r, hexSize)
// - Distance: HexCoordinates.Distance(q1, r1, q2, r2)
// - Directions: HexCoordinates.Directions (6 neighbors)
// 
// EXAMPLE:
//   if (MapGenerator.AllTiles.TryGetValue(new(3,2), out HexTile t))
//       unit.transform.position = t.transform.position;

using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
public enum MapShape { Rectangular, Hexagonal }
public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance { get; private set; }
    public static Dictionary<Vector2Int, HexTile> AllTiles { get; private set; } = new();
    [Header("Chunks")]
    [SerializeField] private List<GameObject> chunkPrefabs;
    public HexTileGenerationSettings generationSettings;

    [Header("Map Settings")]
    public MapShape mapShape = MapShape.Rectangular;
    public int mapWidth = 4;
    public int mapHeight = 4;
    public float hexSize = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    [ContextMenu("Generate Random Map")]
    public void GenerateRandomMap()
    {
        ClearMap();

        if (mapShape == MapShape.Rectangular)
        {
            GenerateRectangularMap();
        }
        else
        {
            GenerateHexagonalMap();
        }
    }
    private void GenerateRectangularMap()
    {
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                GameObject prefab = chunkPrefabs[Random.Range(0, chunkPrefabs.Count)];
                // Use axial-based spacing (not arbitrary 1.8f/1.6f)
                Vector3 pos = HexCoordinates.ToWorld(x * 5, y * 5, hexSize); // 5 = chunk width/height
            
                GameObject chunk = Instantiate(prefab, pos, Quaternion.identity, transform);
                RegisterTilesInChunk(chunk);
            }
        }
    }
    private void GenerateHexagonalMap()
    {
        int radius = mapWidth / 2;
        for (int q = -radius; q <= radius; q++)
        {
            int r1 = Mathf.Max(-radius, -q - radius);
            int r2 = Mathf.Min(radius, -q + radius);
            for (int r = r1; r <= r2; r++)
            {
                GameObject prefab = chunkPrefabs[Random.Range(0, chunkPrefabs.Count)];
                Vector3 pos = HexCoordinates.ToWorld(q * 5, r * 5, hexSize);
           
                GameObject chunk = Instantiate(prefab, pos, Quaternion.identity, transform);
                RegisterTilesInChunk(chunk);
            }
        }
    }
    private void RegisterTilesInChunk(GameObject chunkInstance)
    {
        foreach (HexTile tile in chunkInstance.GetComponentsInChildren<HexTile>())
        {
            Vector2Int key = new(tile.q, tile.r);
            // Optional: warn if duplicate
            if (AllTiles.ContainsKey(key))
                Debug.LogWarning($"Duplicate tile at ({tile.q}, {tile.r})!");
            else
                AllTiles[key] = tile;
        }
    }
    //private void SpawnChunk(ChunkData chunk, int cx, int cy)
    //{
    //    Vector3 chunkOffset = new Vector3(cx * chunk.width * hexSize * 1.8f, 0, cy * chunk.height * hexSize * 1.6f);

    //    foreach (var record in chunk.tiles)
    //    {
    //        GameObject prefab = generationSettings.GetTile(record.type);
    //        if (prefab != null)
    //        {
    //            int globalQ = record.q + cx * chunk.width;
    //            int globalR = record.r + cy * chunk.height;

    //            Vector3 pos = HexToWorld(globalQ, globalR, hexSize);
    //            GameObject go = Instantiate(prefab, pos, Quaternion.identity, transform);

    //            // Attach HexTile if not present
    //            HexTile tile = go.GetComponent<HexTile>();
    //            if (tile == null)
    //            {
    //                tile = go.AddComponent<HexTile>();
    //            }
    //            tile.q = globalQ;
    //            tile.r = globalR;
    //            tile.tileType = record.type;

    //            //Pathfinding Hook(later)
    //            // tile.cubeCoordinate = OffsetToCube(tile.q, tile.r);
    //        }
    //    }
    //}

    private void ClearMap()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
    //private Vector3 HexToWorld(int q, int r, float size)
    //{
    //    float x = size * (Mathf.Sqrt(3) * q + Mathf.Sqrt(3) / 2f * r);
    //    float z = size * (3f / 2f * r);
    //    return new Vector3(x, 0, z);
    //}
#if UNITY_EDITOR
    [CustomEditor(typeof(MapGenerator))]
    public class MapGeneratorInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MapGenerator editor = (MapGenerator)target;

            if (GUILayout.Button("Generate Map"))
            {
                editor.GenerateRandomMap();
            }

            if (GUILayout.Button("Clear Map"))
            {
                editor.ClearMap();
            }
        }
    }
#endif
}
