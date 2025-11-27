using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using QuickOutlinePlugin;
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

    public UnitBase currentUnit; // add the by william, use in Building base to see if any unit is on hextile
    [HideInInspector] public bool isPlayerTurf = false; // william
    [SerializeField] public BuildingBase currentBuilding; // by william|

    [Header("Structure Data")]
    public int structureIndex = -1; // -1 = no structure
    public string StructureName;
    public bool HasStructure => structureIndex >= 0 || !string.IsNullOrEmpty(StructureName);

    private bool isDirty;

    public Vector2Int HexCoords => new Vector2Int(q, r);

    [Header("SeaMonsterSpawnLocation")]
    public bool HasTreeBase => currentBuilding is TreeBase;
    public EnemyBase currentEnemyBase;
    public bool HasEnemyBase => currentEnemyBase != null;
    public EnemyUnit currentEnemyUnit;
    public SeaMonsterBase currentSeaMonster;
    public FishTile fishTile;
    public DebrisTile debrisTile;
    public bool IsOccupied => HasStructure || HasDynamic;

    private bool isBlockedByTurtleWall = false;
    public bool IsBlockedByTurtleWall => isBlockedByTurtleWall;

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

    public void SetBuilding(BuildingBase building)
    {
        currentBuilding = building;
    }

    public void BecomeRuin()
    {
        Debug.Log($"Tile at ({q}, {r}) has become a ruin.");
        currentBuilding = null;
    }

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
        tile = Instantiate(prefab, transform);
        tile.name = "Mesh";
        tile.transform.localPosition = Vector3.zero;

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
    
    public void AddFog(GameObject fogPrefab, float yOffset = 0f)
    {
        if (fogInstance != null || fogPrefab == null)
        {
            return;
        }
        fogInstance = Instantiate(fogPrefab, transform);
        fogInstance.name = "Fog";
        fogInstance.transform.localPosition = new Vector3(0, yOffset, 0);
        SetContentsVisible(false);
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
        SetContentsVisible(true);
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

    public void ApplyStructureByName(string name)
    {
        var structureTile = GetComponent<StructureTile>();
        if (structureTile == null)
        {
            structureTile = gameObject.AddComponent<StructureTile>();
        }
        // Auto-load database if missing
        if (structureTile.structureDatabase == null)
        {
            structureTile.structureDatabase = Resources.Load<StructureDatabase>("StructureDatabase");
        }
        if (structureTile.structureDatabase == null)
        {
            Debug.LogWarning("StructureDatabase not found in Resources folder!");
            return;
        }

        int idx = structureTile.structureDatabase.structures.FindIndex(s => s.structureName == name);
        if (idx >= 0)
        {
            structureTile.selectedIndex = idx;
            structureTile.ApplyStructureRuntime();
        }
        else
        {
            Debug.LogWarning($"Structure '{name}' not found in database!");
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

    private bool unitOccupied = false;
    public bool IsOccupiedByUnit => unitOccupied;

    public void SetOccupiedByUnit(bool occupied)
    {
        unitOccupied = occupied;
    }

    public bool CanUnitStandHere()
    {
        return !unitOccupied;
    }

    public bool IsWalkableForAI()
    {
        return IsWalkable && !IsOccupiedByUnit;
    }

    public void SetBlockedByTurtleWall(bool blocked)
    {
        isBlockedByTurtleWall = blocked;
    }

    //For settings renderes on/off
    public void SetContentsVisible(bool visible)
    {
        // Override visibility if developer setting says to show everything
        if (MapVisibiilitySettings.Instance != null && MapVisibiilitySettings.Instance.showAllContents)
        {
            visible = true;
        }
        //For dynamic tile
        HideObjectsWithTagRecursive(transform, "Debris", visible);
        HideObjectsWithTagRecursive(transform, "Fish", visible);
        HideObjectsWithTagRecursive(transform, "Cache", visible);
        // For Grove
        HideObjectsWithTagRecursive(transform, "Grove", visible);
        // for Turf
        HideObjectsWithTagRecursive(transform, "TurfVisual", visible);
        //For structures using layer
        int structureLayer = LayerMask.NameToLayer("structure");
        foreach (Transform child in transform)
        {
            if (child.gameObject.layer == structureLayer)
            {
                ToggleRenderersAndColliders(child.gameObject, visible);
            }
        }
        // For enemies using tag
        HideObjectsWithTagRecursive(transform, "EnemyBase", visible);
        //For ruins using tag
        HideObjectsWithTagRecursive(transform, "Ruin", visible);
    }
    private void HideObjectsInLayerRecursive(Transform parent, int layer, bool visible)
    {
        foreach (Transform child in parent)
        {
            if (child.gameObject.layer == layer)
            {
                ToggleRenderersAndColliders(child.gameObject, visible);
            }
            HideObjectsInLayerRecursive(child, layer, visible);
        }
    }
    private void HideObjectsWithTagRecursive(Transform parent, string tag, bool visible)
    {
        foreach (Transform child in parent)
        {
            if (child.CompareTag(tag))
            {
                ToggleRenderersAndColliders(child.gameObject, visible);
            }
            HideObjectsWithTagRecursive(child, tag, visible);
        }
    }

    private void ToggleRenderersAndColliders(GameObject obj, bool visible)
    {
        foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = visible;
        }
        foreach (var collider in obj.GetComponentsInChildren<Collider>())
        {
            collider.enabled = visible;
        }
    }

    public void SetTurf(bool value)
    {
        isPlayerTurf = value;   
    }

    public void EnsureOutline(GameObject structure)
    {
        if (structure == null) return;

        Outline outline = structure.GetComponent<Outline>();
        if (outline == null)
        {
            outline = structure.AddComponent<Outline>();
            outline.OutlineMode = Outline.Mode.OutlineAll;
            outline.OutlineColor = Color.cyan; // You can customize
            outline.OutlineWidth = 5f;
        }

        outline.enabled = false; 
    }

    public void OnTileClicked()
    {
        //Select tile
        TileSelector.SelectTile(this);
        //Outline newOutline = currentBuilding != null ? currentBuilding.GetComponent<Outline>() : null;
        //Highlight structure
        //if (currentBuilding != null)
        //{
        //    //Outline outline = currentBuilding.GetComponent<Outline>();
        //    if (outline != null)
        //    {
        //        outline.enabled = true;
        //    }
        //}
        //if (TileSelector.PreviousOutline == newOutline)
        //{
        //    return;
        //}
        ////Disable prev outline
        //if (TileSelector.PreviousOutline != null)
        //{
        //    TileSelector.PreviousOutline.enabled = false;
        //}
        //if (newOutline != null)
        //{
        //    newOutline.enabled = true;
        //}
        //TileSelector.PreviousOutline = newOutline;
        //EventBus.Publish(new TileSelectedEvent(this));
    }
    //public void OnTileDeselected()
    //{
    //    //Remove tile highlight
    //    TileSelector.Hide();

    //    // Disable structure outline
    //    if (currentBuilding != null)
    //    {
    //        Outline outline = currentBuilding.GetComponent<Outline>();
    //        if (outline != null)
    //        {
    //            outline.enabled = false;
    //        }
    //    }

    //    // Clear previous outline tracker
    //    TileSelector.PreviousOutline = null;
    //}

}