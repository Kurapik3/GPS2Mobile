using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents an AI-controlled enemy unit on the map.
/// HP, type from UnitDataBase.
/// </summary>
public class EnemyUnitManager : MonoBehaviour
{
    public static EnemyUnitManager Instance { get; private set; }

    [Header("Runtime")]
    [SerializeField] public List<GameObject> unitPrefabs;
    [SerializeField] private UnitDatabase unitDatabase;
    [SerializeField] private FogSystem fogSystem;

    //Runtime containers
    private Dictionary<int, Vector2Int> unitPositions = new();
    public Dictionary<int, Vector2Int> UnitPositions => unitPositions;
    private Dictionary<int, string> unitTypes = new();
    private Dictionary<int, GameObject> unitObjects = new();
    public IReadOnlyDictionary<int, GameObject> UnitObjects => unitObjects;
    private Dictionary<int, int> unitHousedBase = new(); //Track which base a unit is housed in

    public enum AIState
    {
        Dormant,
        Aggressive
    }
    private Dictionary<int, AIState> unitStates = new();
    private HashSet<int> stateLockedUnits = new();
    private Dictionary<int, AIState> pendingStateChange = new();

    public bool IsSpawning { get; set; } = false;

    private HashSet<int> justSpawnedUnits = new();

    private int nextUnitId = 1;
    public int NextUnitId => nextUnitId;

    private HashSet<int> actedThisTurn = new HashSet<int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #region Registration & Destruction
    public void RegisterUnit(GameObject go, int baseId, string type, Vector2Int hex)
    {
        int id = nextUnitId++;

        EnemyUnit unitGO = go.GetComponent<EnemyUnit>();
        if (unitGO != null)
        {
            var data = unitDatabase.GetUnitByName(type);
            int hp = data != null ? data.hp : 0;
            unitGO.Initialize(id, type, hp, MapManager.Instance.GetTile(hex));
        }

        unitObjects[id] = go;
        unitPositions[id] = hex;
        unitTypes[id] = type;
        unitHousedBase[id] = baseId;

        justSpawnedUnits.Add(id);
        unitStates[id] = AIState.Dormant;

        //Mark map occupied
        MapManager.Instance.SetUnitOccupied(hex, true);

        EventBus.Publish(new EnemyAIEvents.EnemySpawnedEvent(id, baseId, type, hex));

        UpdateEnemyVisibility();
    }

    public void KillUnit(int unitId)
    {
        if (!unitObjects.ContainsKey(unitId))
            return;

        if (unitHousedBase.TryGetValue(unitId, out int baseId))
        {
            if (EnemyBaseManager.Instance.Bases.TryGetValue(baseId, out var baseObj))
            {
                baseObj.OnUnitDestroyed();
            }
        }

        var obj = unitObjects[unitId];
        if (obj != null)
        {
            EnemyUnit unitGO = obj.GetComponent<EnemyUnit>();
            if (unitGO != null && unitGO.currentTile != null)
                unitGO.currentTile.currentEnemyUnit = null;
            Destroy(obj);
        }

        unitObjects.Remove(unitId);
        unitPositions.Remove(unitId);
        unitTypes.Remove(unitId);
        unitHousedBase.Remove(unitId);
        unitStates.Remove(unitId);
        justSpawnedUnits.Remove(unitId);

        Debug.Log($"[EnemyUnitManager] Unit {unitId} destroyed and removed from manager.");
    }
    #endregion

    #region State Management
    public void LockState(int unitId)
    {
        stateLockedUnits.Add(unitId);
    }

    public void UnlockState(int unitId)
    {
        stateLockedUnits.Remove(unitId);
        if (pendingStateChange.TryGetValue(unitId, out var nextState))
        {
            unitStates[unitId] = nextState;
            pendingStateChange.Remove(unitId);
        }
    }

    public void TrySetState(int unitId, AIState newState)
    {
        if (stateLockedUnits.Contains(unitId))
        {
            pendingStateChange[unitId] = newState;
            return;
        }

        unitStates[unitId] = newState;
    }

    public AIState GetUnitState(int unitId)
    {
        return unitStates.TryGetValue(unitId, out var state) ? state : AIState.Dormant;
    }

    public void ClearJustSpawnedUnits() => justSpawnedUnits.Clear();
    #endregion

    #region Queries
    //Check if unit can move this turn
    public bool CanUnitMove(int id)
    {
        return unitPositions.ContainsKey(id) && !justSpawnedUnits.Contains(id);
    }
    public bool CanUnitAttack(int id)
    {
        return !justSpawnedUnits.Contains(id);
    }
    public List<int> GetOwnedUnitIds() => new List<int>(unitPositions.Keys);
    public Vector2Int GetUnitPosition(int id) => unitPositions.TryGetValue(id, out var pos) ? pos : Vector2Int.zero;
    public string GetUnitType(int id) => unitTypes.TryGetValue(id, out var type) ? type : null;
    public int GetUnitAttackRange(int id)
    {
        if (unitTypes.TryGetValue(id, out var type))
        {
            var data = unitDatabase?.GetUnitByName(type);
            if (data == null)
                return 0;
            return data.range;
        }
        return 0;
    }
    //Get player unit's attack power from database
    public int GetPlayerUnitAttackPower(int id)
    {
        if (!unitTypes.TryGetValue(id, out string type))
            return 0;
        var data = unitDatabase?.GetUnitByName(type);
        return data != null ? data.attack : 1;
    }
    public int GetUnitMoveRange(int id)
    {
        if (unitTypes.TryGetValue(id, out var type))
        {
            var data = unitDatabase?.GetUnitByName(type);
            if (data == null)
                return 0;

            if (type == "Builder")
                return 2;
            return 1;
        }
        return 0;
    }

    public bool IsUnitType(int id, string type)
    {
        if (GetUnitType(id) == type)
            return true;
        return false;
    }

    public bool IsAnyUnitAt(Vector2Int hex)
    {
        foreach (var pos in unitPositions.Values)
            if (pos == hex)
                return true;
        return false;
    }
    public bool IsUnitVisibleToPlayer(int id)
    {
        if (fogSystem == null)
            return true;
        var pos = GetUnitPosition(id);
        return fogSystem.revealedTiles.Contains(pos);
    }

    public int CountUnitsOfType(string type)
    {
        int count = 0;
        foreach (var t in unitTypes.Values)
            if (t == type)
                count++;
        return count;
    }

    public int TotalUnitCount() => unitPositions.Count;

    public void MarkUnitAsActed(int unitId) => actedThisTurn.Add(unitId);
    public bool HasUnitActedThisTurn(int unitId) => actedThisTurn.Contains(unitId);
    public void ClearActedUnits() => actedThisTurn.Clear();
    #endregion

    public void UpdateEnemyVisibility()
    {
        foreach (var kv in unitObjects)
        {
            int id = kv.Key;
            GameObject enemy = kv.Value;
            bool visible = IsUnitVisibleToPlayer(id);
            SetLayerRecursively(enemy, visible ? LayerMask.NameToLayer("Enemy") : LayerMask.NameToLayer("EnemyHidden"));
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    //For savedd states - Ashley
    public List<UnitBase> GetAllUnits()
    {
        List<UnitBase> units = new();
        foreach (var obj in unitObjects.Values)
        {
            var u = obj.GetComponent<UnitBase>();
            if (u != null)
                units.Add(u);
        }
        return units;
    }

    public void ClearAll()
    {
        foreach (var obj in unitObjects.Values)
        {
            if (obj != null)
                Destroy(obj);
        }
        unitObjects.Clear();
        unitPositions.Clear();
        unitTypes.Clear();
        nextUnitId = 1;
    }

    public void RefreshReferences()
    {
        if (fogSystem == null)
        {
            fogSystem = FindFirstObjectByType<FogSystem>();
            if (fogSystem == null)
            {
                Debug.LogWarning("[EnemyUnitManager] FogSystem not found in scene!");
            }
        }

        if(unitDatabase == null)
        {
            unitDatabase = FindFirstObjectByType<UnitDatabase>();
            if(unitDatabase == null)
            {
                Debug.LogWarning("[EnemyUnitManager] UnitDatabase not found in scene!");
            }
        }
    }

    public void SpawnLoadedUnit(GameObject prefab, int id, int baseId, string type, Vector2Int pos, int hp, AIState state, bool justSpawned)
    {
        GameObject go = Instantiate(prefab);
        EnemyUnit unitComp = go.GetComponent<EnemyUnit>();

        unitComp.Initialize(id, type, hp, MapManager.Instance.GetTile(pos));

        unitObjects[id] = go;
        unitPositions[id] = pos;
        unitTypes[id] = type;
        unitHousedBase[id] = baseId;
        unitStates[id] = state;

        if (justSpawned)
            justSpawnedUnits.Add(id);

        MapManager.Instance.SetUnitOccupied(pos, true);
    }
    public int GetBaseId(int id)
    {
        return unitHousedBase.ContainsKey(id) ? unitHousedBase[id] : -1;
    }

    public bool IsJustSpawned(int id)
    {
        return justSpawnedUnits.Contains(id);
    }

    public void FullReset()
    {
        unitPositions.Clear();
        unitTypes.Clear();
        unitObjects.Clear();
        unitHousedBase.Clear();
        unitStates.Clear();
        stateLockedUnits.Clear();
        pendingStateChange.Clear();
        justSpawnedUnits.Clear();
        actedThisTurn.Clear();
    }

    public void SetNextUnitId(int id)
    {
        nextUnitId = id;
    }

}