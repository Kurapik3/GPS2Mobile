using System.Collections.Generic;
using UnityEngine;
using static SeaMonsterEvents;

public enum SeaMonsterState
{
    Untamed,
    Tamed
}

/// <summary>
/// Base class for all Sea Monsters (Kraken, TurtleWall)
/// Handles movement, turn logic, damage, and blocking.
/// </summary>
public abstract class SeaMonsterBase : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] public string monsterName;
    [SerializeField] public int attack;
    [SerializeField] public int health;
    [SerializeField] public int killPoints;
    [SerializeField] public int killAP;
    [SerializeField] public int movementRange;
    [SerializeField] public int attackRange;

    [Header("Visual")]
    [SerializeField] public float heightOffset = 2f;

    public HexTile currentTile;
    public int CurrentTurn;

    //Unified ID for all sea monsters
    public int MonsterId { get; private set; }
    private static int nextMonsterId = 1;
    public SeaMonsterState State { get; private set; } = SeaMonsterState.Untamed;
    private bool isSelected = false;

    protected bool hasActedThisTurn = false;
    protected bool isBlocking = false;

    [Header("TechTreeCreatureResearch")]
    [SerializeField] private GameObject untamedRangeIndicatorPrefab;
    [SerializeField] private GameObject tamedRangeIndicatorPrefab;
    private List<GameObject> activeIndicators = new List<GameObject>();

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
        if (State == SeaMonsterState.Untamed)
        {
            PerformTurnAction(); //AI action
        }
    }

    #region Untamed(AI)
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
    }
    #endregion

    #region Tamed
    public virtual void OnPlayerClickTile(HexTile tile) 
    {
        if (State != SeaMonsterState.Tamed)
            return;

        //If player click on the current tile that has sea monster
        if (tile == currentTile) 
        {
            SetSelected(!isSelected);
        }
        else if (tile.currentEnemyUnit != null || tile.currentEnemyBase != null || tile.currentSeaMonster != null)
        {
            PerformAttackOnTile(tile);
            SetSelected(false);
        }
        else if (isSelected && GetAvailableTiles().Contains(tile))
        {
            TryMove(tile);
            SetSelected(false);
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (isSelected)
            ShowSMRangeIndicators();
        else
            HideSMRangeIndicators();
    }

    public void TryMove(HexTile targetTile)
    {
        if (targetTile == null) 
            return;

        var availableTiles = GetAvailableTiles();
        if (!availableTiles.Contains(targetTile))
        {
            Debug.LogWarning($"[{monsterName}] Cannot move to {targetTile.HexCoords} — not in available tiles.");
            return;
        }

        MoveTo(targetTile);
    }

    #endregion

    public List<HexTile> GetAvailableTiles()
    {
        if (currentTile == null) return new List<HexTile>();

        List<HexTile> result = new List<HexTile>();
        var neighbors = MapManager.Instance.GetNeighborsWithinRadius(currentTile.HexCoords.x, currentTile.HexCoords.y, movementRange);

        foreach (var tile in neighbors)
        {
            if (tile == null)
                continue;

            if (MapManager.Instance.IsWalkable(tile.HexCoords) && !MapManager.Instance.IsTileOccupied(tile.HexCoords)
                && tile.currentBuilding == null && tile.currentEnemyBase == null && tile.currentSeaMonster == null)
            {
                result.Add(tile);
            }
        }

        return result;
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

    public void Tame()
    {
        if (State == SeaMonsterState.Untamed)
        {
            State = SeaMonsterState.Tamed;
            Debug.Log($"{name} has been tamed!");
        }
    }

    public void Untame()
    {
        State = SeaMonsterState.Untamed;
    }

    public abstract HexTile GetNextMoveTile();

    public void ShowSMRangeIndicators()
    {
        if (!TechTree.Instance.IsCreaturesResearch)
            return;

        HideSMRangeIndicators();

        if (untamedRangeIndicatorPrefab == null || currentTile == null || tamedRangeIndicatorPrefab == null)
            return;

        if(State == SeaMonsterState.Untamed)
        {
            HexTile nextMoveTile = GetNextMoveTile();

            if (nextMoveTile != null)
            {
                SpawnIndicatorAt(nextMoveTile, untamedRangeIndicatorPrefab);
            }
        }
        else if(State == SeaMonsterState.Tamed)
        {
            List<HexTile> tilesInRange = MapManager.Instance.GetNeighborsWithinRadius(currentTile.HexCoords.x, currentTile.HexCoords.y, movementRange);

            foreach (var tile in tilesInRange)
            {
               SpawnIndicatorAt(tile, tamedRangeIndicatorPrefab);
            }
        }            
    }

    private void SpawnIndicatorAt(HexTile tile, GameObject indicatorPrefab)
    {
        Vector3 spawnPos = new Vector3(tile.transform.position.x, 2.0f, tile.transform.position.z);
        GameObject indicator = Instantiate(indicatorPrefab, spawnPos, Quaternion.Euler(90f, 0f, 0f));
        indicator.name = $"SeaMonster_RangeIndicator({tile.q},{tile.r})";
        activeIndicators.Add(indicator);
    }

    public void HideSMRangeIndicators()
    {
        foreach (var ind in activeIndicators)
            if (ind != null) Destroy(ind);
        activeIndicators.Clear();
    }

    public void PerformAttackOnTile(HexTile tile)
    {
        if (tile.currentEnemyUnit != null || tile.currentEnemyBase != null || tile.currentSeaMonster != null)
        {
            int damage = attack;
            if (tile.currentEnemyUnit != null) 
                tile.currentEnemyUnit.TakeDamage(damage);
            else if (tile.currentEnemyBase != null) 
                tile.currentEnemyBase.TakeDamage(damage);
            else if (tile.currentSeaMonster != null) 
                tile.currentSeaMonster.TakeDamage(damage);
        }
    }
}
