using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AITestRunner : MonoBehaviour
{
    private AIController ai;

    void Start()
    {
        //Spawn AI Unit at (0,0,0)
        var aiUnit = GameObject.CreatePrimitive(PrimitiveType.Cube);
        aiUnit.name = "AI Unit";
        aiUnit.transform.position = new Vector3(0, 1, -10);

        //Spawn Enemy at (5,0,0)
        var enemy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        enemy.name = "Enemy";
        enemy.transform.position = new Vector3(1.5f, 1, -10);

        //Fake AI context + actor
        var context = new TestContext(aiUnit, enemy);
        var actor = gameObject.AddComponent<TestActor>();
        actor.unit = aiUnit;
        actor.enemy = enemy;

        ai = new AIController(context, actor, AIController.Difficulty.Normal);

        Debug.Log("=== AI Turn Start ===");
        ai.ExecuteTurn();
        Debug.Log("=== AI Turn End ===");
    }
}

#region ---- Fake classes for testing ----
public class TestContext : IAIContext
{
    private GameObject unit;
    private GameObject enemy;

    public TestContext(GameObject aiUnit, GameObject enemyObj)
    {
        unit = aiUnit;
        enemy = enemyObj;
    }

    public List<int> GetOwnedUnitIds() => new List<int> { 1 }; //AI owns one unit
    public Vector3 GetUnitPosition(int unitId) => unit.transform.position;
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

    public Vector3 GetNearestEnemy(Vector3 fromPosition) => enemy.transform.position;

    public List<int> GetEnemiesInRange(Vector3 position, float range)
    {
        if (Vector3.Distance(position, enemy.transform.position) <= range)
            return new List<int> { 200 };

        return new List<int>();
    }

    public Vector3 GetEnemyPosition(int enemyId) => enemy.transform.position;

    public int GetTurnNumber() => 1;
}

public class TestActor : MonoBehaviour, IAIActor
{
    public GameObject unit;
    public GameObject enemy;

    public void MoveTo(int unitId, Vector3 destination)
    {
        Debug.Log($"[AI] Unit {unitId} prepares to teleport to {destination}");
        StartCoroutine(TeleportWithDelay(destination, unitId));
    }

    private IEnumerator TeleportWithDelay(Vector3 target, int unitId)
    {
        var renderer = unit.GetComponent<Renderer>();
        Color originalColor = renderer.material.color;
        Color faded = originalColor;
        faded.a = 0.3f;

        renderer.material.color = faded;

        yield return new WaitForSeconds(0.5f);

        unit.transform.position = target;

        for (int i = 0; i < 3; i++)
        {
            renderer.enabled = false;
            yield return new WaitForSeconds(0.15f);
            renderer.enabled = true;
            yield return new WaitForSeconds(0.15f);
        }

        renderer.material.color = originalColor;
    }

    public void AttackTarget(int unitId, int targetId)
    {
        Debug.Log($"[AI] Unit {unitId} attacks enemy {targetId}");
        if (enemy != null)
            enemy.GetComponent<Renderer>().material.color = Color.red;
    }

    public void RebuildRuin(Vector3 location) =>
        Debug.Log($"[AI] Rebuild ruin at {location}");

    public void UpgradeBase(int baseId) =>
        Debug.Log($"[AI] Upgrade base {baseId}");

    public void ProduceUnit(int baseId, string unitType) =>
        Debug.Log($"[AI] Base {baseId} produces {unitType}");

    public void RetreatTo(int unitId, Vector3 safeLocation) =>
        Debug.Log($"[AI] Unit {unitId} retreats to {safeLocation}");

    public void EndTurn() =>
        Debug.Log("[AI] End Turn");
}

#endregion
