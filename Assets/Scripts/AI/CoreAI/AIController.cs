using System.Collections.Generic;
using UnityEngine;

public class AIController
{
    private List<ISubAI> subAIs = new List<ISubAI>();

    public AIController(IAIContext context, IAIActor actor)
    {
        //======== Register subAIs (work in progress)
        subAIs.Add(new EnemyBaseAI());
        subAIs.Add(new ExplorationAI());
        subAIs.Add(new CombatAI());

        //Initialize all subAIs
        foreach (var subAI in subAIs)
        {
            subAI.Initialize(context, actor);
        }
    }

    public void ExecuteTurn()
    {
        //Execute in priority order (lower = first)
        subAIs.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        foreach (var subAI in subAIs)
        {
            subAI.Execute();
        }

        Debug.Log("[AIController] Enemy turn finished.");
    }
}