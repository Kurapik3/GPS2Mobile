
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

    [Header("Chunk Settings")]
    public int chunkWidth = 5;  // Must match ChunkEditor gridSize.x for square
    public int chunkHeight = 5; // Must match ChunkEditor gridSize.y for square
    public int chunkRadius = 2;// chunk radius in tiles (radius 2, diameter 5)


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

                // Get world position for this chunk
                Vector3 pos = ChunkToWorld(x, y, hexSize, chunkWidth, chunkHeight);

                GameObject chunk = Instantiate(prefab, pos, Quaternion.identity, transform);

                // Calculate global axial offset for tiles inside this chunk
                int chunkQ = x - (y + (y % 2)) / 2;  // even-r offset to axial
                int chunkR = y;

                RegisterTilesInChunk(chunk, chunkQ * chunkWidth, chunkR * chunkHeight);
            }
        }
        //set neighbours
        foreach (var tile in AllTiles.Values)
        {
            tile.FindNeighbors();
        }
    }
    private void GenerateHexagonalMap()
    {
        int mapRadius = mapWidth; // use mapWidth as radius (symmetric)
        int chunkSpacing = (chunkRadius * 2) + 1; // diameter in tiles

        for (int cq = -mapRadius; cq <= mapRadius; cq++)
        {
            for (int cr = -mapRadius; cr <= mapRadius; cr++)
            {
                int cs = -cq - cr;
                if (Mathf.Abs(cq) > mapRadius || Mathf.Abs(cr) > mapRadius || Mathf.Abs(cs) > mapRadius)
                    continue; // outside map shape

                GameObject prefab = chunkPrefabs[Random.Range(0, chunkPrefabs.Count)];

                int chunkOriginQ = cq * chunkSpacing;
                int chunkOriginR = cr * chunkSpacing;

                //Use pointy-top chunk spacing
                Vector3 pos = HexCoordinates.ToWorld(chunkOriginQ, chunkOriginR, hexSize);

                GameObject chunk = Instantiate(prefab, pos, Quaternion.identity, transform);
                RegisterTilesInChunk(chunk, chunkOriginQ, chunkOriginR);
            }
        }
    }
 
    private void RegisterTilesInChunk(GameObject chunkInstance, int chunkOriginQ, int chunkOriginR)
    {
        foreach (HexTile tile in chunkInstance.GetComponentsInChildren<HexTile>())
        {
            // Add chunk's global offset to tile's local coordinates
            int globalQ = tile.q + chunkOriginQ;
            int globalR = tile.r + chunkOriginR;

            tile.q = globalQ;
            tile.r = globalR;
            
            Vector2Int key = new(globalQ, globalR);
            if (AllTiles.ContainsKey(key))
                Debug.LogWarning($"Duplicate tile at ({globalQ}, {globalR})!");
            else
                AllTiles[key] = tile;
        }
    }

    private void ClearMap()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        AllTiles.Clear();
    }
    // Converts chunk grid position (col, row) to world position for chunk origin
    private Vector3 ChunkToWorld(int chunkCol, int chunkRow, float hexSize, int chunkWidth, int chunkHeight)
    {
        // Width and height of ONE hex tile
        float hexWidth = Mathf.Sqrt(3f) * hexSize;      // pointy-top hex width
        float rowHeight = 1.5f * hexSize;               // vertical spacing between rows

        // Total width/height of the entire chunk
        float chunkWorldWidth = chunkWidth * hexWidth;
        float chunkWorldHeight = chunkHeight * rowHeight;

        // X position: shift odd rows by half a hex width
        float x = chunkCol * chunkWorldWidth;
        if (chunkRow % 2 == 1)
        {
            x += hexWidth / 2f; 
        }

        // Z position: spaced by chunk height
        float z = chunkRow * chunkWorldHeight;

        return new Vector3(x, 0, z);
    }
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
