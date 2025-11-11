using UnityEngine;

public class Tanker : UnitBase
{
    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount);

        if (hp <= 0) return; // dead can't counterattack

        // need last attack 
        //UnitBase attacker = 

        //if (attacker == null || attacker.currentTile == null) return;

        //int dist = HexDistance(currentTile.q, currentTile.r, attacker.currentTile.q, attacker.currentTile.r);

        //if (dist <= range)
        //{
        //    Debug.Log($"{unitName} counterattacks {attacker.unitName}!");
        //    attacker.TakeDamage(attack);
        //}
    }
}
