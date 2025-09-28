using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    /* 

    private void Awake()
    {
        instance = this;
        tiles = new Dictionary<Vector3Int, HexTile>();

        HexTile[] hexTiles = gameObject.GetComponentInChildren<HexTile>();
        //Register all the tiles
        foreach (HexTile hexTile in hexTiles)
        {
            RegisterTile(hexTile);
        }
        //Get each tiles set of neighbours
        foreach (HexTile hexTile in hexTiles)
        {
            List<HexTile> neighbours = GetNeighbours(hexTile);
            hexTile.neighbours = neighbours;
        }
    }
    ////Put the player somewhere
    //HexTile tile = hexTiles.GetRandom();
    //while (tile.tileType != HexTileGenerationSettings.TileType.Normal)
    //{
    //    tile = hexTiles.GetRandom();
    //}
    //playerPos = tile.cubeCoordinate;
    //player.transform.position = tile.transform.position + new Vector3(0,1f,0);
    //player.currenTile = tile;
    public void RegisterTile(HexTile tile)
    {
        tiles.Add(tile.cubeCoordinate, tile);
    }

    private List<HexTile> GetNeighbours(HexTile hexTile)
    {
        List<HexTile> neighbours = new List<HexTile>();
        Vector3Int[] neighbourCoords = new Vector3Int[]
        {
            new Vector3Int(1,-1,0),
            new Vector3Int(1,0,-1),
            new Vector3Int(0,1,-1),
            new Vector3Int(-1,1,0),
            new Vector3Int(-1,0,1),
            new Vector3Int(0,-1,1),
        };
        foreach(Vector3Int neighbourCoord in neighbourCoords)
        {
            Vector3Int tileCoord = tile.cubeCoordinate;
            if(tiles.TryGetValue(tileCoord + neighbourCoord,out HexTile neighbour))
            {
                neighbours.Add(neighbour);
            }
        }
        return neighbours;
    }
    */
}
