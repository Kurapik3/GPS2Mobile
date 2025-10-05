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

    public int Priority => 2; //After exploration

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

            Vector3 pos = context.GetUnitPosition(unitId);
            float attackRange = context.GetUnitAttackRange(unitId);

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
                    Debug.Log($"[CombatAI] Unit {unitId} attacks entity {selected}");
                    continue;
                }
                else
                {
                    Debug.Log($"[CombatAI] Unit {unitId} rolled to not attack");
                }
            }

            //If no valid target or skipped attack: decide movement (70% towards origin)
            bool moveToOrigin = rng.NextDouble() < 0.7;
            Vector3 dir = (moveToOrigin ? (Vector3.zero - pos) : (pos - Vector3.zero)).normalized;
            Vector3 destination = pos + dir * 1f;
            actor.MoveTo(unitId, destination);
            Debug.Log($"[CombatAI] Unit {unitId} moves {(moveToOrigin ? "towards" : "away from")} origin to {destination}");
        }
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
}
