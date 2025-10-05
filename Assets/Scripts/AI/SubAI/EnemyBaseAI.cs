using UnityEngine;

public class EnemyBaseAI : ISubAI
{
    private IAIContext context;
    private IAIActor actor;
    private System.Random rng = new System.Random();

    public int Priority => 0; //Base take action before units

    public void Initialize(IAIContext context, IAIActor actor)
    {
        this.context = context;
        this.actor = actor;
    }

    public void Execute()
    {
        var baseIds = context.GetOwnedBaseIds();
        if (baseIds == null || baseIds.Count == 0) return;

        foreach (var baseId in baseIds)
        {
            int currentHP = context.GetBaseHP(baseId);
            if (currentHP <= 0)
            {
                Debug.Log($"[EnemyBaseAI] Base {baseId} destroyed, skipping.");
                continue;
            }

            //Checks whether the base currently houses fewer than 3 units
            if (context.CanProduceUnit(baseId))
            {
                string unitType = rng.NextDouble() < 0.5 ? "Shooter" : "Scout";
                actor.ProduceUnit(baseId, unitType);
                Debug.Log($"[EnemyBaseAI] Base {baseId} trains a new {unitType}");
            }
            else
            {
                Debug.Log($"[EnemyBaseAI] Base {baseId} already full (3 units).");
            }
        }
    }
}
