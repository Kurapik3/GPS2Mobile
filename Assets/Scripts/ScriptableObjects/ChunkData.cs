using UnityEngine;
[CreateAssetMenu(menuName = "TileGen/ChunkData")]
public class ChunkData : ScriptableObject
{
    public int width;
    public int height;

    [System.Serializable]
    public struct TileRecord
    {
        public HexTile.TileType type;
        public int q;
        public int r;
    }

    public TileRecord[] tiles;
}
