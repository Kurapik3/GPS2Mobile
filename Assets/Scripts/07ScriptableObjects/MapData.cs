using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "TileGen/MapData")]
public class MapData : ScriptableObject
{
    public List<HexTileData> tiles = new List<HexTileData>();
    public List<Vector2Int> revealedTiles = new List<Vector2Int>();
    public List<DynamicObjectData> dynamicObjects = new();
}

[System.Serializable]
public class DynamicObjectData
{
    public int q;
    public int r;
    public string resourceId;
}