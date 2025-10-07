using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Exploration behavior for enemy units in Dormant state.
/// Moves units randomly: 50% towards origin, 50% away.
/// Executed before CombatAI so movement is visible to Combat decisions.
/// </summary>
public class ExplorationAI : ISubAI
{
    private IAIContext context;
    private IAIActor actor;
    private System.Random rng = new System.Random();

    public int Priority => 1; //After base production

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
            //Skip units that are already aggressive (visible to player)
            if (context.IsUnitVisibleToPlayer(unitId)) 
                continue;

            Vector3 pos = context.GetUnitPosition(unitId);

            //50% towards origin (0,0), 50% away
            bool moveTowards = rng.NextDouble() < 0.5;
            Vector3 dir = (moveTowards ? (Vector3.zero - pos) : (pos - Vector3.zero)).normalized;

            //Move roughly 1 tile
            Vector3 destination = pos + dir * 1f;

            actor.MoveTo(unitId, destination);

            Debug.Log($"[ExplorationAI] Unit {unitId} moving {(moveTowards ? "towards" : "away from")} origin to {destination}");
        }
    }
}
