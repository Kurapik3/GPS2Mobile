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
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private float unitHeightOffset = 2f;
    public static float AISpeedMultiplier = 2.5f;

    private List<ISubAI> subAIs;
    private int currentTurn = 1;
    public bool IsTurnFinished { get; private set; }

    //==================== Runtime Data (Enemy) ====================
    private Dictionary<int, Vector2Int> aiUnitPositions = new();
    private Dictionary<int, string> aiUnitTypes = new();
    private Dictionary<int, int> aiUnitHPs = new();
    private Dictionary<int, int> aiUnitSpawnTurn = new();
    private Dictionary<int, int> aiUnitHousedInBase = new(); //Track which base a unit is housed in
    private Dictionary<int, GameObject> aiUnitObjects = new();

    private Dictionary<int, Vector2Int> aiBasePositions = new();
    private Dictionary<int, int> aiBaseHPs = new();
    private Dictionary<int, int> aiBaseUnitCount = new(); //Track how many units are housed in each base
    private Dictionary<int, GameObject> aiBaseObjects = new();

    private int nextUnitId = 1;
    private int nextBaseId = 1;

    //==================== Runtime Data (Player) ====================
    private Dictionary<int, Vector2Int> playerUnitPositions = new();
    private Dictionary<int, GameObject> playerUnitObjects = new();
    private Dictionary<int, Vector2Int> playerBasePositions = new();
    private Dictionary<int, GameObject> playerBaseObjects = new();

    //Player ids start ranges, so they don't collide with AI ids
    private int nextPlayerUnitId = 1000;
    private int nextPlayerBaseId = 1000;

    private Dictionary<string, GameObject> prefabDict;
    
    public delegate void AIEvent();
    public event AIEvent OnAITurnFinished;

    private float hexSize;

    private void Awake()
    {
        Initialize();

        prefabDict = new Dictionary<string, GameObject>();
        foreach (var prefab in unitPrefabs)
            prefabDict[prefab.name] = prefab;
    }

    private void OnEnable()
    {
        MapGenerator.OnMapReady += HandleMapReady;
    }

    private void OnDisable()
    {
        MapGenerator.OnMapReady -= HandleMapReady;
    }

    private void HandleMapReady(MapGenerator map)
    {
        DiscoverEnemyBases();
        DiscoverPlayerBases();
        DiscoverPlayerUnits();
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

    //Discovers all GameObjects with "EnemyBase" tag and registers them as AI bases
    private void DiscoverEnemyBases()
    {
        GameObject[] basesArray = GameObject.FindGameObjectsWithTag("EnemyBase");

        if (basesArray == null || basesArray.Length == 0)
        {
            Debug.LogWarning("[AIController] No enemy bases found with 'EnemyBase' tag!");
            return;
        }

        foreach (var baseGO in basesArray)
        {
            int baseId = nextBaseId++;
            Vector2Int baseHex = WorldToHex(baseGO.transform.position);

            aiBasePositions[baseId] = baseHex;
            aiBaseHPs[baseId] = Random.Range(20, 36); //HP: 20 ~ 35
            aiBaseUnitCount[baseId] = 0; //Start with no units housed
            aiBaseObjects[baseId] = baseGO;

            //Mark base tile as occupied
            TryReserveTile(baseHex);
        }
    }

    //Discover player bases using tag "PlayerBase" and assign stable ids
    private void DiscoverPlayerBases()
    {
        GameObject[] basesArray = GameObject.FindGameObjectsWithTag("PlayerBase");


        if (basesArray == null || basesArray.Length == 0)
        {
            Debug.LogWarning("[AIController] No player bases found with 'PlayerBase' tag!");
            return;
        }

        foreach (var baseGO in basesArray)
        {
            int baseId = nextPlayerBaseId++;
            Vector2Int baseHex = WorldToHex(baseGO.transform.position);
            playerBasePositions[baseId] = baseHex;
            playerBaseObjects[baseId] = baseGO;
        }
    }

    //Discover player units using tag "PlayerUnit" and assign stable ids
    private void DiscoverPlayerUnits()
    {
        GameObject[] unitsArray = GameObject.FindGameObjectsWithTag("PlayerUnit");


        if (unitsArray == null || unitsArray.Length == 0)
        {
            Debug.LogWarning("[AIController] No player units found with 'PlayerUnit' tag!");
            return;
        }

        foreach (var unitGO in unitsArray)
        {
            int unitId = nextPlayerUnitId++;
            Vector2Int unitHex = WorldToHex(unitGO.transform.position);
            playerUnitPositions[unitId] = unitHex;
            playerUnitObjects[unitId] = unitGO;
        }
    }

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
            Debug.LogWarning($"[AIActor] MoveTo: unit {unitId} not found.");
            return;
        }

        if (IsUnitNewlySpawned(unitId))
        {
            Debug.Log($"[AIActor] Unit {unitId} was just spawned, cannot move yet.");
            return;
        }

        //Release the old tile's occupation status
        Vector2Int oldHex = aiUnitPositions[unitId];
        Vector2Int newHex = WorldToHex(destination);

        //Do nothing when already at target hex
        if (newHex == oldHex)
            return;

        //If tile is not walkable for AI, reject
        if (!MapManager.Instance.CanUnitStandHere(newHex))
            return;

        // If someone already occupies the tile (another unit or base), reject
        if (!TryReserveTile(newHex))
        {
            Debug.Log($"[AIActor] MoveTo aborted: tile {newHex} already reserved.");
            return;
        }

        //Release old tile AFTER new one reserved
        ReleaseTile(oldHex);

        //Update the unit's stored position
        aiUnitPositions[unitId] = newHex;
        Vector3 worldDest = HexToWorld(newHex);

        //Move the GameObject in the scene
        if (aiUnitObjects.TryGetValue(unitId, out var go))
        {
            StartCoroutine(SmoothMove(go, worldDest));
        }
    }

    private IEnumerator SmoothMove(GameObject unit, Vector3 destination)
    {
        Vector3 start = unit.transform.position;
        float t = 0f;
        float duration = 0.5f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            unit.transform.position = Vector3.Lerp(start, destination, t);
            yield return null;
        }

        unit.transform.position = destination;
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
        Vector2Int baseHex = aiBasePositions[baseId];
        Vector3 spawnPos = HexToWorld(baseHex);

        //Create unit GameObject
        if (!prefabDict.TryGetValue(unitType, out var prefab))
        {
            Debug.LogWarning($"Prefab for {unitType} not found!");
            return;
        }

        int unitId = nextUnitId++;

        //Store runtime info
        aiUnitPositions[unitId] = baseHex;
        aiUnitTypes[unitId] = unitType;
        aiUnitHPs[unitId] = data.hp;
        aiUnitSpawnTurn[unitId] = currentTurn;
        aiUnitHousedInBase[unitId] = baseId; //Unit is housed in this base

        //Increment unit count in base
        aiBaseUnitCount[baseId]++;

        GameObject unitGO = Instantiate(prefab, spawnPos, Quaternion.identity);
        unitGO.name = $"Enemy_{unitType}_{unitId}";
        aiUnitObjects[unitId] = unitGO;

        //Added a tag for easy detection
        unitGO.tag = "EnemyUnit"; 
        foreach (Transform child in unitGO.transform)
        {
            child.gameObject.tag = "EnemyUnit";
        }

        //Mark spawn tile as occupied
        if (!TryReserveTile(baseHex))
        {
            Debug.LogWarning($"[AIActor] Spawn failed: tile {baseHex} occupied.");
            return;
        }
    }

    public void AttackTarget(int unitId, int targetId)
    {
        if (IsUnitNewlySpawned(unitId))
        {
            Debug.Log($"[AI] Unit {unitId} was just spawned, cannot attack yet.");
            return;
        }
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

        //Decrement unit count in base if unit was housed
        if (aiUnitHousedInBase.TryGetValue(unitId, out int baseId) && aiBaseUnitCount.ContainsKey(baseId))
        {
            aiBaseUnitCount[baseId]--;
            Debug.Log($"[AIActor] Unit {unitId} was housed in base {baseId}. Base now houses {aiBaseUnitCount[baseId]}/3 units.");
        }

        //Release tile occupation
        Vector2Int hex = aiUnitPositions[unitId];
        ReleaseTile(hex);

        //Remove runtime data
        aiUnitPositions.Remove(unitId);
        aiUnitTypes.Remove(unitId);
        aiUnitHPs.Remove(unitId);
        aiUnitSpawnTurn.Remove(unitId);
        aiUnitHousedInBase.Remove(unitId);

        if (aiUnitObjects.TryGetValue(unitId, out var go))
            Destroy(go);

        aiUnitObjects.Remove(unitId);
    }

    private bool IsUnitNewlySpawned(int unitId)
    {
        if (!aiUnitSpawnTurn.ContainsKey(unitId))
            return false;

        return aiUnitSpawnTurn[unitId] == currentTurn;
    }

    // ==================== IAIContext Implementation ====================
    // ----- Units -----
    public List<int> GetOwnedUnitIds() => new(aiUnitPositions.Keys);
    public Vector2Int GetUnitPosition(int id)
    {
        if (!aiUnitPositions.TryGetValue(id, out var pos))
        {
            Debug.LogWarning($"[AIContext] GetUnitPosition: unit {id} not found!");
            return new Vector2Int(int.MinValue, int.MinValue);
        }
        return pos;
    }
    public string GetUnitType(int id)
    {
        if (!aiUnitTypes.TryGetValue(id, out var t))
        {
            Debug.LogWarning($"[AIContext] GetUnitType: unit {id} not found!");
            return null;
        }
        return t;
    }
    public int GetUnitAttackRange(int id)
    {
        if (aiUnitTypes.TryGetValue(id, out var type))
        {
            var data = unitDatabase?.GetUnitByName(type);
            if (data == null)
            {
                Debug.LogWarning($"[AIContext] GetUnitAttackRange: No data for {type}");
                return 0;
            }
            return data.range;
        }
        return 0;
    }
    public int GetUnitMoveRange(int id)
    {
        if (aiUnitTypes.TryGetValue(id, out var type))
        {
            var data = unitDatabase?.GetUnitByName(type);
            if (data == null)
            {
                Debug.LogWarning($"[AIContext] GetUnitMovementRange: No data for {type}");
                return 0;
            }
            return data.movement;
        }
        return 0;
    }
    public bool IsUnitVisibleToPlayer(int unitId)
    {
        FogSystem fog = FindFirstObjectByType<FogSystem>();
        if (fog == null)
        {
            Debug.LogWarning("FogSystem not found — assume all units visible");
            return true;
        }

        Vector2Int unitPos = GetUnitPosition(unitId);

        bool visible = fog.revealedTiles.Contains(unitPos);

        //Debug.Log($"[AIContext] Unit {unitId} at {unitPos} visible = {visible}");

        return visible;
    }
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
        //Check if any AI unit is at the hex
        foreach (var pos in aiUnitPositions.Values)
        {
            if (pos == hex)
                return true;
        }

        //Check player units
        foreach (var pos in playerUnitPositions.Values)
        {
            if (pos == hex) 
                return true;
        }

        //Check player bases
        foreach (var pos in playerBasePositions.Values)
        {
            if (pos == hex) 
                return true;
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
    public Vector2Int GetBasePosition(int id)
    {
        if (!aiBasePositions.TryGetValue(id, out var pos))
        {
            Debug.LogWarning($"[AIContext] Base {id} not found!");
            return new Vector2Int(int.MinValue, int.MinValue);
        }
        return pos;
    }
    public int GetBaseHP(int baseId) => aiBaseHPs.TryGetValue(baseId, out var hp) ? hp : 0;
    public bool CanProduceUnit(int baseId)
    {
        if (!aiBaseUnitCount.TryGetValue(baseId, out var count))
        {
            Debug.LogWarning($"[AIContext] CanProduceUnit: base {baseId} not found!");
            return false;
        }
        return count < 3;
    }

    //public bool CanUpgradeBase(int baseId) => aiBaseHPs.ContainsKey(baseId) && aiBaseHPs[baseId] < 200;
    public int GetBaseUnitCount(int baseId)
    {
        return aiBaseUnitCount.TryGetValue(baseId, out var count) ? count : 0;
    }
    public bool IsBaseOccupied(int baseId)
    {
        if (!aiBasePositions.TryGetValue(baseId, out var basePos))
            return false;

        // Check if any AI unit stands on same hex
        foreach (var kvp in aiUnitPositions)
        {
            if (kvp.Value == basePos)
                return true;
        }
        return false;
    }

    //Check if there is an enemy unit standing on the base tile.
    //If true, all incoming damage is redirected to this unit instead of the base.
    public bool IsUnitOnBaseTile(int baseId)
    {
        if (!aiBasePositions.TryGetValue(baseId, out var basePos))
            return false;

        Vector2Int baseHex = basePos;
        foreach (var unitPos in aiUnitPositions.Values)
        {
            Vector2Int unitHex = unitPos;
            if (unitHex == baseHex)
                return true;
        }
        return false;
    }

    //Get the ID of the unit standing on the base tile (if any).
    //Returns -1 if no unit is on the base.
    public int GetUnitOnBaseTile(int baseId)
    {
        if (!aiBasePositions.TryGetValue(baseId, out var basePos))
            return -1;

        Vector2Int baseHex = basePos;
        foreach (var unitPair in aiUnitPositions)
        {
            Vector2Int unitHex = unitPair.Value;
            if (unitHex == baseHex)
                return unitPair.Key; // Return the unit ID
        }
        return -1;
    }

    public bool IsBaseDestroyed(int baseId)
    {
        return GetBaseHP(baseId) <= 0;
    }


    // ----- Structure Tiles -----
    //public List<Vector3> GetUnexploredTiles() => new List<Vector3>();

    // ----- Enemy / Player -----
    public List<int> GetPlayerUnitIds() => new (playerUnitPositions.Keys);
    public List<int> GetPlayerBaseIds() => new (playerBasePositions.Keys);
    public Vector2Int GetEnemyPosition(int id) => playerUnitPositions.TryGetValue(id, out var p) ? p : Vector2Int.zero;
    public bool IsEnemyNearby(Vector2Int fromHex, int range)
    {
        foreach (var kvp in playerUnitPositions)
        {
            var enemyHex = kvp.Value;
            int dist = HexCoordinates.Distance(fromHex.x, fromHex.y, enemyHex.x, enemyHex.y);
            if (dist <= range)
                return true;
        }
        return false;
    }

    public int GetNearestEnemy(int unitId)
    {
        if (!aiUnitPositions.TryGetValue(unitId, out Vector2Int myHex))
            return -1;

        int nearestId = -1;
        int nearestDist = int.MaxValue;

        foreach (var kvp in playerUnitPositions)
        {
            int enemyId = kvp.Key;
            Vector2Int enemyHex = kvp.Value;
            int dist = HexCoordinates.Distance(myHex.x, myHex.y, enemyHex.x, enemyHex.y);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestId = enemyId;
            }

            if (nearestId == -1)
                Debug.Log("[AIContext] No enemies found.");
        }
        return nearestId;
    }
    public List<int> GetEnemiesInRange(Vector2Int fromHex, int range)
    {
        List<int> result = new();
        foreach (var kvp in playerUnitPositions)
        {
            int enemyId = kvp.Key;
            var enemyHex = kvp.Value;
            int dist = HexCoordinates.Distance(fromHex.x, fromHex.y, enemyHex.x, enemyHex.y);
            if (dist <= range)
                result.Add(enemyId);

            if (result.Count == 0)
                Debug.Log($"[AIContext] No enemies in range {range} from {fromHex}");
        }
        return result;
    }

    // ----- Hex Helpers -----
    public Vector2Int WorldToHex(Vector3 pos)
    {
        hexSize = mapGenerator.GetHexSize();
        // Reverse of ToWorld (flat-top)
        float q = (2f / 3f * pos.x) / hexSize;
        float r = (-1f / 3f * pos.x + Mathf.Sqrt(3) / 3f * pos.z) / hexSize;
        return HexRound(q, r);
    }
    public Vector3 HexToWorld(Vector2Int hex)
    {
        hexSize = mapGenerator.GetHexSize();
        Vector3 pos = HexCoordinates.ToWorld(hex.x, hex.y, hexSize);
        pos.y += unitHeightOffset;
        return pos;
    }
    public int GetHexDistance(Vector2Int a, Vector2Int b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
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
    // ----- Turn Info -----
    public int GetTurnNumber() => currentTurn;

    // ==================== Tile Occupancy Helpers ====================
    private bool TryReserveTile(Vector2Int hex)
    {
        // Check if already occupied
        if (MapManager.Instance.IsTileOccupied(hex) || IsTileOccupied(hex))
        {
            Debug.Log($"[AIController] Tile {hex} already occupied.");
            return false;
        }

        MapManager.Instance.SetUnitOccupied(hex, true);
        return true;
    }

    private void ReleaseTile(Vector2Int hex)
    {
        //Avoid releasing if tile was never occupied
        if (!MapManager.Instance.IsTileOccupied(hex))
            return;

        MapManager.Instance.SetUnitOccupied(hex, false);
    }
}
