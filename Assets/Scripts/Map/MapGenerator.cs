using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class MapGenerator : MonoBehaviour
{
    [SerializeField] private float hexSize = 1f;
    [SerializeField] private int mapRadius = 2;
    [SerializeField] private HexTileGenerationSettings generationSettings;
    [SerializeField] private MapData mapData;
    [SerializeField] public StructureDatabase structureDatabase;
    public Dictionary<Vector2Int, HexTile> AllTiles { get; private set; } = new();
    private readonly List<HexTile> activeTiles = new();
    public MapData MapData => mapData;
    public float GetHexSize() => hexSize;

    public delegate void MapReadyHandler(MapGenerator map);
    public static event MapReadyHandler OnMapReady;
    public bool IsMapReady { get; private set; } = false;

    private void Awake()
    {
        RebuildTileDictionary();
    }
    private void Start()
    {
        RebuildTileDictionary();
        if (mapData == null)
        {
            mapData = Resources.Load<MapData>("DefaultBaseMap");
            if (mapData == null)
            {
                Debug.LogError("[MapGenerator] No MapData assigned and DefaultBaseMap not found in Resources!");
                return;
            }
            else
            {
                Debug.Log("[MapGenerator] Loaded DefaultBaseMap from Resources.");
            }
        }
        GenerateFromData();

        GetComponent<FogSystem>()?.InitializeFog();
        GetComponent<DynamicTileGenerator>()?.GenerateDynamicElements();
    }
    public void SetMapData(MapData data)
    {
        mapData = data;
    }

    public void GenerateDefaultMap() //to use for GameManager later on
    {
        // Just to create a map when no data exists
        LayoutGrid();
        SaveMapData(); // saves into a .asset for editor viewing
    }

    public void RebuildTileDictionary()
    {
        AllTiles.Clear();
        foreach (var tile in GetComponentsInChildren<HexTile>(true))
        {
            AllTiles[new Vector2Int(tile.q, tile.r)] = tile;
        }
        MapManager.Instance?.RegisterTiles(new(AllTiles));
    }

    [ContextMenu("Layout Grid")]
    public void LayoutGrid()
    {
        Clear();
        int radius = mapRadius;
        for(int r= -radius; r <= radius; r++)
        {
            int qMin = Mathf.Max(-radius, -r - radius);
            int qMax = Mathf.Min(radius, -r + radius);
            for(int q = qMin; q <= qMax; q++)
            {
                Vector3 pos = HexCoordinates.ToWorld(q, r, hexSize);
                CreateTile(q, r, pos,HexTile.TileType.Normal);
            }
        }
        RebuildTileDictionary();
        foreach (var tile in AllTiles.Values) //set neighbouring tiles
        {
            tile.FindNeighbors();
        }

        IsMapReady = true;
        OnMapReady?.Invoke(this);
        EventBus.Publish(new EnemyAIEvents.MapReadyEvent(this));
    }
    private HexTile CreateTile(int q, int r, Vector3 pos,HexTile.TileType initialType)
    {
        //Create empty parent
        GameObject container = new($"Hex_{q}_{r}");
        container.transform.parent = transform;
        container.transform.localPosition = pos;

        //Attach HexTile script to parent
        HexTile tile = container.AddComponent<HexTile>();
        tile.settings = generationSettings;
        tile.q = q;
        tile.r = r;
        tile.tileType = initialType;
        tile.AddTile();

        return tile;
    }

    public void GenerateFromData()
    {
        if (mapData == null || generationSettings == null)
        {
            Debug.LogError("Missing map data or generation settings!");
            return;
        }

        // Clear old
        Clear();
        activeTiles.Clear();

        foreach (var tileInfo in mapData.tiles)
        {
            // create hex tile
            Vector3 worldPos = HexCoordinates.ToWorld(tileInfo.q, tileInfo.r, hexSize);
            HexTile hex = CreateTile(tileInfo.q, tileInfo.r, worldPos, tileInfo.tileType);
            activeTiles.Add(hex);
            
            hex.q = tileInfo.q;
            hex.r = tileInfo.r;
            hex.tileType = tileInfo.tileType;
            hex.StructureName = tileInfo.hasStructure ? tileInfo.structureName : null;

            // runtime structure spawn
            if (tileInfo.hasStructure && structureDatabase != null)
            {
                StructureData data = structureDatabase.GetByName(tileInfo.structureName);
                if (data != null)
                {
                    hex.ApplyStructureRuntime(data);
                }
            }
        }
        // put tiles into dictionary and compute neighbors
        RebuildTileDictionary();
        FogSystem fogSystem = FindFirstObjectByType<FogSystem>();
        if (fogSystem != null)
        {
            fogSystem.InitializeFog(); // generate fog first

            foreach (Vector2Int coord in mapData.revealedTiles)
            {
                if (MapManager.Instance.GetAllTiles().TryGetValue(coord, out HexTile tile))
                {
                    tile.RemoveFog(); // reveal the remembered tiles
                }
            }

            // Also sync back into fog system
            fogSystem.revealedTiles = new List<Vector2Int>(mapData.revealedTiles);
        }

        foreach (var t in AllTiles.Values)
        {
            t.FindNeighbors();
        }

        // init runtime-only systems
        //GetComponent<FogSystem>()?.InitializeFog();
        GetComponent<DynamicTileGenerator>()?.GenerateDynamicElements();

        IsMapReady = true;
        OnMapReady?.Invoke(this);
        EventBus.Publish(new EnemyAIEvents.MapReadyEvent(this));

        Debug.Log("Map generated from MapData (runtime).");
    }

    [ContextMenu("Save Map to ScriptableObject")]
    public void SaveMapData()
    {
#if UNITY_EDITOR
        if (mapData == null)
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Map Data", "NewMapData", "asset", "");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            mapData = ScriptableObject.CreateInstance<MapData>();
            AssetDatabase.CreateAsset(mapData, path);
        }

        mapData.tiles.Clear();
        foreach (var (coord, tile) in AllTiles)
        {
            HexTileData data = new()
            {
                q = tile.q,
                r = tile.r,
                tileType = tile.tileType,
                hasStructure = tile.HasStructure,
                structureName = tile.StructureName
            };
            mapData.tiles.Add(data);
        }

        EditorUtility.SetDirty(mapData);
        AssetDatabase.SaveAssets();
        Debug.Log($"Map saved to ScriptableObject: {mapData.name}");
#endif
    }

    [ContextMenu("Load Map From ScriptableObject")]
    public void LoadMapData()
    {
        if (mapData == null)
        {
            Debug.LogError("No MapData assigned!");
            return;
        }

        Clear();
        foreach (var data in mapData.tiles)
        {
            Vector3 pos = HexCoordinates.ToWorld(data.q, data.r, hexSize);
            HexTile tile = CreateTile(data.q, data.r, pos, data.tileType);
            tile.tileType = data.tileType;
            tile.StructureName = data.hasStructure ? data.structureName : null;

#if UNITY_EDITOR
            if (data.hasStructure && !string.IsNullOrEmpty(data.structureName))
            {
                if (!tile.TryGetComponent(out StructureTile structureTile))
                    structureTile = tile.gameObject.AddComponent<StructureTile>();

                structureTile.structureDatabase = structureDatabase;
                structureTile.selectedIndex = Mathf.Clamp(
                    structureDatabase.structures.FindIndex(s => s.structureName == data.structureName),
                    0,
                    Mathf.Max(0, structureDatabase.structures.Count - 1)
                );

                structureTile.ApplyStructure();

                tile.StructureName = data.structureName;
                tile.structureIndex = structureTile.selectedIndex;
            }
#else
            if (data.hasStructure && structureDatabase != null)
            {
                StructureData sData = structureDatabase.GetByName(data.structureName);
                if (sData != null)
                {
                    tile.ApplyStructureRuntime(sData);
                }
            }
#endif
         
    }
    RebuildTileDictionary();
#if UNITY_EDITOR
        // Force update structure visuals in Editor
        if (!Application.isPlaying)
        {
            foreach (var tile in AllTiles.Values)
            {
                if (tile.tileType == HexTile.TileType.Structure)
                {
                    var st = tile.GetComponent<StructureTile>();
                    if (st == null)
                    {
                        st = tile.gameObject.AddComponent<StructureTile>();
                    }

                    st.structureDatabase = structureDatabase;
                    st.ApplyStructure(); // rebuilds the visual
                }
            }

            UnityEditor.SceneView.RepaintAll();
        }
#endif
        IsMapReady = true;
        OnMapReady?.Invoke(this);
        EventBus.Publish(new EnemyAIEvents.MapReadyEvent(this));
        Debug.Log($"Loaded map from ScriptableObject: {mapData.name}");
    }


    private void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        AllTiles.Clear();
        MapManager.Instance?.Clear();
        IsMapReady = false;
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(MapGenerator))]
    public class MapGeneratorInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MapGenerator editor = (MapGenerator)target;

            if (GUILayout.Button("Layout Grid"))
            {
                editor.LayoutGrid();
            }

            if (GUILayout.Button("Clear Grid"))
            {
                editor.Clear();
            }

            if (GUILayout.Button("Save Map to ScriptableObject"))
            {
                editor.SaveMapData();
            }

            if (GUILayout.Button("Load Map From ScriptableObject"))
            {
                editor.LoadMapData();
            }
        }
    }
#endif
}
