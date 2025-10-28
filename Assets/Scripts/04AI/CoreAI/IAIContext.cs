//using System.Buffers.Text;
//using System.Collections.Generic;
//using UnityEngine;

///// <summary>
///// Provides world and game-state data for the enemy AI to make decisions.
///// Implemented by the game layer, read-only for AI logic.
///// </summary>
//public interface IAIContext
//{
//    //==================== Units ====================
//    List<int> GetOwnedUnitIds(); //All owned units
//    Vector2Int GetUnitPosition(int unitId);
//    string GetUnitType(int unitId);
//    int GetUnitAttackRange(int unitId);
//    bool IsUnitVisibleToPlayer(int unitId); //Drives Dormant -> Aggressive switch
//    int GetUnitMoveRange(int unitId);
//    List<Vector2Int> GetReachableHexes(Vector2Int startHex, int moveRange);
//    bool IsTileOccupied(Vector2Int hex);
//    int GetUnitHP(int unitId);

//    //==================== Bases ====================
//    List<int> GetOwnedBaseIds(); //All owned Bases
//    Vector2Int GetBasePosition(int baseId); //Base position
//    int GetBaseHP(int baseId);
//    bool CanProduceUnit(int baseId); //Check whether the Base can produce unit
//    //bool CanUpgradeBase(int baseId); //Check whether the Base can be upgraded
//    int GetBaseUnitCount(int baseId); //Returns how many units are currently stationed or linked to the base
//    bool IsBaseOccupied(int baseId);
//    bool IsUnitOnBaseTile(int baseId); //Returns unit ID if any is standing on the base
//    int GetUnitOnBaseTile(int baseId);
//    bool IsBaseDestroyed(int baseId);

//    //==================== Structure Tiles Objects ====================
//    //List<Vector3> GetRuinLocations(); //All _KNOWN_ Ruin positions
//    //List<Vector3> GetCacheLocations(); //All _KNOWN_ Cache positions
//    //List<Vector3> GetUnexploredTiles();

//    //==================== Enemy(/Player) Info ====================
//    bool IsEnemyNearby(Vector2Int position, int range); //Check if any enemy is within range
//    int GetNearestEnemy(int unitId); //Get the nearest enemy from a self unit
//    List<int> GetEnemiesInRange(Vector2Int position, int range); //Get all enemy IDs within attack range
//    Vector2Int GetEnemyPosition(int enemyId); //Get position of a specific enemy unit or Base (especially for combat)
//    List<int> GetPlayerBaseIds(); //All _KNOWN_ player bases
//    List<int> GetPlayerUnitIds(); //All _KNOWN_ player units

//    //==================== Hex Tile Helpers ====================
//    Vector2Int WorldToHex(Vector3 worldPos); //Convert between world and hex coordinates
//    Vector3 HexToWorld(Vector2Int hexPos);
//    int GetHexDistance(Vector2Int a, Vector2Int b); //Get hex distance (range in tiles)

//    //==================== Turn Info ====================
//    int GetTurnNumber(); //Current turn number
//}
