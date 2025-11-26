using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EnemyAIEvents;
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

    [Header("Feedback Settings")]
    [SerializeField] private float preSpawnDelay = 1f;
    [SerializeField] private float shakeIntensity = 0.6f;
    [SerializeField] private float shakeDuration = 0.5f;

    [SerializeField] private List<SeaMonsterBase> activeMonsters = new();
    public IReadOnlyList<SeaMonsterBase> ActiveMonsters => activeMonsters;

    private readonly Dictionary<int, Vector2Int> monsterPositions = new();
    public IReadOnlyDictionary<int, Vector2Int> MonsterPositions => monsterPositions;

    private bool isProcessingAITurn = false;

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
        EventBus.Subscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Subscribe<TamingUnlockedEvent>(OnTamingUnlocked);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<SeaMonsterTurnStartedEvent>(OnTurnStarted);
        EventBus.Unsubscribe<SeaMonsterKilledEvent>(OnSeaMonsterKilled);
        EventBus.Unsubscribe<SeaMonsterMoveEvent>(OnMonsterMoved);
        EventBus.Unsubscribe<KrakenAttacksUnitEvent>(OnKrakenAttackUnit);
        EventBus.Unsubscribe<KrakenAttacksMonsterEvent>(OnKrakenAttackMonster);
        EventBus.Unsubscribe<TurtleWallBlockEvent>(OnTurtleWallBlock);
        EventBus.Unsubscribe<TurtleWallUnblockEvent>(OnTurtleWallUnblock);
        EventBus.Unsubscribe<UnitSelectedEvent>(OnUnitSelected);
        EventBus.Unsubscribe<TamingUnlockedEvent>(OnTamingUnlocked);
    }

    private void Start()
    {
        EventBus.Publish(new SeaMonsterSystemReadyEvent(this));
    }

    private void OnTurnStarted(SeaMonsterTurnStartedEvent evt)
    {
        if (isProcessingAITurn)
        {
            Debug.LogWarning("[SeaMonsterManager] Already processing AI turn!");
            return;
        }

        StartCoroutine(ProcessSeaMonsterTurn(evt.Turn));
    }

    private IEnumerator ProcessSeaMonsterTurn(int turn)
    {
        isProcessingAITurn = true;
        Debug.Log($"[SeaMonsterManager] Processing Sea Monster Turn {turn}");

        //Start at turn 10, then every 4 turns
        if (turn == 10 || (turn > 10 && (turn - 10) % 4 == 0))
        {
            yield return StartCoroutine(SpawnSequence(turn));
        }

        if (turn < 10)
        {
            Debug.Log("[SeaMonsterManager] Turn < 10: No Sea Monster phase.");
            EndSeaMonsterTurn(turn);
            yield break;
        }

        //Execute AI for all untamed monsters
        bool hasUntamed = false;
        List<SeaMonsterBase> untamedMonsters = new List<SeaMonsterBase>();

        foreach (var monster in activeMonsters)
        {
            if (monster != null && monster.State == SeaMonsterState.Untamed)
            {
                untamedMonsters.Add(monster);
                hasUntamed = true;
            }
        }

        if (hasUntamed)
        {
            Debug.Log($"[SeaMonsterManager] Found {untamedMonsters.Count} untamed monsters. Running AI.");

            foreach (var monster in untamedMonsters)
            {
                if (monster != null && monster.State == SeaMonsterState.Untamed)
                {
                    monster.PerformTurnAction();
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }
        else
        {
            Debug.Log("[SeaMonsterManager] No untamed monsters. Skipping AI phase.");
        }

        //End turn
        EndSeaMonsterTurn(turn);
    }

    private void EndSeaMonsterTurn(int turn)
    {
        Debug.Log($"[SeaMonsterManager] Ending Sea Monster Turn {turn}");
        isProcessingAITurn = false;
        EventBus.Publish(new SeaMonsterTurnEndEvent(turn));
    }

    private IEnumerator SpawnSequence(int turn)
    {
        //SeaMonster warning (turn 10 only)
        if (turn == 10)
        {
            EventBus.Publish(new KrakenPreSpawnWarningEvent(turn));
            ManagerAudio.instance.PlaySFX("SeaMonsterSpawn");

            yield return StartCoroutine(ShakeCamera());
            yield return new WaitForSeconds(preSpawnDelay);
        }

        //Spawn random monster
        SeaMonsterBase monster = spawner.SpawnRandomMonster();
        if (monster != null)
        {
            RegisterMonster(monster);

            Vector2Int tilePos = monster.currentTile != null ? monster.currentTile.HexCoords : Vector2Int.zero;
            EventBus.Publish(new SeaMonsterSpawnedEvent(monster, tilePos));

            if (monster is Kraken)
            {
                ManagerAudio.instance.PlaySFX("KrakenSpawn");
            }
        }
    }

    public void RegisterMonster(SeaMonsterBase monster)
    {
        if (monster == null) 
            return;
        if (!activeMonsters.Contains(monster))
            activeMonsters.Add(monster);

        Vector2Int pos = monster.currentTile != null ? monster.currentTile.HexCoords : Vector2Int.zero;
        monsterPositions[monster.MonsterId] = pos;

        if (TechTree.Instance.IsTaming)
        {
            monster.Tame();
        }
        else
        {
            monster.Untame();
        }

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
            ManagerAudio.instance.PlaySFX("KrakenDie");
        }
        else if (evt.Monster is TurtleWall)
        {
            ManagerAudio.instance.PlaySFX("TurtleDie");
        }

        if (activeMonsters.Contains(evt.Monster))
            activeMonsters.Remove(evt.Monster);

        monsterPositions.Remove(evt.Monster.MonsterId);
    }

    #region Move
    private void OnMonsterMoved(SeaMonsterMoveEvent evt)
    {
        if (evt.Monster == null)
            return;

        if (evt.Monster is Kraken)
        {
            ManagerAudio.instance.PlaySFX("KrakenMove");
        }
        else if (evt.Monster is TurtleWall)
        {
            ManagerAudio.instance.PlaySFX("TurtleMove");
        }

        StartCoroutine(SmoothMove(evt.Monster, evt.From, evt.To));
        monsterPositions[evt.Monster.MonsterId] = evt.To;
        UpdateSeaMonsterVisibility();
    }

    private IEnumerator SmoothMove(SeaMonsterBase sm, Vector2Int startHex,Vector2Int endHex)
    {
        if (sm == null)
            yield break;

        Vector3 startPos = MapManager.Instance.HexToWorld(startHex);
        startPos.y += sm.heightOffset;

        Vector3 endPos = MapManager.Instance.HexToWorld(endHex);
        endPos.y += sm.heightOffset;

        float angle = GetRotationAngleTo(startPos, endPos);
        //Face movement direction on Y axis
        sm.transform.rotation = Quaternion.Euler(0f, angle + 180f, 0f);

        float t = 0f;
        float duration = 0.5f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            sm.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        sm.transform.position = endPos;
    }

    private float GetRotationAngleTo(Vector3 worldStart, Vector3 worldEnd)
    {
        Vector3 dir = worldEnd - worldStart;
        dir.y = 0; //Ignore vertical move

        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        return angle;
    }
    #endregion

    private void OnKrakenAttackUnit(KrakenAttacksUnitEvent evt)
    {
        if (evt.Target == null)
            return;

        if (evt.Attacker != null)
        {
            var render = evt.Attacker.GetComponentInChildren<Renderer>();
            if (render != null)
            {
                Color aOriginal = render.material.color;
                render.material.color = Color.yellow;
                StartCoroutine(RestoreColor(render, aOriginal, 0.2f));
            }
        }

        //If target is player unit
        if (evt.Target.TryGetComponent<UnitBase>(out UnitBase playerUnit))
        {
            ManagerAudio.instance.PlaySFX("KrakenAttack");
            playerUnit.TakeDamage(evt.Damage);
            if(playerUnit.unitName == "Tanker")
            {
                evt.Attacker.TakeDamage(evt.Damage);
            }
            Debug.Log($"Kraken attacked player {playerUnit.unitName} unit for {evt.Damage} damage!");
            return;
        }

        if (evt.Target.TryGetComponent<EnemyUnit>(out EnemyUnit enemyUnit))
        {
            ManagerAudio.instance.PlaySFX("KrakenAttack");
            enemyUnit.TakeDamage(evt.Damage);
            Debug.Log($"Kraken attacked enemy {enemyUnit.unitType} unit for {evt.Damage} damage!");
        }

        //Temp, for testing
        Renderer r = evt.Target.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            Color original = r.material.color;
            r.material.color = Color.red;
            StartCoroutine(RestoreColor(r, original, 0.2f));
        }
    }

    //Temp, for testing
    private IEnumerator RestoreColor(Renderer r, Color original, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (r != null)
            r.material.color = original;
    }

    private void OnKrakenAttackMonster(KrakenAttacksMonsterEvent evt)
    {
        if (evt.Target == null)
            return;

        ManagerAudio.instance.PlaySFX("KrakenAttack");
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

    #region RangeIndicator
    private void OnUnitSelected(UnitSelectedEvent evt)
    {
        if (evt.IsSelected)
        {
            ShowAllSeaMonsterRanges();
        }
        else
        {
            HideAllSeaMonsterRanges();
        }
    }

    private void ShowAllSeaMonsterRanges()
    {
        foreach (var monster in activeMonsters)
        {
            if (monster != null)
            {
                monster.ShowSMRangeIndicators();
            }
        }
    }

    private void HideAllSeaMonsterRanges()
    {
        foreach (var monster in activeMonsters)
        {
            if (monster != null)
            {
                monster.HideSMRangeIndicators();
            }
        }
    }
    #endregion
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

    private void OnTamingUnlocked(TamingUnlockedEvent evt)
    {
        foreach (var sm in activeMonsters)
        {
            if (sm != null)
                sm.Tame();
        }
    }
}
