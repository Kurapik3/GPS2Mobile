using UnityEngine;

public class EnemyBaseAI : ISubAI
{
    private IAIContext context;
    private IAIActor actor;
    private int currentTurn;

    public int Priority => 0; //Base take action before units

    public void Initialize(IAIContext context, IAIActor actor)
    {
        this.context = context;
        this.actor = actor;
    }

    public void Execute()
    {
        var baseIds = context.GetOwnedBaseIds();
        if (baseIds == null || baseIds.Count == 0) 
            return;

        currentTurn = context.GetTurnNumber();

        foreach (var baseId in baseIds)
        {
            int currentHP = context.GetBaseHP(baseId);
            if (currentHP <= 0)
            {
                Debug.Log($"[EnemyBaseAI] EnemyBase {baseId} destroyed, skipping.");
                continue;
            }

            //Skip spawning if the base already has a unit stationed.
            if (context.IsBaseOccupied(baseId))
            {
                Debug.Log($"[EnemyBaseAI] EnemyBase {baseId} occupied, skip spawning.");
                continue;
            }

            //Checks whether the base currently houses fewer than 3 units
            if (!context.CanProduceUnit(baseId))
            {
                Debug.Log($"[EnemyBaseAI] EnemyBase {baseId} can't produce unit yet.");
                continue;
            }

            string unitType = GetUnlockedUnitType(currentTurn);
            actor.SpawnUnit(baseId, unitType);
            Debug.Log($"[EnemyBaseAI] EnemyBase {baseId} spawned {unitType}.");
        }
    }

    private string GetUnlockedUnitType(int turn)
    {
        string[] available;

        if (turn < 3)
            available = new[] { "Builder" };
        else if (turn < 5)
            available = new[] { "Builder", "Scout" };
        else if (turn < 8)
            available = new[] { "Builder", "Scout", "Tanker" };
        else if (turn < 12)
            available = new[] { "Builder", "Scout", "Tanker", "Shooter" };
        else
            available = new[] { "Builder", "Scout", "Tanker", "Shooter", "Bomber" };

        return available[Random.Range(0, available.Length)];
    }
}
