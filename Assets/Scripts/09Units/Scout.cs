using UnityEngine;
public class Scout : UnitBase
{
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

    public override void TryMove(HexTile targetTile)
    {
        if (movesLeftThisTurn <= 0)
        {
            Debug.Log($"{unitName} has no moves left this turn!");
            return;
        }

        base.TryMove(targetTile);
        movesLeftThisTurn--; 
    }

}
