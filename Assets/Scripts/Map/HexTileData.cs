using UnityEngine;

[System.Serializable]

public struct HexTileData
{
    public int q;
    public int r;
    public HexTile.TileType tileType;
    public bool hasStructure;
    public string structureName;
}