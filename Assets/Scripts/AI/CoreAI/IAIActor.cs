using UnityEngine;

public interface IAIActor
{
    //==================== Explore ====================
    void MoveTo(int unitId, Vector3 destination); //Move a unit to a specific position

    //==================== Expand ====================
    void RebuildRuin(Vector3 location); //Rebuild a Ruin into a Base
    void UpgradeBase(int baseId);

    //==================== Exploit ====================
    void SpawnUnit(int baseId, string unitType);

    //==================== Exterminate ====================
    void AttackTarget(int unitId, int targetId); //Attack enemy unit or base
    void RetreatTo(int unitId, Vector3 safeLocation); //Retreat unit to a safe place

    //==================== TurnControl ====================
    void EndTurn();
}
