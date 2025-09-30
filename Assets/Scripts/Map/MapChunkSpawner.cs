using UnityEngine;

public class MapChunkSpawner : MonoBehaviour
{
    [SerializeField] private ChunkData chunkData;
    [SerializeField] private HexTileGenerationSettings generationSettings;
    [SerializeField] private float hexSize = 1f;

    public void SpawnChunk(Vector3 startPos)
    {
        foreach (var record in chunkData.tiles)
        {
            GameObject prefab = generationSettings.GetTile(record.type);
            if (prefab != null)
            {
                Vector3 pos = startPos + HexToWorld(record.q, record.r, hexSize);
                GameObject go = Instantiate(prefab, pos, Quaternion.identity, transform);

                // Attach HexTile if not present
                HexTile tile = go.GetComponent<HexTile>();
                if (tile == null)
                {
                    tile = go.AddComponent<HexTile>();
                }
                tile.q = record.q;
                tile.r = record.r;
                tile.tileType = record.type;
            }
        }
    }
    //give world positions for spawning
    private Vector3 HexToWorld(int q, int r, float size)
    {
        float x = size * (Mathf.Sqrt(3) * q + Mathf.Sqrt(3) / 2f * r);
        float z = size * (3f / 2f * r);
        return new Vector3(x, 0, z);
    }
    /*
    private void OnEnable()
    {
        //LayoutGrid();
    }
    
    public void LayoutGrid()
    {
        //Clear();
        for (int y = 0; y < mapSize.y; y++)
        {
            for (int x = 0; x < mapSize.x; x++)
            {
                GameObject chunk = GameObject.Instantiate(MapChunkSpawner.GetRandom());
                chunk.name = $"Chunk (C{x},R{y})";
                chunk.transform.position = GetPositionForHexFromCoordinate(chunkSize * x, chunkSize * y);
                chunk.SetParent(transform, true);

                Vector2Int chunkCornerCoordinate = new Vector2Int(chunkSize * x, chunkSize * y);
                //offset all the children by the cube coordinate
                foreach(Transform child in chunk.transform)
                {
                    HexTile tile = child.GetComponent<HexTile>();
                    tile.offsetCoordinate += new Vector2Int(chunkCornerCoordinate.x, chunkCornerCoordinate.y);
                    tile.cubeCoordinate = OffsetToCube(tile.offsetCoordinate);

                    tile.gameObject.name = $"Hex (C{tile.offsetCoordinate.x},R{tile.offsetCoordinate.y})";
                }
            }
        }
    }
    //Latr for pathfinding, this give cube coordinates
    public static Vector3Int OffsetToCube(Vector2Int offset)
    {
        var q = offset.x - (offset.y + (offset.y % 2)) / 2;
        var r = offset.y;
        return new Vector3Int(q, r, -q - r);
    }
    */
}
