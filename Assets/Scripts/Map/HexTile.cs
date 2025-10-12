using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
[ExecuteInEditMode]
public class HexTile : MonoBehaviour
{
    public enum TileType
    {
        Normal,
        Structure,
    }

    [Header("References")]
    [SerializeField] public HexTileGenerationSettings settings;

    [Header("Hex Coordinates (Axial)")]
    public int q;
    public int r;

    [Header("Tile Data")]
    [SerializeField] public TileType tileType;
    public List<HexTile> neighbours = new();
    [HideInInspector] public GameObject tile;
    public GameObject fogInstance;
    public bool IsFogged => fogInstance != null;

    public GameObject dynamicInstance;
    public bool HasDynamic => dynamicInstance != null;
    //Computed properties
    //public Vector2Int OffsetCoord => new Vector2Int(q + (r + (r % 2)) / 2, r);
    //public Vector3Int CubeCoord => new Vector3Int(q, -q - r, r);


    [Header("Structure Data")]
    public int structureIndex = -1; // -1 = no structure
    public string StructureName;
    //private StructureTile structureTile;
    public bool HasStructure => structureIndex >= 0 || !string.IsNullOrEmpty(StructureName);

    private bool isDirty;

    public bool IsOccupied => HasStructure || HasDynamic;
    private void OnValidate()
    {
#if UNITY_EDITOR
        if (settings != null)
        {
            isDirty = true;
            EditorApplication.delayCall += () =>
            {
                if (this != null) HandleStructureComponent();
            };
        }
#endif
    }

    private void Update()
    {
        if (!isDirty) return;

        // Destroy old visual if it exists
        var oldMesh = transform.Find("Mesh");
        if (oldMesh != null)
        {
            if (Application.isPlaying)
            {
                Destroy(oldMesh.gameObject);
            }   
            else
            {
                DestroyImmediate(oldMesh.gameObject);
            }
        }
        // Spawn new one
        AddTile();
        isDirty = false;
    }
 
    //public void RollTileType()
    //{
    //    tileType = (TileType)Random.Range(0, System.Enum.GetValues(typeof(TileType)).Length);
    //    isDirty = true;
    //}
    public void AddTile()
    {
        if (settings == null)
        {
            return;
        }
        GameObject prefab = settings.GetTile(tileType);
        if (prefab == null)
        {
            return;
        }
        // Instantiate as child so it stays local
        tile = Application.isPlaying?Instantiate(prefab, transform):(GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
        tile.name = "Mesh";
        tile.transform.localPosition = Vector3.zero;
        //tile.transform.localRotation = Quaternion.identity;
        //tile.transform.localScale = Vector3.one;

        if (tile.TryGetComponent(out MeshFilter mf) && !tile.GetComponent<MeshCollider>())
        {
            tile.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        }
    }

    public void FindNeighbors()
    {
        neighbours.Clear();
        foreach (var dir in HexCoordinates.Directions)
        {
            Vector2Int key = new(q + dir.x, r + dir.y);
            if (MapManager.Instance.TryGetTile(key, out HexTile neighbor))
            {
                neighbours.Add(neighbor);
            }
        }
    }
    public void AddFog(GameObject fogPrefab)
    {
        if (fogInstance != null || fogPrefab == null)
        {
            return;
        }
        fogInstance = Instantiate(fogPrefab, transform);
        fogInstance.name = "Fog";
        fogInstance.transform.localPosition = Vector3.zero;
        
    }
    public void RemoveFog()
    {
        if (fogInstance == null)
        {
            return;
        }
        if (Application.isPlaying)
        {
            Destroy(fogInstance);
        }
        else
        {
            DestroyImmediate(fogInstance);
        }
        fogInstance = null;
    }
    public void ApplyStructureRuntime(StructureData data)
    {
        if (data == null || data.prefab == null)
        {
            return;
        }
        // Clean old structure
        Transform old = transform.Find("Structure");
        if (old != null)
        {
            Destroy(old.gameObject);
        }
        GameObject instance = Instantiate(data.prefab, transform);
        instance.name = "Structure";
        instance.transform.localPosition = data.yOffset;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
    }
    public void SetStructure(StructureData data)
    {
        if (data == null) return;
        structureIndex = -1;
        StructureName = data.structureName;
    }
    public StructureData GetStructureData()
    {
        var generator = GetComponentInParent<MapGenerator>();
        if (generator == null || generator.structureDatabase == null || string.IsNullOrEmpty(StructureName))
        {
            return null;
        }
        return generator.structureDatabase.GetByName(StructureName);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Show axial (q, r) above tile
        //GUIStyle style = new() { normal = { textColor = Color.white }, fontSize = 12 };
        //Handles.Label(transform.position + Vector3.up * 0.3f, $"({q}, {r})", style);
        foreach (HexTile neighbour in neighbours)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, 0.1f);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, neighbour.transform.position);
        }
    }
#endif

    //private Material originalMaterial;
    //private static Material highlightMaterial;
    //public void Highlight(bool on)
    //{
    //    Renderer renderer = GetComponentInChildren<Renderer>();
    //    if (renderer == null)
    //    {
    //        return;
    //    }
    //    if (highlightMaterial == null)
    //    {
    //        //yellow highlight
    //        highlightMaterial = new Material(Shader.Find("Standard"))
    //        {
    //            color = new Color(1, 1, 0, 0.3f) // Yellow, semi-transparent
    //        };
    //    }

    //    if (on)
    //    {
    //        if (originalMaterial == null)
    //            originalMaterial = renderer.material;
    //        renderer.material = highlightMaterial;
    //    }
    //    else
    //    {
    //        if (originalMaterial != null)
    //            renderer.material = originalMaterial;
    //    }
    //}
#if UNITY_EDITOR
    private void HandleStructureComponent()
    {
#if UNITY_EDITOR
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }
        if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
        {
            return;
        }
#endif
        var structureTile = GetComponent<StructureTile>();

        if (tileType == TileType.Structure)
        {
            if (structureTile == null)
            {
                gameObject.AddComponent<StructureTile>();
            }
        }
        else
        {
            if (!Application.isPlaying && structureTile != null && !EditorApplication.isUpdating)
            {
                DestroyImmediate(structureTile, true);
            }
        }

        // Force inspector refresh
        EditorUtility.SetDirty(this);
    }
#endif
    // Runtime structure placement (used by MapGenerator.LoadMapData)
    public void ApplyStructureByName(string name)
    {
        StructureName = name;
        var structure = GetComponent<StructureTile>();
        if (structure != null)
        {
            structure.ApplyStructure();
        }
    }

    public bool IsWalkable => true;
    //public bool IsWalkable
    //{
    //    get
    //    {
    //        //putt condition here
    //        //if(tile has an unwalkable object)
    //        //return false

    //        //otherwise it is true
    //        return true;
    //    }
    //}
}

