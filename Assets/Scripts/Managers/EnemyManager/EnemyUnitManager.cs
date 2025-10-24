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

    private Dictionary<int, int> unitSpawnTurn = new();

    private int nextUnitId = 1;
    private int currentTurn = 0;

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
        EventBus.Subscribe<EnemyAIEvents.EnemyTurnStartEvent>(OnEnemyTurnStart);
        EventBus.Subscribe<EnemyAIEvents.EnemyTurnEndEvent>(OnEnemyTurnEnd);
    }

    private void OnDisable()
    {

        EventBus.Unsubscribe<EnemyAIEvents.EnemySpawnRequestEvent>(OnSpawnRequest);
        EventBus.Unsubscribe<EnemyAIEvents.EnemyMoveRequestEvent>(OnMoveRequest);
        EventBus.Unsubscribe<EnemyAIEvents.EnemyAttackRequestEvent>(OnAttackRequest);
        EventBus.Unsubscribe<EnemyAIEvents.EnemyTurnStartEvent>(OnEnemyTurnStart);
        EventBus.Unsubscribe<EnemyAIEvents.EnemyTurnEndEvent>(OnEnemyTurnEnd);
    }

    //Spawn handling: create runtime unit and publish EnemySpawnedEvent
    private void OnSpawnRequest(EnemyAIEvents.EnemySpawnRequestEvent evt)
    {
        Debug.Log($"[EnemyUnitManager] Received Spawn Request for {evt.UnitType}");
        //Find a prefab by unit type
        GameObject prefab = unitPrefabs.Find(p => p.name == evt.UnitType);
        Vector2Int spawnHex = EnemyBaseManagerFindBaseHex(evt.BaseId);
        Vector3 world = MapManager.Instance.HexToWorld(spawnHex);
        world.y += (unitHeightOffset + 0.5f);

        if (prefab == null)
        {
            Debug.LogWarning($"[EnemyUnitManager] Prefab for '{evt.UnitType}' not found.");
            return;
        }

        var unitGO = Instantiate(prefab, world, Quaternion.identity);
        unitGO.name = $"Enemy_{evt.UnitType}_{nextUnitId}";
        RegisterUnit(unitGO, evt.BaseId, evt.UnitType, spawnHex);
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

        unitSpawnTurn[id] = currentTurn;

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
            Debug.Log($"[EnemyUnitManager] Unit {evt.UnitId} cannot move this turn (spawned recently).");
            return;
        }

        Vector2Int from = unitPositions[evt.UnitId];
        Vector2Int to = evt.Destination;

        if (!MapManager.Instance.CanUnitStandHere(to))
            return;

        //Check occupancy
        if (MapManager.Instance.IsTileOccupied(to) && !IsAnyUnitAt(to))
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

        EventBus.Publish(new EnemyAIEvents.EnemyAttackedEvent(evt.AttackerId, evt.TargetId));
    }
    private void OnEnemyTurnStart(EnemyAIEvents.EnemyTurnStartEvent evt)
    {
        currentTurn = evt.Turn;
    }

    private void OnEnemyTurnEnd(EnemyAIEvents.EnemyTurnEndEvent evt)
    {
        currentTurn++;
    }

    //Check if unit can move this turn
    public bool CanUnitMove(int id)
    {
        if (!unitPositions.ContainsKey(id)) 
            return false;
        return currentTurn > unitSpawnTurn[id];
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

    //Helper to find base hex (if EnemyBaseManager not exposed directly)
    private Vector2Int EnemyBaseManagerFindBaseHex(int baseId)
    {
        EnemyBaseManager ebm = FindFirstObjectByType<EnemyBaseManager>();
        if (ebm == null) 
            return Vector2Int.zero;
        return ebm.GetBasePosition(baseId);
    }
}
