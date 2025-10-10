using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "TileGen/MapData")]
public class MapData : ScriptableObject
{
    public List<HexTileData> tiles = new List<HexTileData>();
}
