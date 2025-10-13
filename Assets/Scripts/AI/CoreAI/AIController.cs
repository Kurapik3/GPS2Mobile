using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the overall AI turn flow:
/// - Base management (spawn/upgrade)
/// - Exploration & Combat logic
/// - Unlocks new unit types as turns progress
/// Works with TurnManager: call ExecuteTurn() each enemy turn.
/// Fully implements IAIActor + IAIContext for prototype.
/// </summary>
public class AIController : MonoBehaviour, IAIActor, IAIContext
{
    [SerializeField] private List<GameObject> unitPrefabs;
    [SerializeField] private AIUnlockSystem unlockSystem;
    [SerializeField] private UnitDatabase unitDatabase;

    private List<ISubAI> subAIs;
    private int currentTurn = 1;
    public bool IsTurnFinished { get; private set; }

    // ==================== Runtime Data ====================
    private Dictionary<int, Vector3> aiUnitPositions = new();
    private Dictionary<int, string> aiUnitTypes = new();
    private Dictionary<int, int> aiUnitHPs = new();
    private Dictionary<int, Vector3> aiBasePositions = new();
    private Dictionary<int, int> aiBaseHPs = new();

    private Dictionary<int, GameObject> aiUnitObjects = new();

    private int nextUnitId = 1;

    private Dictionary<string, GameObject> prefabDict;
    
    public delegate void AIEvent();
    public event AIEvent OnAITurnFinished;

    private void Awake()
    {
        Initialize();

        prefabDict = new Dictionary<string, GameObject>();
        foreach (var prefab in unitPrefabs)
            prefabDict[prefab.name] = prefab;
        SetupTestBaseAndUnit();
    }

    private void Initialize()
    {
        if (unlockSystem == null)
            unlockSystem = FindFirstObjectByType<AIUnlockSystem>();

        subAIs = new List<ISubAI>
        {
            new EnemyBaseAI(),
            new ExplorationAI(),
            new CombatAI()
        };

        foreach (var subAI in subAIs)
            subAI.Initialize(this, this);

        Debug.Log("[AIController] Initialized.");
    }


    //=======================Temp=========================
    private void SetupTestBaseAndUnit()
    {
        int testBaseId = 10;
        Vector2Int baseHex = new Vector2Int(0, 0);
        Vector3 baseWorldPos = HexToWorld(baseHex);

        aiBasePositions[testBaseId] = baseWorldPos;
        aiBaseHPs[testBaseId] = 100;

        MapManager.Instance.SetUnitOccupied(baseHex, true);

        Debug.Log($"[TestSetup] Created test base {testBaseId} at {baseWorldPos}");

        int testUnitId = nextUnitId++;
        string unitType = "Scout";
        Vector2Int unitHex = new Vector2Int(0, 1);
        Vector3 unitWorldPos = HexToWorld(unitHex);

        aiUnitPositions[testUnitId] = unitWorldPos;
        aiUnitTypes[testUnitId] = unitType;

        UnitData data = unitDatabase.GetUnitByName(unitType);
        aiUnitHPs[testUnitId] = data != null ? data.hp : 100;

        if (prefabDict.TryGetValue(unitType, out var prefab))
        {
            GameObject unitGO = Instantiate(prefab, unitWorldPos, Quaternion.identity);
            unitGO.name = $"TestEnemy_{unitType}_{testUnitId}";
            aiUnitObjects[testUnitId] = unitGO;
        }

        MapManager.Instance.SetUnitOccupied(unitHex, true);

        Debug.Log($"[TestSetup] Spawned test unit {unitType} #{testUnitId} at {unitWorldPos}");
    }
    //================================================================

    // ==================== Turn Management ====================
    public void ExecuteTurn()
    {
        StartCoroutine(ExecuteTurnCoroutine());
    }

    private IEnumerator ExecuteTurnCoroutine()
    {
        IsTurnFinished = false;
        Debug.Log($"<color=yellow>=== [AIController] Enemy Turn {currentTurn} Start ===</color>");

        foreach (var subAI in subAIs)
        {
            yield return StartCoroutine(subAI.ExecuteStepByStep());
            Debug.Log($"Executing {subAI.GetType().Name}...");
        }

        EndTurn();
    }

    public void EndTurn()
    {
        IsTurnFinished = true;
        Debug.Log("<color=yellow>=== [AIController] Enemy Turn End ===</color>");
        currentTurn++;

        OnAITurnFinished?.Invoke();
    } 

    // ==================== IAIActor Implementation ====================
    public void MoveTo(int unitId, Vector3 destination)
    {
        if (!aiUnitPositions.ContainsKey(unitId))
        {
            Debug.LogWarning($"[AI] MoveTo: unit {unitId} not found.");
            return;
        }

        //Release the old tile's occupation status
        Vector2Int oldHex = WorldToHex(aiUnitPositions[unitId]);
        MapManager.Instance.SetUnitOccupied(oldHex, false);

        //Update the unit's stored position
        aiUnitPositions[unitId] = destination;

        //Move the GameObject in the scene
        if (aiUnitObjects.TryGetValue(unitId, out var go))
            go.transform.position = destination;

        //Mark the new tile as occupied by this unit
        Vector2Int newHex = WorldToHex(destination);
        MapManager.Instance.SetUnitOccupied(newHex, true);

        Debug.Log($"[AIActor] Unit {unitId} moved to {destination}");
    }

    //public void RebuildRuin(Vector3 location)
    //{
    //    int newBaseId = nextBaseId++;
    //    aiBasePositions[newBaseId] = location;
    //    aiBaseHPs[newBaseId] = 100;

    //    var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //    baseObj.name = $"AI_Base_{newBaseId}";
    //    baseObj.transform.position = location;
    //    baseObj.GetComponent<Renderer>().material.color = Color.red;
    //    aiBaseObjects[newBaseId] = baseObj;

    //    Debug.Log($"[AIActor] Built base {newBaseId} at {location}");
    //}

    //public void UpgradeBase(int baseId)
    //{
    //    if (!aiBaseHPs.ContainsKey(baseId)) return;
    //    aiBaseHPs[baseId] += 50;
    //    Debug.Log($"[AIActor] Upgraded base {baseId} to {aiBaseHPs[baseId]} HP");
    //}

    public void SpawnUnit(int baseId, string unitType)
    {
        if (!aiBasePositions.ContainsKey(baseId))
        {
            Debug.LogWarning($"[AIActor] SpawnUnit failed: base {baseId} not found.");
            return;
        }

        UnitData data = unitDatabase?.GetUnitByName(unitType);
        if (data == null)
        {
            Debug.LogWarning($"[AIActor] SpawnUnit failed: unitType {unitType} not found.");
            return;
        }

        //Determine spawn position near the base
        Vector3 spawnPos = aiBasePositions[baseId] + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
        int unitId = nextUnitId++;

        //Store runtime info
        aiUnitPositions[unitId] = spawnPos;
        aiUnitTypes[unitId] = unitType;
        aiUnitHPs[unitId] = data.hp;

        //Create unit GameObject
        if (!prefabDict.TryGetValue(unitType, out var prefab))
        {
            Debug.LogWarning($"Prefab for {unitType} not found!");
            return;
        }
        GameObject unitGO = Instantiate(prefab, spawnPos, Quaternion.identity);
        unitGO.name = $"Enemy_{unitType}_{unitId}";
        aiUnitObjects[unitId] = unitGO;


        //Mark spawn tile as occupied
        Vector2Int hex = WorldToHex(spawnPos);
        MapManager.Instance.SetUnitOccupied(hex, true);

        Debug.Log($"[AIActor] Spawned {unitType} (id {unitId}) near base {baseId}");
    }

    public void AttackTarget(int unitId, int targetId)
    {
        Debug.Log($"[AIActor] Unit {unitId} attacks target {targetId}");
    }

    //public void RetreatTo(int unitId, Vector3 safeLocation)
    //{
    //    MoveTo(unitId, safeLocation);
    //    Debug.Log($"[AIActor] Unit {unitId} retreats to {safeLocation}");
    //}

    public void DestroyUnit(int unitId)
    {
        if (!aiUnitPositions.ContainsKey(unitId))
            return;

        //Release tile occupation
        Vector2Int hex = WorldToHex(aiUnitPositions[unitId]);
        MapManager.Instance.SetUnitOccupied(hex, false);

        //Remove runtime data
        aiUnitPositions.Remove(unitId);
        aiUnitTypes.Remove(unitId);
        aiUnitHPs.Remove(unitId);

        if (aiUnitObjects.TryGetValue(unitId, out var go))
            Destroy(go);

        aiUnitObjects.Remove(unitId);

        Debug.Log($"[AIActor] Destroyed unit {unitId} and freed tile {hex}");
    }

    // ==================== IAIContext Implementation ====================
    // ----- Units -----
    public List<int> GetOwnedUnitIds() => new(aiUnitPositions.Keys);
    public Vector3 GetUnitPosition(int id) => aiUnitPositions.TryGetValue(id, out var pos) ? pos : Vector3.zero;
    public string GetUnitType(int id) => aiUnitTypes.TryGetValue(id, out var t) ? t : "Unknown";
    public int GetUnitAttackRange(int id)
    {
        if (aiUnitTypes.TryGetValue(id, out var type))
        {
            var data = unitDatabase?.GetUnitByName(type);
            return data != null ? data.range : 1;
        }
        return 1;
    }
    public int GetUnitMoveRange(int id)
    {
        if (aiUnitTypes.TryGetValue(id, out var type))
        {
            var data = unitDatabase?.GetUnitByName(type);
            return data != null ? data.movement : 2;
        }
        return 2;
    }
    public bool IsUnitVisibleToPlayer(int unitId) => false;
    public List<Vector2Int> GetReachableHexes(Vector2Int startHex, int moveRange)
    {
        List<Vector2Int> reachable = new();
        for (int dx = -moveRange; dx <= moveRange; dx++)
        {
            for (int dy = Mathf.Max(-moveRange, -dx - moveRange); dy <= Mathf.Min(moveRange, -dx + moveRange); dy++)
            {
                reachable.Add(new Vector2Int(startHex.x + dx, startHex.y + dy));
            }
        }
        return reachable;
    }
    public bool IsTileOccupied(Vector2Int hex)
    {
        // Simplified: check if any unit is at the hex
        foreach (var pos in aiUnitPositions.Values)
        {
            Vector2Int unitHex = WorldToHex(pos);
            if (unitHex == hex) return true;
        }
        return false;
    }
    public int GetUnitHP(int unitId)
    {
        return aiUnitHPs.TryGetValue(unitId, out var hp) ? hp : 0;
    }
    //public bool IsUnitAlive(int unitId) => aiUnitHPs.ContainsKey(unitId) && aiUnitHPs[unitId] > 0;

    // ----- Bases -----
    public List<int> GetOwnedBaseIds() => new(aiBasePositions.Keys);
    public Vector3 GetBasePosition(int id) => aiBasePositions.TryGetValue(id, out var pos) ? pos : Vector3.zero;
    public int GetBaseHP(int baseId) => aiBaseHPs.TryGetValue(baseId, out var hp) ? hp : 0;
    public bool CanProduceUnit(int baseId) => aiBaseHPs.ContainsKey(baseId);
    public bool CanUpgradeBase(int baseId) => aiBaseHPs.ContainsKey(baseId) && aiBaseHPs[baseId] < 200;
    public int GetBaseUnitCount(int baseId) => 0;
    public bool IsBaseOccupied(int baseId) => false;

    // ----- Structure Tiles -----
    public List<Vector3> GetUnexploredTiles() => new List<Vector3>();

    // ----- Enemy / Player -----
    public List<int> GetPlayerUnitIds() => new() { 2000, 2001 };
    public List<int> GetPlayerBaseIds() => new() { 1000 };
    public Vector3 GetEnemyPosition(int id) => Vector3.zero;
    public bool IsEnemyNearby(Vector3 pos, int range) => false;
    public Vector3 GetNearestEnemy(Vector3 from) => Vector3.zero;
    public List<int> GetEnemiesInRange(Vector3 position, int range) => new List<int>();

    // ----- Hex Helpers -----
    public Vector2Int WorldToHex(Vector3 pos) => new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.z));
    public Vector3 HexToWorld(Vector2Int hex) => new Vector3(hex.x, 2, hex.y);
    public int GetHexDistance(Vector2Int a, Vector2Int b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

    // ----- Turn Info -----
    public int GetTurnNumber() => currentTurn;
}
