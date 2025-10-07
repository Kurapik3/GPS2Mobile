using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class MapGenerator : MonoBehaviour
{
    //[Tooltip("For Square")]
    //[SerializeField] private Vector2Int gridSize = new(5, 5);
    
    //[SerializeField] private ChunkShape shape = ChunkShape.Square;
    [SerializeField] private float hexSize = 1f;
    [Tooltip("For Hexagon")]
    [SerializeField] private int mapRadius = 2;
    [SerializeField] private HexTileGenerationSettings generationSettings;
    public static Dictionary<Vector2Int, HexTile> AllTiles { get; private set; } = new();

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
    //private void LayoutSquareGrid()
    //{
    //    for (int row = 0; row < gridSize.y; row++)
    //    {
    //        for (int col = 0; col < gridSize.x; col++)
    //        {
    //            // Even-r offset to axial (for pointy-top)
    //            int q = col - (row + (row % 2)) / 2;
    //            int r = row;

    //            Vector3 pos = HexCoordinates.ToWorld(q, r, hexSize);
    //            CreateTile(q, r, pos);
    //        }
    //    }
    //}
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

        //GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
        //go.name = $"Hex_{q}_{r}";
        //go.transform.localPosition = pos;

        //HexTile tile = go.GetComponent<HexTile>();
        //if (tile == null) tile = go.AddComponent<HexTile>();
        //tile.settings = generationSettings;
        //tile.q = q;
        //tile.r = r;
        //tile.tileType = HexTile.TileType.Normal;

        //Register tile in dictionary
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
            Debug.Log($"Chunk saved as Prefab at {path}");
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
    //private Vector3 HexToWorld(int q, int r, float size)
    //{
    //    float x = size * (Mathf.Sqrt(3) * q + Mathf.Sqrt(3) / 2f * r);
    //    float z = size * (3f / 2f * r);
    //    return new Vector3(x, 0, z);
    //}
    //public static Vector3Int OffsetToCube(Vector2Int offset)
    //{
    //    var q = offset.x - (offset.y + (offset.y % 2)) / 2;
    //    var r = offset.y;
    //    return new Vector3Int(q, r, -q - r);
    //}
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
