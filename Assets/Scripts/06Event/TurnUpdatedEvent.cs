using UnityEngine;

public class TurnUpdatedEvent
{
    public int currentTurn;
    public int maxTurns;

    public TurnUpdatedEvent(int current, int max)
    {
        currentTurn = current;
        maxTurns = max;
    }
}
