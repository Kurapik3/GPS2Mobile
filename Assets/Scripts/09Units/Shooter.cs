using UnityEngine;

public class Shooter : UnitBase
{
    public override void Attack(HexTile target)
    {
        int distance = HexDistance(currentTile.q, currentTile.r, target.q, target.r);

        if (distance < 2)
        {
            Debug.Log($"{unitName} cannot attack targets within 2 tiles! Distance: {distance}");
            return;
        }

        base.Attack(target);
    }
}
