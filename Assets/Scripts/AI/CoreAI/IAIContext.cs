using UnityEngine;
using System.Collections.Generic;

public interface IAIContext
{
    //==================== Units ====================
    List<int> GetOwnedUnitIds(); //All owned units
    Vector3 GetUnitPosition(int unitId);
    string GetUnitType(int unitId);

    //==================== Bases ====================
    List<int> GetOwnedBaseIds(); //All owned Bases
    Vector3 GetBasePosition(int baseId); //Base position
    int GetBaseHP(int baseId);
    bool CanProduceUnit(int baseId); //Check whether the Base can produce unit
    bool CanUpgradeBase(int baseId); //Check whether the Base can be upgraded

    //==================== Structure Tiles Objects ====================
    List<Vector3> GetRuinLocations(); //All _KNOWN_ Ruin positions
    List<Vector3> GetCacheLocations(); //All _KNOWN_ Cache positions
    List<Vector3> GetUnexploredTiles();

    //==================== Enemy(/Player) Info ====================
    bool IsEnemyNearby(Vector3 position, float range); //Check if any enemy is within range
    Vector3 GetNearestEnemy(Vector3 fromPosition); //Get the nearest enemy position from a given location
    List<int> GetEnemiesInRange(Vector3 position, float range); //Get all enemy IDs within attack range
    Vector3 GetEnemyPosition(int enemyId); //Get position of a specific enemy unit or Base (especially for combat)

    //==================== Turn Info ====================
    int GetTurnNumber(); //Current turn number
}
