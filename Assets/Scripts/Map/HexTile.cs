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
        Development
    }

    [Header("References")]
    [SerializeField] public HexTileGenerationSettings settings;

    [Header("Hex Coordinates (Axial)")]
    public int q;
    public int r;

    [Header("Tile Data")]
    [SerializeField] public TileType tileType;
    public GameObject tile;
    //public GameObject fow; //"fog of war"
    //public Vector2Int offsetCoordinate;
    //public Vector3Int cubeCoordinate;
    public List<HexTile> neighbours = new();
    private bool isDirty = false;
    //Computed properties
    public Vector2Int OffsetCoord => new Vector2Int(q + (r + (r % 2)) / 2, r);
    public Vector3Int CubeCoord => new Vector3Int(q, -q - r, r);

    private void OnValidate()
    {
        // Only trigger update if we have settings assigned
        if (settings == null)
        {
            return;
        }
        // Mark dirty whenever tileType is changed in inspector
        isDirty = true;
    }

    private void Update()
    {
        if (!isDirty) return;

        // Destroy old visual if it exists
        if (tile != null)
        {
            if (Application.isPlaying)
            { 
                Destroy(tile); 
            }
            else
            { 
                DestroyImmediate(tile);
            }
        }
        // Spawn new one
        AddTile();

        isDirty = false;
    }
 
    public void RollTileType()
    {
        tileType = (TileType)Random.Range(0, System.Enum.GetValues(typeof(TileType)).Length);
        isDirty = true;
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
        tile = Application.isPlaying?Instantiate(prefab, transform):(GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
        tile.transform.localPosition = Vector3.zero;
        tile.transform.localRotation = Quaternion.identity;
        tile.transform.localScale = Vector3.one;

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
            Vector2Int key = new Vector2Int(q + dir.x, r + dir.y);
            if (MapGenerator.AllTiles.TryGetValue(key, out HexTile neighbor))
            {
                neighbours.Add(neighbor);
            }
        }
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

    private Material originalMaterial;
    private static Material highlightMaterial;
    public void Highlight(bool on)
    {
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            return;
        }
        if (highlightMaterial == null)
        {
            //yellow highlight
            highlightMaterial = new Material(Shader.Find("Standard"))
            {
                color = new Color(1, 1, 0, 0.3f) // Yellow, semi-transparent
            };
        }

        if (on)
        {
            if (originalMaterial == null)
                originalMaterial = renderer.material;
            renderer.material = highlightMaterial;
        }
        else
        {
            if (originalMaterial != null)
                renderer.material = originalMaterial;
        }
    }
}

/*
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
[ExecuteInEditMode]
public class HexTile : MonoBehaviour
{
    public HexTileGenerationSettings settings;
    public HexTileGenerationSettings.TileType tileType;
    public GameObject tile;
    public GameObject fow;
    public Vector2Int offsetCoordinate;
    public Vector3Int cubeCoordinate;
    public List<HexTile> neighbours;
    private bool isDirty = false;
    private void OnValidate()
    {
        if (tile == null) 
        { 
            return;
        }
        isDirty = true;
    }
    private void Update()
    {
        if(!isDirty)
        {
            if(Application.isPlaying)
            {
                GameObject.Destroy(tile);
            }
            else
            {
                GameObject.DestroyImmediate(tile);
            }
            AddTile();
            isDirty = false;
        }
    }
    public void RollTileType()
    {
        tileType = (HexTileGenerationSettings.TileType)Random.Range(0, 3);
    }
    public void AddTile()
    {
        tile = GameObject.Instantiate(settings.GetTile(tileType));
        if(gameObject.GetComponent<MeshCollider>() == null )
        {
            MeshCollider collider = gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = GetComponentInChildren<MeshFilter>().mesh;
        }
        //transform.AddChild(tile);
    }

    public void OnDrawGizmosSelected()
    {
        foreach(HexTile neighbour in neighbours)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, 0.1f);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position,neighbour.transform.position);
        }
    }
}
*/