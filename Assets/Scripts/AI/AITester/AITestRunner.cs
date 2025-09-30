using UnityEngine;
using System.Collections.Generic;

public class AITestRunner : MonoBehaviour
{
    private AIController ai;

    void Start()
    {
        //Create fake context (map + units info) and fake actor (prints actions)
        var context = new TestContext();
        var actor = new TestActor();

        //Create AI controller with Normal difficulty
        ai = new AIController(context, actor, AIController.Difficulty.Normal);

        Debug.Log("=== AI Turn Start ===");
        ai.ExecuteTurn(); //Run an AI turn
        Debug.Log("=== AI Turn End ===");
    }
}

#region ---- Fake classes for testing ----
public class TestContext : IAIContext
{
    public List<int> GetOwnedUnitIds() => new List<int> { 1 }; //AI owns one unit
    public Vector3 GetUnitPosition(int unitId) => Vector3.zero; //Unit starts at (0,0,0)
    public string GetUnitType(int unitId) => "Soldier";

    public List<int> GetOwnedBaseIds() => new List<int> { 10 }; //AI has one base
    public Vector3 GetBasePosition(int baseId) => new Vector3(5, 0, 5);
    public int GetBaseHP(int baseId) => 100;
    public bool CanProduceUnit(int baseId) => true;
    public bool CanUpgradeBase(int baseId) => false;

    public List<Vector3> GetRuinLocations() => new List<Vector3> { new Vector3(3, 0, 3) };
    public List<Vector3> GetCacheLocations() => new List<Vector3>();
    public List<Vector3> GetUnexploredTiles() => new List<Vector3>
    {
        new Vector3(1,0,1), new Vector3(2,0,2), new Vector3(3,0,3)
    };

    public bool IsEnemyNearby(Vector3 position, float range) => true;

    public Vector3 GetNearestEnemy(Vector3 fromPosition) => new Vector3(2, 0, 0);

    public List<int> GetEnemiesInRange(Vector3 position, float range)
    {
        //If AI unit is near (1,0,0), then enemy ID=200 is in range
        if (Vector3.Distance(position, new Vector3(1, 0, 0)) <= range)
            return new List<int> { 200 };

        return new List<int>();
    }

    public Vector3 GetEnemyPosition(int enemyId) => new Vector3(1, 0, 0);

    public int GetTurnNumber() => 1;
}

public class TestActor : IAIActor
{
    public void MoveTo(int unitId, Vector3 destination) =>
        Debug.Log($"[AI] Unit {unitId} moves to {destination}");

    public void RebuildRuin(Vector3 location) =>
        Debug.Log($"[AI] Rebuild ruin at {location}");

    public void UpgradeBase(int baseId) =>
        Debug.Log($"[AI] Upgrade base {baseId}");

    public void ProduceUnit(int baseId, string unitType) =>
        Debug.Log($"[AI] Base {baseId} produces {unitType}");

    public void AttackTarget(int unitId, int targetId) =>
        Debug.Log($"[AI] Unit {unitId} attacks enemy {targetId}");

    public void RetreatTo(int unitId, Vector3 safeLocation) =>
        Debug.Log($"[AI] Unit {unitId} retreats to {safeLocation}");

    public void EndTurn() =>
        Debug.Log("[AI] End Turn");
}

#endregion
