using UnityEngine;

[System.Serializable]

public struct HexTileData
{
    public int q;
    public int r;
    public HexTile.TileType tileType;
    public bool hasStructure;
    public string structureName;

    public HexTileData Clone()
    {
        return new HexTileData
        {
            q = this.q,
            r = this.r,
            tileType = this.tileType,
            hasStructure = this.hasStructure,
            structureName = this.structureName
        };
    }

}