using UnityEngine;

public class TileSelectedEvent
{
    public HexTile Tile { get; }
    public TileSelectedEvent(HexTile tile)
    {
        Tile = tile;
    }
}

public class TileDeselectedEvent
{
    public HexTile Tile { get; }
    public TileDeselectedEvent(HexTile tile)
    {
        Tile = tile;
    }
}
