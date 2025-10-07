using UnityEngine;
using System.Collections.Generic;
using UnityEditor.SceneManagement;


#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class MapGenerator : MonoBehaviour
{
    [SerializeField] private float hexSize = 1f;
    [Tooltip("For Hexagon")]
    [SerializeField] private int mapRadius = 2;
    [SerializeField] private HexTileGenerationSettings generationSettings;
    public static Dictionary<Vector2Int, HexTile> AllTiles { get; private set; } = new();
    private void Awake()
    {
        RebuildTileDictionary();
    }
    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            RebuildTileDictionary();
        }
    }
    private void Start()
    {
        //RebuildAllStructures();
        var fog = GetComponent<FogSystem>();
        if (fog != null)
            fog.InitializeFog();

        var dynamic = GetComponent<DynamicTileGenerator>();
        if (dynamic != null)
            dynamic.GenerateDynamicElements();
    }
    public void RebuildAllStructures()
    {
        foreach (var tile in MapGenerator.AllTiles.Values)
        {
            if (tile.HasStructure)
            {
                tile.RebuildStructure();
            }
        }
    }
    
    public void RebuildTileDictionary()
    {
        AllTiles.Clear();
        HexTile[] tiles = GetComponentsInChildren<HexTile>(true);
        Debug.Log($"[MapGenerator] Found {tiles.Length} HexTile components");
        foreach (var tile in tiles)
        {
            Vector2Int key = new Vector2Int(tile.q, tile.r);
            AllTiles[key] = tile;
        }
        Debug.Log($"Map Generator: Rebuilt dictionary with {AllTiles.Count} tiles.");
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            RebuildTileDictionary();
        }
    }
#endif
    [ContextMenu("Layout Grid")]
    public void LayoutGrid()
    {
        Clear();
        LayoutHexagonGrid();
        foreach (var tile in AllTiles.Values) //set neighbouring tiles
        {
            tile.FindNeighbors();
        }
    }
    
    private void LayoutHexagonGrid()
    {
        int radius = mapRadius;

        for (int r = -radius; r <= radius; r++)
        {
            int qMin = Mathf.Max(-radius, -r - radius);
            int qMax = Mathf.Min(radius, -r + radius);

            for (int q = qMin; q <= qMax; q++)
            {
                Vector3 pos = HexCoordinates.ToWorld(q, r, hexSize);
                CreateTile(q, r, pos);
            }
        }
        
    }
    private void CreateTile(int q, int r, Vector3 pos)
    {
        //Create empty parent
        GameObject container = new GameObject($"Hex_{q}_{r}");
        container.transform.parent = transform;
        container.transform.localPosition = pos;

        //Attach HexTile script to parent
        HexTile tile = container.AddComponent<HexTile>();
        tile.settings = generationSettings;
        tile.q = q;
        tile.r = r;
        tile.tileType = HexTile.TileType.Normal;

        //Spawn the mesh prefab as a child
        GameObject prefab = generationSettings.GetTile(tile.tileType);
        if (prefab != null)
        {
            GameObject mesh = (GameObject)PrefabUtility.InstantiatePrefab(prefab,container.transform);
            mesh.name = "Mesh";
            mesh.transform.localPosition = Vector3.zero;
        }
        else
        {
            Debug.LogError("No prefab assigned!");
        }
        Vector2Int key = new(q, r);
        if(AllTiles.ContainsKey(key))
        {
            Debug.LogWarning($"Duplicate tile at {q},{r}");
        }
        else
        {
            AllTiles[key] = tile;
        }
    }

    [ContextMenu("Save As Prefab")]
    public void SaveChunk()
    {
#if UNITY_EDITOR
        
        if (GetComponent<FogSystem>() == null)
        {
            gameObject.AddComponent<FogSystem>();
            Debug.Log("Added FogSystem component automatically");
        }
        if(GetComponent<DynamicTileGenerator>() == null)
        {
            gameObject.AddComponent<DynamicTileGenerator>();
            Debug.Log("Added DynamicTileGenerator component automatically");
        }
       
        string path = EditorUtility.SaveFilePanelInProject
        (
            "Save Map Prefab",
            "Map",
            "prefab",
            "Save this Map as a prefab"
        );

        if (!string.IsNullOrEmpty(path))
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, path, InteractionMode.UserAction);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log($"Map saved as Prefab at {path}");
        }
        
#endif
    }
    private void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        AllTiles.Clear();
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

            if (GUILayout.Button("Save As Prefab"))
            {
                editor.SaveChunk();
            }
        }
    }
#endif
}
