using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static EnemyAIEvents;

public class EnemyBaseManager : MonoBehaviour
{
    public static EnemyBaseManager Instance { get; private set; }

    [Header("Spawn Settings")]
    [SerializeField] private float spawnDelay = 0.5f;

    private readonly Dictionary<int, EnemyBase> bases = new();
    public IReadOnlyDictionary<int, EnemyBase> Bases => bases;

    private int nextBaseId = 1;

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

    private void OnEnable()
    {
        EventBus.Subscribe<ExecuteBasePhaseEvent>(OnExecuteBasePhase);
        EventBus.Subscribe<EnemySpawnedEvent>(OnEnemySpawnedNotification);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<ExecuteBasePhaseEvent>(OnExecuteBasePhase);
        EventBus.Unsubscribe<EnemySpawnedEvent>(OnEnemySpawnedNotification);
    }

    public int RegisterBase(EnemyBase enemyBase)
    {
        int id = nextBaseId++;
        bases[id] = enemyBase;
        Debug.Log($"[EnemyBaseManager] Registered base #{id} at {enemyBase.currentTile?.HexCoords}");
        return id;
    }

    public void UnregisterBase(EnemyBase enemyBase)
    {
        if (enemyBase == null)
        {
            Debug.LogWarning("[EnemyBaseManager] UnregisterBase called with null.");
            return;
        }

        int keyToRemove = -1;
        foreach (var kvp in bases)
        {
            if (kvp.Value == enemyBase)
            {
                keyToRemove = kvp.Key;
                break;
            }
        }

        if (keyToRemove != -1)
        {
            bases.Remove(keyToRemove);
            Debug.Log($"[EnemyBaseManager] Unregistered base #{keyToRemove}");
        }
    }

    public void OnBaseDestroyed(EnemyBase destroyedBase)
    {
        if (destroyedBase == null)
        {
            Debug.LogWarning("[EnemyBaseManager] OnBaseDestroyed called with null.");
            return;
        }

        UnregisterBase(destroyedBase);

        if (bases.Count <= 0)
        {
            Debug.Log("[EnemyBaseManager] All enemy bases destroyed! Player wins?");
            //eg: EventBus.Publish(new XXXEvents.AllEnemyBasesDestroyedEvent());
            //Wait for global game events to control the game state____
        }
    }

    private void OnExecuteBasePhase(ExecuteBasePhaseEvent evt)
    {
        StartCoroutine(SpawnUnitsStepByStep(evt.Turn, evt.OnCompleted));
    }

    private IEnumerator SpawnUnitsStepByStep(int turn, Action onCompleted)
    {
        //Randomize base order to avoid always the same bases to spawn builder units
        var baseList = bases.ToList();
        baseList = baseList.OrderBy(x => UnityEngine.Random.value).ToList();

        foreach (var kvp in baseList)
        {
            int id = kvp.Key;
            EnemyBase b = kvp.Value;

            if (b == null || b.IsDestroyed)
                continue;

            if (b.currentUnits >= b.maxUnits)
                continue;

            //Check if base tile is already occupied by another unit
            if (b.currentTile != null && EnemyUnitManager.Instance.IsAnyUnitAt(b.currentTile.HexCoords))
            {
                Debug.Log($"[EnemyBaseManager] Base {id} tile occupied, skipping spawn.");
                continue;
            }

            //Decide which unit to spawn
            string chosen = SelectUnitToSpawn();
            Debug.Log($"[EnemyBaseManager] Base {id} will spawn {chosen}");

            EventBus.Publish(new EnemySpawnRequestEvent(id, chosen));

            while (EnemyUnitManager.Instance.IsSpawning)
                yield return null;

            //Wait a small delay before spawning the next unit
            yield return new WaitForSeconds(spawnDelay);
        }

        onCompleted?.Invoke();
    }

    private string SelectUnitToSpawn()
    {
        var unitManager = EnemyUnitManager.Instance;
        if (unitManager == null) 
            return "Scout";

        int builderCount = unitManager.CountUnitsOfType("Builder");
        int bomberCount = unitManager.CountUnitsOfType("Bomber");
        int totalShips = unitManager.TotalUnitCount();

        if (builderCount < 2) 
            return "Builder";
        if (totalShips > 10 && bomberCount == 0) 
            return "Bomber";

        float roll = UnityEngine.Random.value;
        if (roll < 0.4f) 
            return "Scout";
        if (roll < 0.8f) 
            return "Tanker";
        return "Shooter";
    }

    private void OnEnemySpawnedNotification(EnemySpawnedEvent evt)
    {
        if (!bases.ContainsKey(evt.BaseId)) 
            return;
        bases[evt.BaseId].OnUnitSpawned();
    }
}
