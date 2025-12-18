using UnityEngine;

[System.Serializable]

public struct HexTileData
{
    public int q;
    public int r;
    public HexTile.TileType tileType;
    public bool hasStructure;
    public string structureName;

    public bool hasSavedBuilding;  // True if this is from a save file with building data
    public int buildingOwner;      // 0=none, 1=player, 2=enemy
    public int buildingLevel;
    public HexTileData Clone()
    {
        return new HexTileData
        {
            q = this.q,
            r = this.r,
            tileType = this.tileType,
            hasStructure = this.hasStructure,
            structureName = this.structureName,
            hasSavedBuilding = this.hasSavedBuilding,
            buildingOwner = this.buildingOwner,
            buildingLevel = this.buildingLevel
        };
    }

}