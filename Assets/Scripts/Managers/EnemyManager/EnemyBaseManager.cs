//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class EnemyBaseAI : ISubAI
//{
//    private IAIContext context;
//    private IAIActor actor;
//    private AIUnlockSystem unlockSystem;
//    private float delay = 1f;

//    public void Initialize(IAIContext context, IAIActor actor)
//    {
//        this.context = context;
//        this.actor = actor;

//        unlockSystem = Object.FindFirstObjectByType<AIUnlockSystem>();
//        if (unlockSystem == null)
//            Debug.LogError("[EnemyBaseAI] No AIUnlockSystem found in scene!");
//    }

//    public void SetUnlockSystem(AIUnlockSystem system)
//    {
//        unlockSystem = system;
//    }

//    public IEnumerator ExecuteStepByStep()
//    {
//        var baseIds = context.GetOwnedBaseIds();
//        if (baseIds == null || baseIds.Count == 0)
//            yield break;;

//        int currentTurn = context.GetTurnNumber();

//        if (unlockSystem != null)
//            unlockSystem.UpdateUnlocks(currentTurn);

//        float spawnProbability = 0.45f;

//        foreach (var baseId in baseIds)
//        {
//            int currentHP = context.GetBaseHP(baseId);
//            if (currentHP <= 0)
//            {
//                Debug.Log($"[EnemyBaseAI] EnemyBase {baseId} destroyed, skipping.");
//                continue;
//            }

//            if (Random.value > spawnProbability)
//            {
//                Debug.Log($"[EnemyBaseAI] Base {baseId} skipped spawning this turn (p={spawnProbability}).");
//                continue;
//            }

//            //Skip spawning if the base already has a unit stationed.
//            if (context.IsBaseOccupied(baseId))
//            {
//                Debug.Log($"[EnemyBaseAI] EnemyBase {baseId} occupied, skip spawning.");
//                continue;
//            }

//            //Checks whether the base currently houses fewer than 3 units
//            if (!context.CanProduceUnit(baseId))
//            {
//                Debug.Log($"[EnemyBaseAI] EnemyBase {baseId} can't produce unit yet.");
//                continue;
//            }

//            //Get unlocked unit list
//            List<string> availableUnits = unlockSystem.GetUnlockedUnits();
//            if (availableUnits == null || availableUnits.Count == 0)
//            {
//                Debug.Log("[EnemyBaseAI] No unlocked units available for spawning.");
//                continue;
//            }

//            //Randomly select one unlocked unit type to spawn
//            string chosenUnit = availableUnits[Random.Range(0, availableUnits.Count)];

//            actor.SpawnUnit(baseId, chosenUnit);
//            Debug.Log($"[EnemyBaseAI] EnemyBase {baseId} spawned {chosenUnit} (Turn {currentTurn}).");

//            yield return new WaitForSeconds(delay / AIController.AISpeedMultiplier);
//        }
//    }
//}

using System.Collections.Generic;
using UnityEngine;
using static EnemyAIEvents;

/// <summary>
/// Tracks enemy bases, HP, housed units and performs spawn logic.
/// Publishes EnemySpawnRequestEvent when a base decides to spawn a unit.
/// </summary>
public class EnemyBaseManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapGenerator mapGenerator;

    [Header("Spawn Settings")]
    [SerializeField] private int minBaseHP = 20;
    [SerializeField] private int maxBaseHP = 35;
    [SerializeField] private int maxUnitsPerBase = 3;
    [SerializeField] private float spawnProbability = 0.45f;

    //Runtime containers
    private Dictionary<int, Vector2Int> basePositions = new();
    private Dictionary<int, int> baseHP = new();
    private Dictionary<int, int> baseUnitCount = new(); //Track how many units are housed in each base
    private Dictionary<int, GameObject> baseObjects = new();

    private int nextBaseId = 1;

    private void OnEnable()
    {
        EventBus.Subscribe<EnemyAIEvents.MapReadyEvent>(OnMapReady);
        EventBus.Subscribe<EnemyAIEvents.ExecuteBasePhaseEvent>(OnExecuteBasePhase);
        EventBus.Subscribe<EnemyAIEvents.EnemySpawnedEvent>(OnEnemySpawnedNotification);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EnemyAIEvents.MapReadyEvent>(OnMapReady);
        EventBus.Unsubscribe<EnemyAIEvents.ExecuteBasePhaseEvent>(OnExecuteBasePhase);
        EventBus.Unsubscribe<EnemyAIEvents.EnemySpawnedEvent>(OnEnemySpawnedNotification);
    }

    private void OnMapReady(EnemyAIEvents.MapReadyEvent evt)
    {
        mapGenerator = evt.Map;
        DiscoverBases();
    }

    private void DiscoverBases()
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
            Vector2Int baseHex = MapManager.Instance.WorldToHex(baseGO.transform.position);

            basePositions[baseId] = baseHex;
            baseHP[baseId] = Random.Range(minBaseHP, maxBaseHP + 1); //HP: 20 ~ 35
            baseUnitCount[baseId] = 0; //Start with no units housed
            baseObjects[baseId] = baseGO;
        }
    }

    private void OnExecuteBasePhase(EnemyAIEvents.ExecuteBasePhaseEvent evt)
    {
        int turn = evt.Turn;

        foreach (var kvp in basePositions)
        {
            int baseId = kvp.Key;
            Debug.Log($"[EnemyBaseManager] Checking base {baseId}");

            if (IsBaseDestroyed(baseId))
            {
                Debug.Log($"[EnemyBaseManager] Base {baseId} destroyed, skipping");
                continue;
            }

            if (IsBaseOccupied(baseId))
            {
                Debug.Log($"[EnemyBaseManager] Base {baseId} occupied, skipping");
                continue;
            }

            if (!CanProduceUnit(baseId))
            {
                Debug.Log($"[EnemyBaseManager] Base {baseId} cannot produce unit, skipping");
                continue;
            }

            string chosen = "Builder";
            Debug.Log($"[EnemyBaseManager] Base {baseId} ready to spawn {chosen}");

            //Request spawn
            Debug.Log($"[EnemyBaseManager] Requesting spawn for unit '{chosen}' on {baseId}");
            EventBus.Publish(new EnemySpawnRequestEvent(baseId, chosen));
            Debug.Log("[EnemyBaseManager] Spawn event published");
        }
    }

    //Spawn pool implementation per spec (priority brackets)
    private string SelectUnitToSpawn()
    {
        //Count existing enemy unit types from EnemyUnitManager
        var unitManager = EnemyUnitManager.Instance;
        if (unitManager == null) 
            return "Scout";

        int builderCount = unitManager.CountUnitsOfType("Builder");
        int bomberCount = unitManager.CountUnitsOfType("Bomber");
        int totalShips = unitManager.TotalUnitCount();

        //Priority bracket 1: spawn Builder if enemies have <2 builders in totol
        if (builderCount < 2)
        {
            return "Builder";
        }

        //Otherwise use weighted chances - Scout 40%, Tanker 40%, Shooter 20%

        //Special: If enemies > 10 ships and do not control a Bomber, Bomber prioritized
        if (totalShips > 10 && bomberCount == 0)
        {
            return "Bomber";
        }

        float roll = Random.value;
        if (roll < 0.4f) 
            return "Scout";
        if (roll < 0.8f) 
            return "Tanker";
        return "Shooter";
    }

    public bool CanProduceUnit(int baseId)
    {
        return baseUnitCount.TryGetValue(baseId, out var count) && count < maxUnitsPerBase;
    }

    public void RegisterSpawnedUnitAtBase(int baseId)
    {
        if (baseUnitCount.ContainsKey(baseId)) baseUnitCount[baseId]++;
    }

    public void UnregisterUnitFromBase(int baseId)
    {
        if (baseUnitCount.ContainsKey(baseId)) baseUnitCount[baseId] = Mathf.Max(0, baseUnitCount[baseId] - 1);
    }

    public bool IsBaseOccupied(int baseId)
    {
        if (!basePositions.TryGetValue(baseId, out var pos)) 
            return false;
        return EnemyUnitManager.Instance.IsAnyUnitAt(pos) || MapManager.Instance.IsTileOccupied(pos);
    }

    public Vector2Int GetBasePosition(int baseId) => basePositions.TryGetValue(baseId, out var p) ? p : Vector2Int.zero;
    public int GetBaseHP(int baseId) => baseHP.TryGetValue(baseId, out var hp) ? hp : 0;
    public bool IsBaseDestroyed(int baseId) => GetBaseHP(baseId) <= 0;

    private void OnEnemySpawnedNotification(EnemyAIEvents.EnemySpawnedEvent evt)
    {
        //When UnitManager notifies a unit was spawned, increment housed count
        if (evt.BaseId != 0)
            RegisterSpawnedUnitAtBase(evt.BaseId);
    }    
}
