using System.Collections;
using UnityEngine;

public class Scout : UnitBase
{
    private int movesLeftThisTurn;
    private const int maxMovesPerTurn = 2;

    protected override void Start()
    {
        base.Start();
        Debug.Log($"{unitName} is a Scout unit ready to move! HP:{hp}, Attack:{attack}, Movement:{movement}");
        ResetMove();
    }
    public override void ResetMove()
    {
        movesLeftThisTurn = maxMovesPerTurn;
        hasMovedThisTurn = false;
        HasAttackThisTurn = false;
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
        hasMovedThisTurn = movesLeftThisTurn <= 0;

        // Refresh indicators if moves remain
        if (movesLeftThisTurn > 0)
        {
            ShowRangeIndicators();
        }
    }
    protected override IEnumerator PerformAttack(HexTile target)
    {
        yield return PlayAttackAnimation(target, true);
    }
}
