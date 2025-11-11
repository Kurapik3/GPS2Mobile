using UnityEngine;
using System.Collections.Generic;

public class Bomber : UnitBase
{
    public override void Attack(UnitBase target)
    {
        base.Attack(target);

        if (target.hp <= 0) return;

        List<UnitBase> allUnits = UnitManager.Instance.GetAllUnits();
        foreach (var unit in allUnits)
        {
            if (unit == target || unit == this || unit.currentTile == null) continue;

            int dist = HexDistance(target.currentTile.q, target.currentTile.r, unit.currentTile.q, unit.currentTile.r);

            // splash to 1-tile radius
            if (dist == 1)
            {
                int splash = Mathf.FloorToInt(attack * 0.5f);
                unit.TakeDamage(splash);
                Debug.Log($"{unitName} dealt {splash} splash damage to {unit.unitName}");
            }
        }
    }
}
