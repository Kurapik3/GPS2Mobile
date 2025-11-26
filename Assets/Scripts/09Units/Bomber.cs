using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomber : UnitBase
{
    //public override void Attack(HexTile target)
    //{
    //    // Do main attack
    //    base.Attack(target);
    //    ManagerAudio.instance.PlaySFX("BomberBombing");

    //    int splashDamage = Mathf.FloorToInt(attack * 0.5f);

    //    // Get all tiles in radius 1 around the target
    //    List<HexTile> splashTiles = MapManager.Instance.GetNeighborsWithinRadius(target.q, target.r,1 );

    //    foreach (HexTile tile in splashTiles)
    //    {
    //        // Skip the main target tile
    //        if (tile == target)
    //            continue;

    //        // Splash enemy unit
    //        if (tile.currentEnemyUnit != null)
    //        {
    //            tile.currentEnemyUnit.TakeDamage(splashDamage);
    //            Debug.Log($"{unitName} dealt {splashDamage} splash damage to {tile.currentEnemyUnit.unitType}");
    //        }

    //        // Splash enemy base
    //        if (tile.currentEnemyBase != null)
    //        {
    //            tile.currentEnemyBase.TakeDamage(splashDamage);
    //            Debug.Log($"{unitName} dealt {splashDamage} splash damage to an enemy base!");
    //        }

    //        // Splash sea monster
    //        if (tile.currentSeaMonster != null)
    //        {
    //            tile.currentSeaMonster.TakeDamage(splashDamage);
    //            Debug.Log($"{unitName} dealt {splashDamage} splash damage to a Sea Monster!");
    //        }
    //    }
    //    HideAttackIndicators();
    //}

    protected override IEnumerator PerformAttack(HexTile target)
    {
        int splashDamage = Mathf.FloorToInt(attack * 0.5f);

        ManagerAudio.instance.PlaySFX("BomberBombing");
        yield return PlayAttackAnimation(target, true, 1);
        ApplySplashDamage(target, splashDamage);
    }

    private void ApplySplashDamage(HexTile target, int splashDamage)
    {
        //Get all tiles in radius 1 around the target
        List<HexTile> splashTiles = MapManager.Instance.GetNeighborsWithinRadius(target.q, target.r, 1);

        foreach (HexTile tile in splashTiles)
        {
            // Skip the main target tile
            if (tile == null || tile == target)
                continue;

            // Splash enemy unit
            if (tile.currentEnemyUnit != null)
            {
                tile.currentEnemyUnit.TakeDamage(splashDamage);
                Debug.Log($"{unitName} dealt {splashDamage} splash damage to {tile.currentEnemyUnit.unitType}");
            }

            // Splash enemy base
            if (tile.currentEnemyBase != null)
            {
                tile.currentEnemyBase.TakeDamage(splashDamage);
                Debug.Log($"{unitName} dealt {splashDamage} splash damage to an enemy base!");
            }

            // Splash sea monster
            if (tile.currentSeaMonster != null)
            {
                tile.currentSeaMonster.TakeDamage(splashDamage);
                Debug.Log($"{unitName} dealt {splashDamage} splash damage to a Sea Monster!");
            }
        }
    }
}
