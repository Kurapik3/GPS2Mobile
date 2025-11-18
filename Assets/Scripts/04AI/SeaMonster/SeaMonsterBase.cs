using System.Collections;
using UnityEngine;
using static SeaMonsterEvents;

/// <summary>
/// Base class for all Sea Monsters (Kraken, TurtleWall)
/// Handles movement, turn logic, damage, and blocking.
/// </summary>
public abstract class SeaMonsterBase : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] protected string monsterName;
    [SerializeField] protected int attack;
    [SerializeField] protected int health;
    [SerializeField] protected int killPoints;
    [SerializeField] protected int killAP;
    [SerializeField] protected int movementRange;
    [SerializeField] protected int attackRange;

    public string MonsterName => monsterName;
    public int MovementRange => movementRange;

    [Header("Visual")]
    [SerializeField] public float heightOffset = 2f;

    public HexTile currentTile;
    public int CurrentTurn;

    //Unified ID for all sea monsters
    [HideInInspector] public int MonsterId;
    private static int nextMonsterId = 1;

    protected bool hasActedThisTurn = false;
    protected bool isBlocking = false;

    protected virtual void Awake()
    {
        MonsterId = nextMonsterId++;
    }

    protected virtual void OnEnable()
    {
        EventBus.Subscribe<SeaMonsterTurnStartedEvent>(OnTurnStarted);
    }

    protected virtual void OnDisable()
    {
        EventBus.Unsubscribe<SeaMonsterTurnStartedEvent>(OnTurnStarted);
    }

    //Controls sea monster spawning
    public virtual void Initialize(HexTile spawnTile)
    {
        currentTile = spawnTile;
        Vector3 world = MapManager.Instance.HexToWorld(spawnTile.HexCoords);
        world.y += heightOffset;
        transform.position = world;

        //Register tile occupancy
        MapManager.Instance.SetUnitOccupied(spawnTile.HexCoords, true);
        currentTile.currentSeaMonster = this;

        EventBus.Publish(new SeaMonsterSpawnedEvent(this, spawnTile.HexCoords));

        Debug.Log($"[{monsterName}] Spawned at {spawnTile.HexCoords}");
    }

    private void OnTurnStarted(SeaMonsterEvents.SeaMonsterTurnStartedEvent evt)
    {
        CurrentTurn = evt.Turn;
        hasActedThisTurn = false;
        PerformTurnAction();
    }

    public virtual void PerformTurnAction() { }

    protected virtual void MoveTo(HexTile newTile)
    {
        if (newTile == null || newTile == currentTile)
            return;

        Vector2Int oldPos = currentTile.HexCoords;
        Vector2Int newPos = newTile.HexCoords;

        //Validate walkability
        if (!MapManager.Instance.IsWalkable(newPos) || MapManager.Instance.IsTileOccupied(newPos) || newTile.currentBuilding != null || newTile.currentEnemyBase != null)
        {
            Debug.LogWarning($"[{monsterName}] Cannot move to {newPos} — blocked or not walkable.");
            return;
        }

        //Clear previous tile occupation
        MapManager.Instance.SetUnitOccupied(oldPos, false);

        if (currentTile != null)
            currentTile.currentSeaMonster = null;

        currentTile = newTile;
        currentTile.currentSeaMonster = this;

        //Register new tile as occupied
        MapManager.Instance.SetUnitOccupied(newPos, true);

        EventBus.Publish(new SeaMonsterMoveEvent(this, oldPos, newPos));

        //If blocking, trigger reapply event (used by TurtleWall)
        if (isBlocking)
            EventBus.Publish(new TurtleWallBlockEvent(this, newPos));

        Debug.Log($"[{monsterName}] Moved from {oldPos} to {newPos}");
        StartCoroutine(SmoothMove(newTile));
    }

    private IEnumerator SmoothMove(HexTile newTile)
    {
        Vector3 start = MapManager.Instance.HexToWorld(currentTile.HexCoords);
        start.y += heightOffset;
        Vector3 end = MapManager.Instance.HexToWorld(newTile.HexCoords);
        end.y += heightOffset;

        float t = 0f;
        float duration = 1f / AIController.AISpeedMultiplier; ;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            Vector3 nextPos = Vector3.Lerp(start, end, t);
            transform.position = nextPos;
            yield return null;
        }

        transform.position = end;
    }

    public virtual void TakeDamage(int dmg)
    {
        health -= dmg;
        if (health <= 0)
            Die();
    }

    protected virtual void Die()
    {
        Vector2Int pos = currentTile != null ? currentTile.HexCoords : Vector2Int.zero;

        //Free up the tile
        if (currentTile != null)
        {
            MapManager.Instance.SetUnitOccupied(pos, false);
            currentTile.currentSeaMonster = null;
        }

        EventBus.Publish(new SeaMonsterKilledEvent(this, pos));
        Debug.Log($"[{monsterName}] died at {pos}");

        Destroy(gameObject);
    }
}
