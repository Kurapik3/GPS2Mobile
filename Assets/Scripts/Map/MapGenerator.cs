using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Overlays;


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
    [SerializeField] private StructureDatabase structureDatabase;
    public Dictionary<Vector2Int, HexTile> AllTiles { get; private set; } = new();
    private readonly List<HexTile> activeTiles = new();
    public MapData MapData => mapData;
    private void Awake()
    {
        RebuildTileDictionary();
    }
    private void Start()
    {
        RebuildTileDictionary();
        GetComponent<FogSystem>()?.InitializeFog();
        GetComponent<DynamicTileGenerator>()?.GenerateDynamicElements();
    }
    public void SetMapData(MapData data)
    {
        mapData = data;
    }

    public void GenerateDefaultMap()
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
                CreateTile(q, r, pos);
            }
        }
        RebuildTileDictionary();
        foreach (var tile in AllTiles.Values) //set neighbouring tiles
        {
            tile.FindNeighbors();
        }
    }
    private void CreateTile(int q, int r, Vector3 pos,HexTile.TileType initialType = HexTile.TileType.Normal)
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

        //Spawn the mesh prefab as a child
        //GameObject prefab = generationSettings.GetTile(tile.tileType);
        //if (prefab != null)
        //{
        //    GameObject mesh = (GameObject)PrefabUtility.InstantiatePrefab(prefab,container.transform);
        //    mesh.name = "Mesh";
        //    mesh.transform.localPosition = Vector3.zero;
        //}
        //else
        //{
        //    Debug.LogError("No prefab assigned!");
        //}


        //Vector2Int key = new(q, r);
        //if(AllTiles.ContainsKey(key))
        //{
        //    Debug.LogWarning($"Duplicate tile at {q},{r}");
        //}
        //else
        //{
        //    AllTiles[key] = tile;
        //}
    }

    public void GenerateFromData()
    {
        if (mapData == null || generationSettings == null)
        {
            Debug.LogError("Missing map data or generation settings!");
            return;
        }

        // Clear old
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        activeTiles.Clear();

        foreach (var tileInfo in mapData.tiles)
        {
            // create hex tile
            Vector3 worldPos = HexCoordinates.ToWorld(tileInfo.q, tileInfo.r, hexSize);
            GameObject tileObj = Instantiate(generationSettings.GetTile(tileInfo.tileType), worldPos, Quaternion.identity, transform);

            HexTile hex = tileObj.GetComponent<HexTile>();
            hex.q = tileInfo.q;
            hex.r = tileInfo.r;
            hex.tileType = tileInfo.tileType;

            activeTiles.Add(hex);

            // if it has structure
            if (tileInfo.hasStructure && structureDatabase != null)
            {
                StructureData data = structureDatabase.GetByName(tileInfo.structureName);
                if (data != null)
                {
                    hex.ApplyStructureRuntime(data);
                }
            }
        }

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
            CreateTile(data.q, data.r, pos, data.tileType);

            if (AllTiles.TryGetValue(new Vector2Int(data.q, data.r), out var tile))
            {
                tile.tileType = data.tileType;
                if (data.hasStructure)
                {
#if UNITY_EDITOR
                    //ensure the structure shows in the editor view
                    if (!Application.isPlaying)
                    {
                        if (!tile.TryGetComponent(out StructureTile structureTile))
                        { 
                            structureTile = tile.gameObject.AddComponent<StructureTile>(); 
                        }

                        structureTile.structureDatabase = structureDatabase;
                        structureTile.selectedIndex = structureDatabase.structures.FindIndex(s => s.structureName == data.structureName);

                        structureTile.ApplyStructure();
                    }
                    else
#endif
                    {
                        tile.ApplyStructureByName(data.structureName);
                    }
                }
            }
        }
        RebuildTileDictionary();
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
