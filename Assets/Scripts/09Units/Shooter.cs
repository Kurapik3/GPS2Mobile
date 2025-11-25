using System.Collections;
using UnityEngine;

public class Shooter : UnitBase
{
    //public override void Attack(HexTile target)
    //{
    //    int distance = HexDistance(currentTile.q, currentTile.r, target.q, target.r);

    //    if (distance < 2)
    //    {
    //        Debug.Log($"{unitName} cannot attack enemies within 2 tiles! Distance: {distance}");
    //        return;
    //    }

    //    base.Attack(target);
    //    ManagerAudio.instance.PlaySFX("ShooterShooting");
    //}

    protected override IEnumerator PerformAttack(HexTile target)
    {
        int distance = HexDistance(currentTile.q, currentTile.r, target.q, target.r);
        if (distance < 2)
        {
            Debug.Log($"{unitName} cannot attack enemies within 2 tiles!");
            yield break;
        }

        ManagerAudio.instance.PlaySFX("ShooterShooting");
        yield return PlayAttackAnimation(target, true);
    }
}
