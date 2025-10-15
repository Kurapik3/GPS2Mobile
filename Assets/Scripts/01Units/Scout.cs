using UnityEngine;
using System.Collections.Generic;
public class Scout : UnitBase
{
    [SerializeField] private int fogRevealRadius = 1;
    private int movesLeftThisTurn;
    protected override void Start()
    {
        base.Start();
        Debug.Log($"{unitName} is a Builder unit ready to build! \nHP:{hp}, Attack:{attack}, Movement:{movement}");
        ResetMoves();
    }

    public void OnTurnStart()
    {
        ResetMoves();
    }

    private void ResetMoves()
    {
        movesLeftThisTurn = 2; // Scout can move twice per turn
    }

    public override void Move(HexTile targetTile)
    {
        if (movesLeftThisTurn <= 0)
        {
            Debug.Log($"{unitName} has no moves left this turn!");
            return;
        }

        if (currentTile != null)
        {
            RevealNearbyFog(currentTile);
        }

        base.Move(targetTile);

        movesLeftThisTurn--;
        Debug.Log($"{unitName} moved. Moves left: {movesLeftThisTurn}");
    }

    private void RevealNearbyFog(HexTile centerTile)
    {
        
        List<HexTile> nearbyTiles = GetTilesInRange(centerTile.q, centerTile.r, fogRevealRadius);

        foreach (HexTile tile in nearbyTiles)
        {
            if (tile.fogInstance != null)
            {
                tile.RemoveFog();
                PlayerTracker.Instance.addScore(50);
            }
        }

        Debug.Log($"{unitName} revealed fog around tile ({centerTile.q}, {centerTile.r})");
    }

    private List<HexTile> GetTilesInRange(int q, int r, int radius)
    {
        List<HexTile> tilesInRange = new List<HexTile>();

        foreach (HexTile tile in MapManager.Instance.GetTiles()) 
        {
            int dist = HexDistance(q, r, tile.q, tile.r);
            if (dist <= radius)
                tilesInRange.Add(tile);
        }

        return tilesInRange;
    }

}
