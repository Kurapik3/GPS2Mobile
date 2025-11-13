using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static SeaMonsterEvents;

/// <summary>
/// Manages all active sea monsters, their spawn, death, and provides a central list for AI.
/// </summary>
public class SeaMonsterManager : MonoBehaviour
{
    public static SeaMonsterManager Instance { get; private set; }
    private Transform mainCameraTransform;

    [Header("References")]
    [SerializeField] private SeaMonsterSpawner spawner;
    [SerializeField] private FogSystem fogSystem;
    [SerializeField] private AudioClip krakenWarningSound;
    [SerializeField] private AudioClip krakenMoveSound;
    [SerializeField] private AudioClip krakenAttackSound;
    [SerializeField] private AudioClip krakenDieSound;
    [SerializeField] private AudioClip turtleMoveSound;
    [SerializeField] private AudioClip turtleDieSound;

    [Header("Feedback Settings")]
    [SerializeField] private float preSpawnDelay = 1f;
    [SerializeField] private float shakeIntensity = 0.6f;
    [SerializeField] private float shakeDuration = 0.5f;

    private readonly List<SeaMonsterBase> activeMonsters = new();
    public IReadOnlyList<SeaMonsterBase> ActiveMonsters => activeMonsters;

    private readonly Dictionary<int, Vector2Int> monsterPositions = new();
    public IReadOnlyDictionary<int, Vector2Int> MonsterPositions => monsterPositions;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);
        else
            Instance = this;

        if (Camera.main != null)
            mainCameraTransform = Camera.main.transform;
        else
            Debug.LogWarning("No main camera found!");
    }

    private void OnEnable()
    {
        EventBus.Subscribe<SeaMonsterTurnStartedEvent>(OnTurnStarted);
        EventBus.Subscribe<SeaMonsterKilledEvent>(OnSeaMonsterKilled);
        EventBus.Subscribe<SeaMonsterMoveEvent>(OnMonsterMoved);
        EventBus.Subscribe<KrakenAttacksUnitEvent>(OnKrakenAttackUnit);
        EventBus.Subscribe<KrakenAttacksMonsterEvent>(OnKrakenAttackMonster);
        EventBus.Subscribe<TurtleWallBlockEvent>(OnTurtleWallBlock);
        EventBus.Subscribe<TurtleWallUnblockEvent>(OnTurtleWallUnblock);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<SeaMonsterTurnStartedEvent>(OnTurnStarted);
        EventBus.Unsubscribe<SeaMonsterKilledEvent>(OnSeaMonsterKilled);
        EventBus.Unsubscribe<TurtleWallBlockEvent>(OnTurtleWallBlock);
        EventBus.Unsubscribe<TurtleWallUnblockEvent>(OnTurtleWallUnblock);
    }

    private void Start()
    {
        EventBus.Publish(new SeaMonsterSystemReadyEvent(this));
    }

    private void OnTurnStarted(SeaMonsterTurnStartedEvent evt)
    {
        int turn = evt.Turn;

        //Start at turn 10, then every 4 turns
        if (turn == 10 || (turn > 10 && (turn - 10) % 4 == 0))
        {
            StartCoroutine(SpawnSequence(turn));
        }
    }

    private IEnumerator SpawnSequence(int turn)
    {
        //SeaMonster warning (turn 10 only)
        if (turn == 10)
        {
            EventBus.Publish(new KrakenPreSpawnWarningEvent(turn));
            if (krakenWarningSound)
                AudioSource.PlayClipAtPoint(krakenWarningSound, mainCameraTransform.position);

            yield return StartCoroutine(ShakeCamera());
            yield return new WaitForSeconds(preSpawnDelay);
        }

        //Spawn random monster
        SeaMonsterBase monster = spawner.SpawnRandomMonster();
        if (monster != null)
        {
            RegisterMonster(monster);

            Vector2Int tilePos = monster.CurrentTile != null ? monster.CurrentTile.HexCoords : Vector2Int.zero;
            EventBus.Publish(new SeaMonsterSpawnedEvent(monster, tilePos));
        }
    }

    public void RegisterMonster(SeaMonsterBase monster)
    {
        if (monster == null) 
            return;
        if (!activeMonsters.Contains(monster))
            activeMonsters.Add(monster);

        Vector2Int pos = monster.CurrentTile != null ? monster.CurrentTile.HexCoords : Vector2Int.zero;
        monsterPositions[monster.MonsterId] = pos;

        UpdateSeaMonsterVisibility();
    }

    private IEnumerator ShakeCamera()
    {
        if (Camera.main == null) 
            yield break;

        Vector3 origin = mainCameraTransform.position;
        float t = 0f;

        while (t < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;
            mainCameraTransform.position = origin + new Vector3(x, y, 0);
            t += Time.deltaTime;
            yield return null;
        }

        mainCameraTransform.position = origin;
    }

    private void OnSeaMonsterKilled(SeaMonsterKilledEvent evt)
    {
        if (evt.Monster == null) 
            return;

        if(evt.Monster is Kraken)
        {
            if (krakenDieSound != null)
                AudioSource.PlayClipAtPoint(krakenDieSound, mainCameraTransform.position);
        }
        else if (evt.Monster is TurtleWall)
        {
            if (turtleDieSound != null)
                AudioSource.PlayClipAtPoint(turtleDieSound, mainCameraTransform.position);
        }

        if (activeMonsters.Contains(evt.Monster))
            activeMonsters.Remove(evt.Monster);

        monsterPositions.Remove(evt.Monster.MonsterId);
    }

    public List<SeaMonsterBase> GetAllMonsters()
    {
        return new List<SeaMonsterBase>(activeMonsters);
    }

    public SeaMonsterBase GetMonsterById(int monsterId)
    {
        return activeMonsters.Find(m => m.MonsterId == monsterId);
    }

    private void OnMonsterMoved(SeaMonsterMoveEvent evt)
    {
        if (evt.Monster == null)
            return;

        if (evt.Monster is Kraken)
        {
            if (krakenMoveSound != null)
                AudioSource.PlayClipAtPoint(krakenMoveSound, mainCameraTransform.position);
        }
        else if (evt.Monster is TurtleWall)
        {
            if (turtleMoveSound != null)
                AudioSource.PlayClipAtPoint(turtleMoveSound, mainCameraTransform.position);
        }

        monsterPositions[evt.Monster.MonsterId] = evt.To;
        UpdateSeaMonsterVisibility();
    }

    private void OnKrakenAttackUnit(KrakenAttacksUnitEvent evt)
    {
        if (evt.Target == null)
            return;

        //If target is player unit
        if (evt.Target.TryGetComponent<UnitBase>(out UnitBase playerUnit))
        {
            if(krakenAttackSound != null)
                AudioSource.PlayClipAtPoint(krakenAttackSound, mainCameraTransform.position);
            playerUnit.TakeDamage(evt.Damage);

            Debug.Log($"Kraken attacked player unit {playerUnit.unitName} for {evt.Damage} damage!");
        }
        else //If target is enemy unit
        {
            int enemyId = FindEnemyId(evt.Target);
            if (enemyId != -1)
            {
                if (krakenAttackSound != null)
                    AudioSource.PlayClipAtPoint(krakenAttackSound, mainCameraTransform.position);
                EnemyUnitManager.Instance.TakeDamage(enemyId, evt.Damage);
            }
            else
            {
                Debug.LogWarning("Kraken attack target is unknown type!");
            }
        }
    }

    private int FindEnemyId(GameObject target)
    {
        foreach (var kv in EnemyUnitManager.Instance.UnitObjects)
        {
            if (kv.Value == target)
                return kv.Key;
        }
        return -1;
    }

    private void OnKrakenAttackMonster(KrakenAttacksMonsterEvent evt)
    {
        if (evt.Target == null)
            return;

        if (krakenAttackSound != null)
            AudioSource.PlayClipAtPoint(krakenAttackSound, mainCameraTransform.position);
        evt.Target.TakeDamage(evt.Damage);
    }


    private void OnTurtleWallBlock(TurtleWallBlockEvent evt)
    {
        if (MapManager.Instance.TryGetTile(evt.TilePos, out HexTile tile))
        {
            tile.SetBlockedByTurtleWall(true);
        }
    }

    private void OnTurtleWallUnblock(TurtleWallUnblockEvent evt)
    {
        if (MapManager.Instance.TryGetTile(evt.TilePos, out HexTile tile))
        {
            tile.SetBlockedByTurtleWall(false);
        }
    }

#region Render
    private bool IsUnderTheFog(int id)
    {
        if (fogSystem == null)
            return true;

        if (!monsterPositions.TryGetValue(id, out Vector2Int pos))
            return true;

        return fogSystem.revealedTiles.Contains(pos);
    }

    public void UpdateSeaMonsterVisibility()
    {
        foreach (var monster in activeMonsters)
        {
            bool visible = IsUnderTheFog(monster.MonsterId);
            SetLayerRecursively(monster.gameObject, visible ? LayerMask.NameToLayer("Default") : LayerMask.NameToLayer("EnemyHidden"));
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
#endregion
}
