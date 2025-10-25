using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TurnManager;

/// <summary>
/// Tracks enemy units (positions, types, HP), handles spawn/move/attack requests.
/// Exposes a simple Instance for queries used by AIs.
/// </summary>
public class EnemyUnitManager : MonoBehaviour
{
    public static EnemyUnitManager Instance { get; private set; }

    [Header("Runtime")]
    [SerializeField] private List<GameObject> unitPrefabs;
    [SerializeField] private UnitDatabase unitDatabase;
    [SerializeField] private float unitHeightOffset = 2f;

    //Runtime containers
    private Dictionary<int, Vector2Int> unitPositions = new();
    private Dictionary<int, string> unitTypes = new();
    private Dictionary<int, int> unitHP = new();
    private Dictionary<int, GameObject> unitObjects = new();
    private Dictionary<int, int> unitHousedBase = new(); //Track which base a unit is housed in

    private HashSet<int> justSpawnedUnits = new();

    private int nextUnitId = 1;

    private void Awake()
    {
        Instance = this;;
    }

    private void OnEnable()
    {
        Debug.Log("[EnemyUnitManager] OnEnable called, subscribing events");
        EventBus.Subscribe<EnemyAIEvents.EnemySpawnRequestEvent>(OnSpawnRequest);
        EventBus.Subscribe<EnemyAIEvents.EnemyMoveRequestEvent>(OnMoveRequest);
        EventBus.Subscribe<EnemyAIEvents.EnemyAttackRequestEvent>(OnAttackRequest);
        EventBus.Subscribe<EnemyAIEvents.EnemyTurnEndEvent>(OnEnemyTurnEnd);
    }

    private void OnDisable()
    {

        EventBus.Unsubscribe<EnemyAIEvents.EnemySpawnRequestEvent>(OnSpawnRequest);
        EventBus.Unsubscribe<EnemyAIEvents.EnemyMoveRequestEvent>(OnMoveRequest);
        EventBus.Unsubscribe<EnemyAIEvents.EnemyAttackRequestEvent>(OnAttackRequest);
        EventBus.Unsubscribe<EnemyAIEvents.EnemyTurnEndEvent>(OnEnemyTurnEnd);
    }

    //Spawn handling: create runtime unit and publish EnemySpawnedEvent
    private void OnSpawnRequest(EnemyAIEvents.EnemySpawnRequestEvent evt)
    {
        Debug.Log($"[EnemyUnitManager] Received Spawn Request for {evt.UnitType}");

        //Find prefab by unit type
        GameObject prefab = unitPrefabs.Find(p => p.name == evt.UnitType);
        if (prefab == null)
        {
            Debug.LogWarning($"[EnemyUnitManager] Prefab for '{evt.UnitType}' not found.");
            return;
        }

        //Get base hex from EnemyBaseManager
        Vector2Int spawnHex = GetBaseSpawnHex(evt.BaseId);
        if (spawnHex == Vector2Int.zero)
        {
            Debug.LogWarning($"[EnemyUnitManager] Base #{evt.BaseId} has invalid spawn location.");
            return;
        }

        Vector3 world = MapManager.Instance.HexToWorld(spawnHex);
        world.y += (unitHeightOffset + 0.5f);

        //Instantiate unit
        var unitGO = Instantiate(prefab, world, Quaternion.identity);
        unitGO.name = $"Enemy_{evt.UnitType}_{nextUnitId}";

        RegisterUnit(unitGO, evt.BaseId, evt.UnitType, spawnHex);
    }

    private Vector2Int GetBaseSpawnHex(int baseId)
    {
        var ebm = EnemyBaseManager.Instance;
        if (ebm == null)
            return Vector2Int.zero;

        if (!ebm.Bases.TryGetValue(baseId, out var enemyBase) || enemyBase == null)
            return Vector2Int.zero;

        return enemyBase.currentTile != null ? enemyBase.currentTile.HexCoords : Vector2Int.zero;
    }


    private void RegisterUnit(GameObject go, int baseId, string type, Vector2Int hex)
    {
        int id = nextUnitId++;
        unitObjects[id] = go;
        unitPositions[id] = hex;
        unitTypes[id] = type;
        unitHousedBase[id] = baseId;
        var data = unitDatabase.GetUnitByName(type);
        unitHP[id] = data != null ? data.hp : 10;

        justSpawnedUnits.Add(id);

        //Mark map occupied
        MapManager.Instance.SetUnitOccupied(hex, true);

        EventBus.Publish(new EnemyAIEvents.EnemySpawnedEvent(id, baseId, type, hex));
    }

    private void OnMoveRequest(EnemyAIEvents.EnemyMoveRequestEvent evt)
    {
        if (!unitPositions.ContainsKey(evt.UnitId)) 
            return;

        //Check if unit can move
        if (!CanUnitMove(evt.UnitId))
        {
            return;
        }

        Vector2Int from = unitPositions[evt.UnitId];
        Vector2Int to = evt.Destination;

        if (!MapManager.Instance.CanUnitStandHere(to))
            return;

        //Check occupancy
        if (MapManager.Instance.IsTileOccupied(to))
            return;

        //Release old tile
        MapManager.Instance.SetUnitOccupied(from, false);

        unitPositions[evt.UnitId] = to;
        MapManager.Instance.SetUnitOccupied(to, true);

        //Move GameObject visually
        if (unitObjects.TryGetValue(evt.UnitId, out var go))
        {
            Vector3 world = MapManager.Instance.HexToWorld(to);
            world.y += unitHeightOffset;
            StartCoroutine(SmoothMove(go, world));
        }

        EventBus.Publish(new EnemyAIEvents.EnemyMovedEvent(evt.UnitId, from, to));
    }

    private IEnumerator SmoothMove(GameObject unit, Vector3 destination)
    {
        Vector3 start = unit.transform.position;
        float t = 0f;
        float duration = 0.5f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            Vector3 nextPos = Vector3.Lerp(start, destination, t);
            unit.transform.position = nextPos;
            yield return null;
        }

        unit.transform.position = destination;
    }

    //Real damage logic will be handled by Combat system listening to this notification
    private void OnAttackRequest(EnemyAIEvents.EnemyAttackRequestEvent evt)
    {
        if (!unitObjects.ContainsKey(evt.AttackerId)) 
            return;

        int damage = GetUnitAttackPower(evt.AttackerId);

        //================= Enemy attacking another enemy (temp - for testing purpose) ===================
        if (unitObjects.ContainsKey(evt.TargetId))
        {
            TakeDamage(evt.TargetId, damage);
            EventBus.Publish(new EnemyAIEvents.EnemyAttackedEvent(evt.AttackerId, evt.TargetId));
            return;
        }

        //Enemy attacking player
        //if (PlayerUnitManager.Instance != null)
        //{
        //    PlayerUnitManager.Instance.TakeDamage(evt.TargetId, damage);
        //    EventBus.Publish(new EnemyAIEvents.EnemyAttackedEvent(evt.AttackerId, evt.TargetId));
        //    return;
        //}
        Debug.LogWarning($"[EnemyUnitManager] Target {evt.TargetId} not found");
    }

    //Get player unit's attack power from database
    public int GetUnitAttackPower(int id)
    {
        if (!unitTypes.TryGetValue(id, out string type)) 
            return 0;
        var data = unitDatabase?.GetUnitByName(type);
        return data != null ? data.attack : 1;
    }

    public void TakeDamage(int unitId, int amount)
    {
        if (!unitHP.ContainsKey(unitId)) 
            return;

        unitHP[unitId] -= amount;
        Debug.Log($"[EnemyUnitManager] Unit {unitId} took {amount} damage, HP now {unitHP[unitId]}");

        if (unitHP[unitId] <= 0)
            KillUnit(unitId);
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

        Debug.Log($"[EnemyUnitManager] Unit {unitId} destroyed and removed from manager.");
    }

    private void OnEnemyTurnEnd(EnemyAIEvents.EnemyTurnEndEvent evt)
    {
        justSpawnedUnits.Clear();
    }

    //Check if unit can move this turn
    public bool CanUnitMove(int id)
    {
        if (!unitPositions.ContainsKey(id)) 
            return false;

        if (justSpawnedUnits.Contains(id))
            return false;

        return true;
    }

    //Query helpers used by AIs
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

    public bool IsAnyUnitAt(Vector2Int hex)
    {
        foreach (var pos in unitPositions.Values)
            if (pos == hex) 
                return true;
        return false;
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
}
