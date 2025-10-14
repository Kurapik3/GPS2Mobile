using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBaseAI : ISubAI
{
    private IAIContext context;
    private IAIActor actor;
    private AIUnlockSystem unlockSystem;
    private float delay = 1f;

    public void Initialize(IAIContext context, IAIActor actor)
    {
        this.context = context;
        this.actor = actor;

        unlockSystem = Object.FindFirstObjectByType<AIUnlockSystem>();
        if (unlockSystem == null)
            Debug.LogError("[EnemyBaseAI] No AIUnlockSystem found in scene!");
    }

    public void SetUnlockSystem(AIUnlockSystem system)
    {
        unlockSystem = system;
    }

    public IEnumerator ExecuteStepByStep()
    {
        var baseIds = context.GetOwnedBaseIds();
        if (baseIds == null || baseIds.Count == 0)
            yield break;;

        int currentTurn = context.GetTurnNumber();

        if (unlockSystem != null)
            unlockSystem.UpdateUnlocks(currentTurn);

        float spawnProbability = 0.45f;

        foreach (var baseId in baseIds)
        {
            int currentHP = context.GetBaseHP(baseId);
            if (currentHP <= 0)
            {
                Debug.Log($"[EnemyBaseAI] EnemyBase {baseId} destroyed, skipping.");
                continue;
            }

            if (Random.value > spawnProbability)
            {
                Debug.Log($"[EnemyBaseAI] Base {baseId} skipped spawning this turn (p={spawnProbability}).");
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

            //Get unlocked unit list
            List<string> availableUnits = unlockSystem.GetUnlockedUnits();
            if (availableUnits == null || availableUnits.Count == 0)
            {
                Debug.Log("[EnemyBaseAI] No unlocked units available for spawning.");
                continue;
            }

            //Randomly select one unlocked unit type to spawn
            string chosenUnit = availableUnits[Random.Range(0, availableUnits.Count)];

            actor.SpawnUnit(baseId, chosenUnit);
            Debug.Log($"[EnemyBaseAI] EnemyBase {baseId} spawned {chosenUnit} (Turn {currentTurn}).");

            yield return new WaitForSeconds(delay / AIController.AISpeedMultiplier);
        }
    }
}