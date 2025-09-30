using System.Collections.Generic;

public class AIController
{
    private List<ISubAI> subAIs = new List<ISubAI>();

    public enum Difficulty { Easy, Normal, Hard }
    private Difficulty difficulty;

    public AIController(IAIContext context, IAIActor actor, Difficulty difficulty)
    {
        //======== Register subAIs (work in progress)
        subAIs.Add(new BasicAI(difficulty));

        //Initialize all subAIs
        foreach (var subAI in subAIs)
        {
            subAI.Initialize(context, actor);
        }
    }

    public void ExecuteTurn()
    {
        //Execute in priority order
        subAIs.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        foreach (var subAI in subAIs)
        {
            subAI.Execute();
        }
    }
}