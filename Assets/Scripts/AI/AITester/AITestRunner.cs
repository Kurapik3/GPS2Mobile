using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple sandbox to visualize AIController behavior.
/// Simulates context and actor for testing Base, Exploration, and Combat AI.
/// </summary>
public class AITestRunner : MonoBehaviour
{
    private AIController ai;
    private TestContext context;
    private TestActor actor;

    private float tileRadius = 1.0f;
    private Dictionary<Vector2Int, Vector3> hexTiles = new();

    void Start()
    {
        //Spawn Base (AI-owned)
        var aiBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        aiBase.name = "AI Base";
        aiBase.transform.position = new Vector3(0, 0, 0);
        aiBase.GetComponent<Renderer>().material.color = Color.red;

        //Spawn AI Units
        var aiUnitDormant = GameObject.CreatePrimitive(PrimitiveType.Cube);
        aiUnitDormant.name = "AI Unit Dormant";
        aiUnitDormant.transform.position = new Vector3(-3, 1, 0);
        aiUnitDormant.GetComponent<Renderer>().material.color = Color.yellow;

        var aiUnitAggressive = GameObject.CreatePrimitive(PrimitiveType.Cube);
        aiUnitAggressive.name = "AI Unit Aggressive";
        aiUnitAggressive.transform.position = new Vector3(3, 1, 0);
        aiUnitAggressive.GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 0f); //orange

        //Spawn Player Base
        var playerBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        playerBase.name = "Player Base";
        playerBase.transform.position = new Vector3(5, 0, 0);
        playerBase.GetComponent<Renderer>().material.color = Color.green;

        //Spawn Player Unit
        var playerUnit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        playerUnit.name = "Player Unit";
        playerUnit.transform.position = new Vector3(4, 1, 2);
        playerUnit.GetComponent<Renderer>().material.color = Color.cyan;

        //Build AI test context and actor
        context = new TestContext(aiBase, aiUnitDormant, aiUnitAggressive, playerBase, playerUnit);
        actor = gameObject.AddComponent<TestActor>();
        actor.units = new() { aiUnitDormant, aiUnitAggressive };
        actor.baseObj = aiBase;

        ai = new AIController(context, actor);

        //Run AI turn gradually
        StartCoroutine(RunTurnStepByStep());
    }

    private IEnumerator RunTurnStepByStep()
    {
        Debug.Log("<color=yellow>=== AI Turn Start ===</color>");

        yield return new WaitForSeconds(1.2f);
        //Base
        Debug.Log("<color=orange>[Step 1] EnemyBaseAI executing...</color>");
        ai.subAIs[0].Execute();
        yield return new WaitForSeconds(1.2f);

        //Exploration
        Debug.Log("<color=orange>[Step 2] ExplorationAI executing...</color>");
        ai.subAIs[1].Execute();
        yield return new WaitForSeconds(1.2f);

        //Combat
        Debug.Log("<color=orange>[Step 3] CombatAI executing...</color>");
        ai.subAIs[2].Execute();
        yield return new WaitForSeconds(1.2f);

        Debug.Log("<color=yellow>=== AI Turn End ===</color>");
    }
}

#region ---- Fake classes for testing ----
public class TestContext : IAIContext
{
    private GameObject aiBase;
    private List<GameObject> aiUnits;
    private GameObject playerBase;
    private GameObject playerUnit;

    public TestContext(GameObject aiBase, GameObject unit1, GameObject unit2, GameObject playerBase, GameObject playerUnit)
    {
        this.aiBase = aiBase;
        this.aiUnits = new List<GameObject> { unit1, unit2 };
        this.playerBase = playerBase;
        this.playerUnit = playerUnit;
    }

    public List<int> GetOwnedUnitIds() => new List<int> { 1, 2 };
    public Vector3 GetUnitPosition(int unitId) => aiUnits[unitId - 1].transform.position;
    public string GetUnitType(int unitId) => "Builder";
    public float GetUnitAttackRange(int unitId) => 2.5f;

    public bool IsUnitVisibleToPlayer(int unitId)
    {        
        return unitId == 2; //Unit 1 (yellow) is dormant, Unit 2 (orange) is aggressive
    }

    public List<int> GetOwnedBaseIds() => new List<int> { 10 };
    public Vector3 GetBasePosition(int baseId) => aiBase.transform.position;
    public int GetBaseHP(int baseId) => 100;
    public bool CanProduceUnit(int baseId)
    {
        if (IsBaseOccupied(baseId)) 
            return false;
        return aiUnits.Count < 3;
    }

    public bool CanUpgradeBase(int baseId) => false;
    public int GetBaseUnitCount(int baseId) => aiUnits.Count;

    public bool IsBaseOccupied(int baseId)
    {
        foreach (var unit in aiUnits)
        {
            if (Vector3.Distance(unit.transform.position, aiBase.transform.position) < 1.5f)
                return true;
        }
        return false;
    }

    public List<Vector3> GetRuinLocations() => new List<Vector3> { new Vector3(-2, 0, 2) };
    public List<Vector3> GetCacheLocations() => new List<Vector3> { new Vector3(3, 0, -3) };
    public List<Vector3> GetUnexploredTiles() => new List<Vector3> { new Vector3(-1, 0, -1), new Vector3(2, 0, 2) };

    public bool IsEnemyNearby(Vector3 position, float range)
        => Vector3.Distance(position, playerBase.transform.position) <= range ||
           Vector3.Distance(position, playerUnit.transform.position) <= range;

    public Vector3 GetNearestEnemy(Vector3 fromPosition)
    {
        var baseDist = Vector3.Distance(fromPosition, playerBase.transform.position);
        var unitDist = Vector3.Distance(fromPosition, playerUnit.transform.position);
        return baseDist < unitDist ? playerBase.transform.position : playerUnit.transform.position;
    }

    public List<int> GetEnemiesInRange(Vector3 position, float range)
    {
        List<int> list = new();
        if (Vector3.Distance(position, playerBase.transform.position) <= range) list.Add(200);
        if (Vector3.Distance(position, playerUnit.transform.position) <= range) list.Add(201);
        return list;
    }

    public Vector3 GetEnemyPosition(int enemyId)
    {
        return enemyId switch
        {
            200 => playerBase.transform.position,
            201 => playerUnit.transform.position,
            _ => Vector3.zero
        };
    }

    public List<int> GetPlayerBaseIds() => new List<int> { 200 };
    public List<int> GetPlayerUnitIds() => new List<int> { 201 };

    public int GetTurnNumber() => 1;
}

public class TestActor : MonoBehaviour, IAIActor
{
    public List<GameObject> units;
    public GameObject baseObj;

    //Move (Explore)
    public void MoveTo(int unitId, Vector3 destination)
    {
        var unit = units[unitId - 1];
        Debug.Log($"[TestActor] Unit {unitId} moves to {destination}");
        StartCoroutine(SmoothMove(unit, destination));
    }

    private IEnumerator SmoothMove(GameObject unit, Vector3 destination)
    {
        Vector3 start = unit.transform.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            unit.transform.position = Vector3.Lerp(start, destination, t);
            yield return null;
        }
    }

    //Combat
    public void AttackTarget(int unitId, int targetId)
    {
        Debug.Log($"[TestActor] Unit {unitId} attacks target {targetId}");
        StartCoroutine(FlashTarget(targetId));
    }

    private IEnumerator FlashTarget(int targetId)
    {
        GameObject target = null;
        if (targetId == 200) target = GameObject.Find("Player Base");
        else if (targetId == 201) target = GameObject.Find("Player Unit");

        if (target != null)
        {
            var renderer = target.GetComponent<Renderer>();
            Color original = renderer.material.color;
            renderer.material.color = Color.red;
            yield return new WaitForSeconds(0.3f);
            renderer.material.color = original;
        }
    }

    //Base Actions
    public void SpawnUnit(int baseId, string unitType)
    {
        Debug.Log($"[TestActor] Base {baseId} produces {unitType}");
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = baseObj.transform.position + new Vector3(0, 1, 2);
        cube.GetComponent<Renderer>().material.color = new Color(1f, 0.6f, 0.6f);
        units.Add(cube);
    }

    public void UpgradeBase(int baseId)
    {
        Debug.Log($"[TestActor] Upgrading Base {baseId}");
        baseObj.transform.localScale *= 1.1f;
    }

    public void RebuildRuin(Vector3 location)
    {
        Debug.Log($"[TestActor] Rebuild ruin at {location}");
    }

    //Retreat & Turn
    public void RetreatTo(int unitId, Vector3 safeLocation)
    {
        Debug.Log($"[TestActor] Unit {unitId} retreats to {safeLocation}");
    }

    public void EndTurn()
    {
        Debug.Log("[TestActor] Turn Ended");
    }
}

#endregion
