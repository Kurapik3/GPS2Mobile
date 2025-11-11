using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks enemy units (positions, types, HP), handles spawn/move/attack requests.
/// Exposes a simple Instance for queries used by AIs.
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
    private Dictionary<int, int> unitHP = new();
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
        unitObjects[id] = go;
        unitPositions[id] = hex;
        unitTypes[id] = type;
        unitHousedBase[id] = baseId;

        var data = unitDatabase.GetUnitByName(type);
        unitHP[id] = data != null ? data.hp : 0;

        justSpawnedUnits.Add(id);
        unitStates[id] = AIState.Dormant;

        //Mark map occupied
        MapManager.Instance.SetUnitOccupied(hex, true);

        EventBus.Publish(new EnemyAIEvents.EnemySpawnedEvent(id, baseId, type, hex));
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

        Destroy(unitObjects[unitId]);
        unitObjects.Remove(unitId);
        unitPositions.Remove(unitId);
        unitTypes.Remove(unitId);
        unitHousedBase.Remove(unitId);
        unitHP.Remove(unitId);
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
    public List<int> GetOwnedUnitIds() => new List<int>(unitPositions.Keys);
    public Vector2Int GetUnitPosition(int id) => unitPositions.TryGetValue(id, out var pos) ? pos : Vector2Int.zero;
    public string GetUnitType(int id) => unitTypes.TryGetValue(id, out var type) ? type : null;
    public int GetUnitHP(int id) => unitHP.TryGetValue(id, out var hp) ? hp : 0;
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
            return data.movement;
        }
        return 0;
    }

    public bool IsBuilderUnit(int id)
    {
        if (GetUnitType(id) == "Builder")
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
    #endregion

    #region Damage
    public void TakeDamage(int unitId, int amount)
    {
        if (!unitHP.ContainsKey(unitId))
            return;

        unitHP[unitId] -= amount;
        Debug.Log($"[EnemyUnitManager] Unit {unitId} took {amount} damage, HP now {unitHP[unitId]}");

        if (unitHP[unitId] <= 0)
            KillUnit(unitId);
    }
    #endregion

    public void UpdateEnemyVisibility()
    {
        foreach (var kv in unitObjects)
        {
            int id = kv.Key;
            GameObject enemy = kv.Value;
            bool visible = IsUnitVisibleToPlayer(id);
            foreach (var r in enemy.GetComponentsInChildren<Renderer>())
            {
                r.enabled = visible;
            }
            foreach (var c in enemy.GetComponentsInChildren<Collider>())
            {
                c.enabled = visible;
            }
        }
    }
}