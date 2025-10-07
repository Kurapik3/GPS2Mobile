using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Provides world and game-state data for the enemy AI to make decisions.
/// Implemented by the game layer, read-only for AI logic.
/// </summary>
public interface IAIContext
{
    //==================== Units ====================
    List<int> GetOwnedUnitIds(); //All owned units
    Vector3 GetUnitPosition(int unitId);
    string GetUnitType(int unitId);
    float GetUnitAttackRange(int unitId);
    bool IsUnitVisibleToPlayer(int unitId); //Drives Dormant -> Aggressive switch


    //==================== Bases ====================
    List<int> GetOwnedBaseIds(); //All owned Bases
    Vector3 GetBasePosition(int baseId); //Base position
    int GetBaseHP(int baseId);
    bool CanProduceUnit(int baseId); //Check whether the Base can produce unit
    bool CanUpgradeBase(int baseId); //Check whether the Base can be upgraded
    int GetBaseUnitCount(int baseId); //Returns how many units are currently stationed or linked to the base
    bool IsBaseOccupied(int baseId);

    //==================== Structure Tiles Objects ====================
    List<Vector3> GetRuinLocations(); //All _KNOWN_ Ruin positions
    List<Vector3> GetCacheLocations(); //All _KNOWN_ Cache positions
    List<Vector3> GetUnexploredTiles();

    //==================== Enemy(/Player) Info ====================
    bool IsEnemyNearby(Vector3 position, float range); //Check if any enemy is within range
    Vector3 GetNearestEnemy(Vector3 fromPosition); //Get the nearest enemy position from a given location
    List<int> GetEnemiesInRange(Vector3 position, float range); //Get all enemy IDs within attack range
    Vector3 GetEnemyPosition(int enemyId); //Get position of a specific enemy unit or Base (especially for combat)
    List<int> GetPlayerBaseIds(); //All _KNOWN_ player bases
    List<int> GetPlayerUnitIds(); //All _KNOWN_ player units


    //==================== Turn Info ====================
    int GetTurnNumber(); //Current turn number
}
