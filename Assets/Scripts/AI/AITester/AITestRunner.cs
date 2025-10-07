using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// Simple sandbox to visualize AIController behavior.
/// Simulates context and actor for testing Base, Exploration, and Combat AI.
/// Includes base HP tracking, base destruction feedback, and hex-based positioning.
/// </summary>
public class AITestRunner : MonoBehaviour
{
    [Header("Map Reference")]
    [SerializeField] private GameObject mapPrefabInstance;
    [SerializeField] private float hexSize = 1f;

    private AIController ai;
    private TestContext context;
    private TestActor actor;
    private MapGenerator mapGenerator;

    void Start()
    {
        //Find or use the map instance
        if (mapPrefabInstance == null)
        {
            Debug.LogError("No map prefab assigned!");
            return;
        }

        // Instantiate prefab in scene
        GameObject mapInstance = Instantiate(mapPrefabInstance);
        mapGenerator = mapInstance.GetComponent<MapGenerator>();
        mapGenerator.LayoutGrid();

        Debug.Log($"[AITestRunner] Map loaded with {MapGenerator.AllTiles.Count} tiles");

        //Spawn Base (AI-owned)
        var aiBase = SpawnAtHex("AI Base", PrimitiveType.Sphere, Color.red, new Vector2Int(0, 0));

        //Spawn AI Units
        var aiUnitDormant = SpawnAtHex("AI Unit Dormant", PrimitiveType.Cube, Color.yellow, new Vector2Int(-2, 1));
        var aiUnitAggressive = SpawnAtHex("AI Unit Aggressive", PrimitiveType.Cube, new Color(1f, 0.5f, 0f), new Vector2Int(2, 1));

        //Spawn Player Base
        var playerBase = SpawnAtHex("Player Base", PrimitiveType.Sphere, Color.green, new Vector2Int(4, 0));

        //Spawn Player Unit
        var playerUnit = SpawnAtHex("Player Unit", PrimitiveType.Cube, Color.cyan, new Vector2Int(3, 1));

        //Build AI test context and actor
        context = new TestContext(hexSize, aiBase, aiUnitDormant, aiUnitAggressive, playerBase, playerUnit);
        actor = gameObject.AddComponent<TestActor>();
        actor.units = new() { aiUnitDormant, aiUnitAggressive };
        actor.baseObj = aiBase;
        actor.context = context;

        ai = new AIController(context, actor);

        //Register base HP (for test)
        context.RegisterBase(10);

        //Run AI turn gradually
        StartCoroutine(RunTurnStepByStep());
    }

    //Gradually runs AI’s turn with visual timing between each sub-AI
    private IEnumerator RunTurnStepByStep()
    {
        Debug.Log("<color=yellow>=== AI Turn Start ===</color>");

        yield return new WaitForSeconds(1.2f);
        ai.subAIs[0].Execute(); //Base
        yield return new WaitForSeconds(1.2f);

        ai.subAIs[1].Execute(); //Exploration
        yield return new WaitForSeconds(1.2f);

        ai.subAIs[2].Execute(); //Combat
        yield return new WaitForSeconds(1.2f);

        yield return SimulateDamageOnBase();

        Debug.Log("<color=yellow>=== AI Turn End ===</color>");
    }

    /// <summary>
    /// Simulate a random hit to the AI base and visualize destruction when HP <= 0.
    /// </summary>
    private IEnumerator SimulateDamageOnBase()
    {
        yield return new WaitForSeconds(1f);
        GameObject target = null;
        int baseId = 10;
        int damage = Random.Range(5, 30);
        int newHP = context.ReduceBaseHP(baseId, damage);
        if (baseId == 10)
            target = GameObject.Find("AI Base");

        if (target != null)
        {
            var renderer = target.GetComponent<Renderer>();
            Color original = renderer.material.color;
            renderer.material.color = Color.black;
            yield return new WaitForSeconds(0.3f);
            if (newHP >= 0)
            {
                renderer.material.color = original;
            }
        }
        Debug.Log($"<color=red>AI Base took {damage} damage! (HP: {newHP})</color>");

        if (newHP <= 0)
        {
            Debug.Log("<color=gray>[TEST] AI Base destroyed and becomes Ruin!</color>");
            actor.ReplaceWithRuin(context.GetBasePosition(baseId));
        }
    }

    //Spawn a cube on a given hex coordinate with given color.
    private GameObject SpawnAtHex(string name, PrimitiveType type, Color color, Vector2Int coord)
    {
        // Check if tile exists
        if (!MapGenerator.AllTiles.TryGetValue(coord, out HexTile tile))
        {
            Debug.LogWarning($"[AITestRunner] No tile at {coord}, using calculated position");
            Vector3 fallbackPos = HexCoordinates.ToWorld(coord.x, coord.y, hexSize);
            return SpawnAtPosition(name, type, color, fallbackPos);
        }

        Vector3 worldPos = tile.transform.position;
        return SpawnAtPosition(name, type, color, worldPos);
    }

    private GameObject SpawnAtPosition(string name, PrimitiveType type, Color color, Vector3 position)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.position = position + new Vector3(0, obj.transform.localScale.y / 2f, 0);
        obj.GetComponent<Renderer>().material.color = color;
        return obj;
    }
}

#region ---- Fake classes for testing ----
public class TestContext : IAIContext
{
    private float hexSize;
    private GameObject aiBase;
    private List<GameObject> aiUnits;
    private GameObject playerBase;
    private GameObject playerUnit;
    private System.Random rng = new System.Random();
    private Dictionary<int, int> baseHPs = new();

    public TestContext(float hexSize, GameObject aiBase, GameObject unit1, GameObject unit2, GameObject playerBase, GameObject playerUnit)
    {
        this.hexSize = hexSize;
        this.aiBase = aiBase;
        this.aiUnits = new List<GameObject> { unit1, unit2 };
        this.playerBase = playerBase;
        this.playerUnit = playerUnit;
    }
    public float HexSize => hexSize;

    // Hex conversion methods using HexCoordinates utility
    public Vector2Int WorldToHex(Vector3 worldPos)
    {
        // Reverse of ToWorld (flat-top)
        float q = (2f / 3f * worldPos.x) / hexSize;
        float r = (-1f / 3f * worldPos.x + Mathf.Sqrt(3) / 3f * worldPos.z) / hexSize;
        return HexRound(q, r);
    }

    public Vector3 HexToWorld(Vector2Int hex)
    {
        return HexCoordinates.ToWorld(hex.x, hex.y, hexSize);
    }

    private Vector2Int HexRound(float q, float r)
    {
        float s = -q - r;

        int rq = Mathf.RoundToInt(q);
        int rr = Mathf.RoundToInt(r);
        int rs = Mathf.RoundToInt(s);

        float q_diff = Mathf.Abs(rq - q);
        float r_diff = Mathf.Abs(rr - r);
        float s_diff = Mathf.Abs(rs - s);

        if (q_diff > r_diff && q_diff > s_diff)
            rq = -rr - rs;
        else if (r_diff > s_diff)
            rr = -rq - rs;

        return new Vector2Int(rq, rr);
    }

    public int GetHexDistance(Vector2Int a, Vector2Int b)
    {
        return HexCoordinates.Distance(a.x, a.y, b.x, b.y);
    }

    // Validate hex coordinate exists in map
    public bool IsValidHex(Vector2Int hex)
    {
        return MapGenerator.AllTiles.ContainsKey(hex);
    }

    // Core unit methods
    public List<int> GetOwnedUnitIds() => new List<int> { 1, 2 };
    public Vector3 GetUnitPosition(int unitId) => aiUnits[unitId - 1].transform.position;
    public string GetUnitType(int unitId) => unitId == 1 ? "Builder" : "Tanker";
    public int GetUnitAttackRange(int unitId)
    {
        string type = GetUnitType(unitId);
        return type switch
        {
            "Builder" => 0,
            "Scout" => 0,
            "Tanker" => 1,
            "Shooter" => 5,
            "Bomber" => 3,
            _ => 0
        };
    }
    public bool IsUnitVisibleToPlayer(int unitId) => unitId == 2;
    public int GetUnitMoveRange(int unitId)
    {
        return GetUnitType(unitId) switch
        {
            "Builder" => 3,
            "Tanker" => 3,
            "Scout" => 3,
            "Shooter" => 2,
            "Bomber" => 2,
            _ => 1
        };
    }
    public List<Vector2Int> GetReachableHexes(Vector2Int startHex, int moveRange)
    {
        List<Vector2Int> reachable = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(startHex);
        visited.Add(startHex);

        Vector2Int[] directions = new Vector2Int[]
        {
        new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1),
        new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1)
        };

        int steps = 0;
        while (queue.Count > 0 && steps <= moveRange)
        {
            int levelCount = queue.Count;
            for (int i = 0; i < levelCount; i++)
            {
                var current = queue.Dequeue();
                reachable.Add(current);

                foreach (var dir in directions)
                {
                    Vector2Int neighbor = current + dir;
                    if (!visited.Contains(neighbor) && IsValidHex(neighbor))
                    {
                        queue.Enqueue(neighbor);
                        visited.Add(neighbor);
                    }
                }
            }
            steps++;
        }

        return reachable;
    }

    // Base methods
    public List<int> GetOwnedBaseIds() => new List<int> { 10 };
    public Vector3 GetBasePosition(int baseId) => aiBase.transform.position;

    public void RegisterBase(int baseId)
    {
        if (!baseHPs.ContainsKey(baseId))
            baseHPs[baseId] = rng.Next(20, 36);
    }

    public int GetBaseHP(int baseId) => baseHPs.TryGetValue(baseId, out int hp) ? hp : 0;

    public int ReduceBaseHP(int baseId, int amount)
    {
        if (!baseHPs.ContainsKey(baseId)) return 0;
        baseHPs[baseId] = Mathf.Max(0, baseHPs[baseId] - amount);
        return baseHPs[baseId];
    }

    public bool CanProduceUnit(int baseId)
    {
        if (IsBaseOccupied(baseId)) return false;
        return aiUnits.Count < 3;
    }

    public bool CanUpgradeBase(int baseId) => false;
    public int GetBaseUnitCount(int baseId) => aiUnits.Count;

    public bool IsBaseOccupied(int baseId)
    {
        Vector2Int baseHex = WorldToHex(aiBase.transform.position);
        foreach (var unit in aiUnits)
        {
            Vector2Int unitHex = WorldToHex(unit.transform.position);
            if (GetHexDistance(unitHex, baseHex) <= 1)
                return true;
        }
        return false;
    }

    // Structure tiles - using MapGenerator.AllTiles
    public List<Vector3> GetRuinLocations()
    {
        List<Vector3> ruins = new List<Vector3>();
        // Example: find tiles marked as ruins (you can extend HexTile.TileType)
        return ruins;
    }

    public List<Vector3> GetCacheLocations()
    {
        List<Vector3> caches = new List<Vector3>();
        return caches;
    }

    public List<Vector3> GetUnexploredTiles()
    {
        List<Vector3> unexplored = new List<Vector3>();
        // Example: return tiles not yet visited
        return unexplored;
    }

    // Enemy detection (hex-based)
    public bool IsEnemyNearby(Vector3 position, int hexRange)
    {
        Vector2Int posHex = WorldToHex(position);
        Vector2Int baseHex = WorldToHex(playerBase.transform.position);
        Vector2Int unitHex = WorldToHex(playerUnit.transform.position);

        return GetHexDistance(posHex, baseHex) <= hexRange || GetHexDistance(posHex, unitHex) <= hexRange;
    }

    public Vector3 GetNearestEnemy(Vector3 fromPosition)
    {
        Vector2Int unitHex = WorldToHex(fromPosition);
        Vector2Int baseHex = WorldToHex(playerBase.transform.position);
        Vector2Int playerUnitHex = WorldToHex(playerUnit.transform.position);

        int dBase = GetHexDistance(unitHex, baseHex);
        int dUnit = GetHexDistance(unitHex, playerUnitHex);

        return dBase < dUnit ? playerBase.transform.position : playerUnit.transform.position;
    }

    public List<int> GetEnemiesInRange(Vector3 worldPos, int hexRange)
    {
        Vector2Int selfHex = WorldToHex(worldPos);
        List<int> enemies = new();

        if (GetHexDistance(selfHex, WorldToHex(playerBase.transform.position)) <= hexRange)
            enemies.Add(200);

        if (GetHexDistance(selfHex, WorldToHex(playerUnit.transform.position)) <= hexRange)
            enemies.Add(201);

        return enemies;
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
    public TestContext context;

    // Move (Explore) - validates hex exists
    public void MoveTo(int unitId, Vector3 destination)
    {
        var unit = units[unitId - 1];

        //Validate destination is on a valid hex tile
        Vector2Int targetHex = context.WorldToHex(destination);
        if (!context.IsValidHex(targetHex))
        {
            Debug.LogWarning($"[TestActor] Unit {unitId} cannot move to invalid hex {targetHex}");
            return;
        }

        Vector3 tilePos;
        if (MapGenerator.AllTiles.TryGetValue(targetHex, out HexTile tile))
        {
            tilePos = tile.transform.position;
        }
        else
        {
            tilePos = HexCoordinates.ToWorld(targetHex.x, targetHex.y, context.HexSize);
        }

        Vector3 groundAdjustedDest = new Vector3(tilePos.x, tilePos.y + unit.transform.localScale.y / 2f, tilePos.z);

        StartCoroutine(SmoothMove(unit, groundAdjustedDest));
    }

    private IEnumerator SmoothMove(GameObject unit, Vector3 destination)
    {
        Vector3 start = unit.transform.position;
        float t = 0f;
        float duration = 0.8f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            unit.transform.position = Vector3.Lerp(start, destination, t);
            yield return null;
        }
    }

    // Combat
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

    // Base Actions
    public void SpawnUnit(int baseId, string unitType)
    {
        Debug.Log($"[TestActor] Base {baseId} produces {unitType}");

        // Spawn on the base
        Vector3 spawnPos = baseObj.transform.position;
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = spawnPos + new Vector3(0, baseObj.transform.localScale.y / 2f + cube.transform.localScale.y / 2f, 0);
        cube.GetComponent<Renderer>().material.color = new Color(1f, 0.6f, 0.6f);
        cube.name = $"{unitType} (new)";
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

    public void ReplaceWithRuin(Vector3 location)
    {
        Destroy(baseObj);
        var ruin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ruin.transform.position = location;
        ruin.transform.localScale = new Vector3(1f, 0.1f, 1f);
        ruin.GetComponent<Renderer>().material.color = Color.gray;
        ruin.name = "Ruin (Destroyed Base)";
    }

    // Retreat & Turn
    public void RetreatTo(int unitId, Vector3 safeLocation)
    {
        Debug.Log($"[TestActor] Unit {unitId} retreats to {safeLocation}");
        MoveTo(unitId, safeLocation);
    }

    public void EndTurn()
    {
        Debug.Log("[TestActor] Turn Ended");
    }
}
#endregion