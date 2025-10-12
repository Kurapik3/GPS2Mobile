using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Combat behavior for enemy units in Aggressive state.
/// Prioritizes player bases, then player units. 60% chance to attack if a target is in range.
/// If no targets, moves (70% towards origin, 30% away).
/// </summary>
public class CombatAI : ISubAI
{
    private IAIContext context;
    private IAIActor actor;
    private System.Random rng = new System.Random();

    public void Initialize(IAIContext context, IAIActor actor)
    {
        this.context = context;
        this.actor = actor;
    }

    public void Execute()
    {
        var units = context.GetOwnedUnitIds();
        if (units == null || units.Count == 0)
            return;

        foreach (var unitId in units)
        {
            //Skip dormant (invisible) units
            if (!context.IsUnitVisibleToPlayer(unitId))
                continue;

            string unitType = context.GetUnitType(unitId);
            Vector3 pos = context.GetUnitPosition(unitId);
            int attackRange = context.GetUnitAttackRange(unitId);

            //Skip combat for Builder & Scout
            if (unitType == "Builder" || unitType == "Scout")
            {
                MoveIdle(unitId, pos);
                Debug.Log($"[CombatAI] {unitType} #{unitId} cannot attack, moving instead.");
                continue;

            }

            //Check for player bases in range first
            List<int> targets = context.GetEnemiesInRange(pos, attackRange); //"Enemies" means player entities from enemy AI POV
            if (targets != null && targets.Count > 0)
            {
                //Choose priority: prefer bases (if possible)
                int selected = ChoosePriorityTarget(targets, pos);

                //60% chance to attack
                if (rng.NextDouble() < 0.6)
                {
                    actor.AttackTarget(unitId, selected);
                    Debug.Log($"[CombatAI] Unit {unitType} #{unitId} attacks entity {selected}");
                    continue;
                }
                else
                {
                    Debug.Log($"[CombatAI] Unit {unitType} #{unitId} rolled to not attack");
                }
            }

            //If no valid target or skipped attack: decide movement (70% towards origin)
            MoveIdle(unitId, pos);
        }
    }

    private void MoveIdle(int unitId, Vector3 pos)
    {
        Vector2Int currentHex = context.WorldToHex(pos);
        int moveRange = context.GetUnitMoveRange(unitId);
        List<Vector2Int> reachableHexes = context.GetReachableHexes(currentHex, moveRange);
        reachableHexes.RemoveAll(hex => !MapManager.Instance.CanUnitStandHere(hex));

        if (reachableHexes.Count == 0)
            return;

        bool moveToOrigin = rng.NextDouble() < 0.7;
        Vector2Int originHex = Vector2Int.zero;
        Vector2Int targetHex = ChooseTargetHex(reachableHexes, currentHex, originHex, moveToOrigin);
        Vector3 destination = context.HexToWorld(targetHex);

        actor.MoveTo(unitId, destination);
        Debug.Log($"[CombatAI] Unit {unitId} moves {(moveToOrigin ? "towards" : "away from")} origin to {destination}");
    }

    //Chooses the most appropriate target —> prioritize bases, then units
    private int ChoosePriorityTarget(List<int> candidateIds, Vector3 fromPos)
    {
        int? baseTarget = null;
        int? unitTarget = null;
        float minBaseDistance = float.MaxValue;
        float minUnitDistance = float.MaxValue;

        foreach (var id in candidateIds)
        {
            Vector3 enemyPos = context.GetEnemyPosition(id);
            float distance = Vector3.Distance(fromPos, enemyPos);

            //Base priority
            if (context.GetPlayerBaseIds().Contains(id))
            {
                if (distance < minBaseDistance)
                {
                    minBaseDistance = distance;
                    baseTarget = id;
                }
            }
            //Unit secondary priority
            else if (context.GetPlayerUnitIds().Contains(id))
            {
                if (distance < minUnitDistance)
                {
                    minUnitDistance = distance;
                    unitTarget = id;
                }
            }
        }

        //Prefer base > unit > fallback
        if (baseTarget.HasValue)
            return baseTarget.Value;
        if (unitTarget.HasValue)
            return unitTarget.Value;

        return candidateIds[0]; //fallback
    }

    private Vector2Int ChooseTargetHex(List<Vector2Int> candidates, Vector2Int current, Vector2Int origin, bool moveTowards)
    {
        List<Vector2Int> valid = new List<Vector2Int>();
        int currentDist = context.GetHexDistance(current, origin);

        foreach (var hex in candidates)
        {
            int d = context.GetHexDistance(hex, origin);
            if (moveTowards && d < currentDist) valid.Add(hex);
            if (!moveTowards && d > currentDist) valid.Add(hex);
        }

        //Fallback
        if (valid.Count == 0)
            valid = candidates;

        return valid[rng.Next(valid.Count)];
    }
}
