using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the overall AI turn flow:
/// - Base management (spawn/upgrade)
/// - Exploration & Combat logic
/// - Unlocks new unit types as turns progress
/// </summary>
public class AIController
{
    private IAIContext context;
    private IAIActor actor;

    public List<ISubAI> subAIs;

    public AIController(IAIContext context, IAIActor actor)
    {
        this.context = context;
        this.actor = actor;

        subAIs = new List<ISubAI>
        {
            new EnemyBaseAI(),
            new ExplorationAI(),
            new CombatAI()
        };

        foreach (var subAI in subAIs)
            subAI.Initialize(context, actor);
    }

    public void ExecuteTurn()
    {
        int turn = context.GetTurnNumber();
        Debug.Log($"<color=yellow>=== [AIController] Enemy Turn {turn} Start ===</color>");

        //    //Base Phase
        //    baseAI.Execute();

        //    //Determine unit states
        //    var dormantUnits = new List<int>();
        //    var aggressiveUnits = new List<int>();

        //    foreach (var unitId in context.GetOwnedUnitIds())
        //    {
        //        bool isVisible = context.IsUnitVisibleToPlayer(unitId);
        //        if (isVisible)
        //            aggressiveUnits.Add(unitId);
        //        else
        //            dormantUnits.Add(unitId);
        //    }

        //    Debug.Log($"[AIController] Dormant: {dormantUnits.Count}, Aggressive: {aggressiveUnits.Count}");

        //    //Execute AI behaviours
        //    if (dormantUnits.Count > 0)
        //        explorationAI.Execute();

        //    if (aggressiveUnits.Count > 0)
        //        combatAI.Execute();

        //    //End Turn
        //    actor.EndTurn();
        //    Debug.Log("<color=yellow>=== [AIController] Enemy Turn End ===</color>");
        //}

        foreach (var subAI in subAIs)
        {
            Debug.Log($"[AIController] Executing {subAI.GetType().Name}...");
            subAI.Execute();
        }

        actor.EndTurn();
        Debug.Log("<color=yellow>=== [AIController] Enemy Turn End ===</color>");
    }
}
