using DG.Tweening.Core.Easing;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class UnitBase : MonoBehaviour
{
    [Header("Indicators")]
    [SerializeField] private GameObject rangeIndicatorPrefab;
    [SerializeField] private GameObject attackIndicatorPrefab;
    private List<GameObject> activeIndicators = new List<GameObject>();
    private List<GameObject> activeAttackIndicators = new List<GameObject>();
    private List<HexTile> tilesInAttackRange = new List<HexTile>();

    private List<HexTile> tilesInRange = new List<HexTile>();

    [Header("Unit Identity")]
    public int unitId = -1;

    [Header("Base Stats (Loaded from CSV)")]
    public string unitName;
    public int cost;
    public int range;
    public int movement;
    public int hp;
    public int attack;
    public bool isCombat;

    public bool isSelected = false;

    public HexTile currentTile;
    private Renderer rend;

    public bool hasMovedThisTurn = false;
    public bool HasAttackThisTurn  = false;

    [Header("Fog of War Settings")]
    [SerializeField] private int fogRevealRadius = 1;

    // ---- KENNETH'S ----
    private UnitHPDisplay hpDisplay;

    // --------------------

    protected virtual void Start()
    {
        rend = GetComponent<Renderer>();
        UpdateSelectionVisual();

        if (UnitManager.Instance != null)
        {
            UnitManager.Instance.RegisterUnit(this);
        }

        // ---- KENNETH'S ----
        hpDisplay = GetComponent<UnitHPDisplay>();
        // --------------------
    }
    public virtual void Initialize(UnitData data, HexTile startingTile)
    {
        unitName = data.unitName;
        hp = data.hp;
        attack = data.attack;
        movement = data.movement;
        range = data.range;
        isCombat = data.isCombat;

        currentTile = startingTile;
        if (startingTile != null)
        {
            Vector3 pos = startingTile.transform.position;
            pos.y = 2f;
            transform.position = pos;
        }

    }

    public virtual void Attack(HexTile target)
    {
        if (currentTile == null || target == null)
        {
            Debug.LogWarning("Either attacker or target is not on a tile!");
            return;
        }

        int distance = HexDistance(currentTile.q, currentTile.r, target.q, target.r);

        // Check if target is within attack range
        if (distance > range)
        {
            Debug.Log($"{unitName} tried to attack {target.currentEnemyUnit.unitType}, but target is out of range! (distance: {distance}, range: {range})");
            return;
        }

        if (target.currentEnemyUnit == null && target.currentEnemyBase == null && target.currentSeaMonster == null)
        {
            return;
        }
        
        if (target.currentEnemyUnit != null)
        {
            target.currentEnemyUnit.TakeDamage(attack);
            //Debug.Log($"{unitName} attacked {target.currentEnemyUnit.unitType} for {attack} damage!");
            HasAttackThisTurn = false;
        }
        else if(target.currentEnemyBase != null)
        {
            target.currentEnemyBase.TakeDamage(attack);
            //Debug.Log($"{unitName} attacked {target.currentEnemyBase} for {attack} damage!");
            HasAttackThisTurn = false;
        }
        else if(target.currentSeaMonster != null)
        {
            target.currentSeaMonster.TakeDamage(attack);
           //Debug.Log($"{unitName} attacked {target.currentSeaMonster.MonsterName} for {attack} damage!");
            HasAttackThisTurn = false;
        }

        HideAttackIndicators();
        EventBus.Publish(new ActionMadeEvent());
    }

    public virtual void TakeDamage(int amount)
    {

        hp -= amount;
        if (hp <= 0)
        {
            hp = 0;
            Die();
        }

        // ---- KENNETH'S ----
        if (hpDisplay != null)
        {
            hpDisplay.UpdateHPDisplay();
        }
        // --------------------

        Debug.Log($"{unitName} took {amount} damage. Remaining HP: {hp}");
        EventBus.Publish(new ActionMadeEvent());
    }

    protected virtual void Die()
    {
        Debug.Log($"{unitName} has died!");

        currentTile?.SetOccupiedByUnit(false); //Release current tile
        HideRangeIndicators();

        if (UnitManager.Instance != null)
        {
            UnitManager.Instance.UnregisterUnit(unitId);
        }
        Destroy(gameObject);
        EventBus.Publish(new ActionMadeEvent());
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateSelectionVisual();
        HideRangeIndicators();
        HideAttackIndicators();
        if (isSelected && !hasMovedThisTurn)
        {
            ShowRangeIndicators();
        }
        else
        {
            HideRangeIndicators();
        }
        
    }
    private void UpdateSelectionVisual()
    {
        if (rend != null)
            rend.material.color = isSelected ? Color.yellow : Color.white;
    }

    public List<HexTile> GetAvailableTiles()
    {
        List<HexTile> tiles = new List<HexTile>();
        foreach (var tile in MapManager.Instance.GetTiles())
        {
            if (HexDistance(currentTile.q, currentTile.r, tile.q, tile.r) <= movement && tile.IsWalkableForAI())
            {
                tiles.Add(tile);
            }
        }
        return tiles;
    }

    public virtual void TryMove(HexTile targetTile)
    {
        if (targetTile == null)
            return;

        int distance = HexDistance(currentTile.q, currentTile.r, targetTile.q, targetTile.r);

        if (distance > movement)
        {
            Debug.Log($"{unitName} can't move that far! (Range: {movement}, Target: {distance})");
            return;
        }

        if (targetTile.IsOccupiedByUnit)
        {
            Debug.Log($"{unitName} can't move there — tile is blocked or occupied!");
            return;
        }
        if (!hasMovedThisTurn)
        {
            Move(targetTile);
        }
        else return;
    }


    public virtual void Move(HexTile targetTile)
    {
        if (targetTile == null) return;

        currentTile?.SetOccupiedByUnit(false); //Release old tile
        transform.position = targetTile.transform.position + Vector3.up * 2f; // optional y offset
        currentTile = targetTile;
        currentTile.SetOccupiedByUnit(true); //Occupied new tile
        Debug.Log($"{unitName} moved to ({currentTile.q}, {currentTile.r})");
        hasMovedThisTurn = true;
        RevealNearbyFog(currentTile);
        EventBus.Publish(new ActionMadeEvent());
    }
    public void ResetMove()
    {
        hasMovedThisTurn = false;
        HasAttackThisTurn = false;
    }

    protected void RevealNearbyFog(HexTile centerTile)
    {
        if (centerTile == null) return;

        List<HexTile> nearbyTiles = GetTilesInRange(centerTile.q, centerTile.r, fogRevealRadius);
        foreach (HexTile tile in nearbyTiles)
        {
            FogSystem fog = FindAnyObjectByType<FogSystem>();
            if (tile.fogInstance != null)
            {
                if (fog != null)
                {
                    fog.RevealTilesAround(centerTile.HexCoords, fogRevealRadius);
                }
            }
        }

        Debug.Log($"{unitName} revealed fog around tile ({centerTile.q}, {centerTile.r})");
    }

    protected List<HexTile> GetTilesInRange(int q, int r, int radius)
    {
        List<HexTile> tilesInRange = new List<HexTile>();
        foreach (HexTile tile in MapManager.Instance.GetTiles())
        {
            int dist = HexDistance(q, r, tile.q, tile.r);
            if (dist <= radius)
                tilesInRange.Add(tile);
        }
        return tilesInRange;
    }

    protected int HexDistance(int q1, int r1, int q2, int r2)
    {
        int dq = q2 - q1;
        int dr = r2 - r1;
        int ds = (-q2 - r2) - (-q1 - r1);
        return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(ds)) / 2;
    }

    // ----------------------------------------[Kenneth's Work]---------------------------------------- //
    public void ShowRangeIndicators()
    {
        HideRangeIndicators();

        if (rangeIndicatorPrefab == null)
        {
            Debug.LogWarning($"{unitName}: Range Indicator Prefab is not assigned!");
            return;
        }

        if (currentTile == null)
        {
            Debug.LogWarning($"{unitName}: Cannot show range - no current tile!");
            return;
        }

        CalculateTilesInRange();

        Debug.Log($"{unitName}: Showing {tilesInRange.Count} range indicators");

        // Spawn indicators
        foreach (var tile in tilesInRange)
        {
            if (tile == null) continue;

            Vector3 spawnPos = new Vector3(tile.transform.position.x, 2.0f, tile.transform.position.z);

            GameObject indicator = Instantiate(rangeIndicatorPrefab, spawnPos, Quaternion.Euler(90f, 0f, 0f));
            indicator.name = $"RangeIndicator{tile.q}{tile.r}";
            activeIndicators.Add(indicator);
        }
    }

    public void HideRangeIndicators()
    {
        foreach (var indicator in activeIndicators)
        {
            if (indicator != null)
                Destroy(indicator);
        }
        activeIndicators.Clear();
        tilesInRange.Clear();
    }

    private void CalculateTilesInRange()
    {
        tilesInRange.Clear();
        if (MapManager.Instance == null)
        {
            Debug.LogError("MapManager.Instance is null!");
            return;
        }

        // Use flood-fill algorithm to find reachable tiles
        Dictionary<Vector2Int, int> reachableTiles = new Dictionary<Vector2Int, int>();
        Queue<(HexTile tile, int distance)> queue = new Queue<(HexTile, int)>();

        // Start from current tile
        queue.Enqueue((currentTile, 0));
        reachableTiles[new Vector2Int(currentTile.q, currentTile.r)] = 0;

        Dictionary<Vector2Int, HexTile> allTiles = (Dictionary<Vector2Int, HexTile>)MapManager.Instance.GetAllTiles();

        while (queue.Count > 0)
        {
            var (currentCheckTile, currentDistance) = queue.Dequeue();

            // Don't expand beyond movement range
            if (currentDistance >= movement)
                continue;

            // Check all 6 neighbors of hex tile
            Vector2Int[] neighbors = GetHexNeighbors(currentCheckTile.q, currentCheckTile.r);

            foreach (var neighborCoord in neighbors)
            {
                // Skip if already visited with shorter distance
                if (reachableTiles.ContainsKey(neighborCoord) && reachableTiles[neighborCoord] <= currentDistance + 1)
                    continue;

                // Check if tile exists in map
                if (!allTiles.TryGetValue(neighborCoord, out HexTile neighborTile))
                    continue;

                // Skip blocked or occupied tiles (they block path but we don't mark them as reachable)
                if (neighborTile.HasStructure || neighborTile.IsOccupiedByUnit)
                    continue;

                // Mark as reachable and add to queue for further exploration
                reachableTiles[neighborCoord] = currentDistance + 1;
                queue.Enqueue((neighborTile, currentDistance + 1));
            }
        }

        // Convert reachable tiles to list (excluding starting tile)
        foreach (var kvp in reachableTiles)
        {
            if (kvp.Value > 0) // Exclude the current tile (distance 0)
            {
                if (allTiles.TryGetValue(kvp.Key, out HexTile tile))
                {
                    tilesInRange.Add(tile);
                }
            }
        }
    }

    // Helper method to get the 6 neighboring hex coordinates
    private Vector2Int[] GetHexNeighbors(int q, int r)
    {
        return new Vector2Int[]
        {
        new Vector2Int(q + 1, r),     // East
        new Vector2Int(q - 1, r),     // West
        new Vector2Int(q, r + 1),     // Southeast
        new Vector2Int(q, r - 1),     // Northwest
        new Vector2Int(q + 1, r - 1), // Northeast
        new Vector2Int(q - 1, r + 1)  // Southwest
        };
    }

    private void OnDestroy()
    {
        HideRangeIndicators();
        HideAttackIndicators();
    }



    //Attack UI indictor 
    public void ShowAttackIndicators()
    {
        HideAttackIndicators();

        if (attackIndicatorPrefab == null)
        {
            Debug.LogWarning($"{unitName}: Attack Indicator Prefab not assigned!");
            return;
        }

        if (currentTile == null)
            return;

        CalculateTilesInAttackRange();

        foreach (var tile in tilesInAttackRange)
        {
            Vector3 pos = new Vector3(tile.transform.position.x, 2f, tile.transform.position.z);
            GameObject indicator = Instantiate(attackIndicatorPrefab, pos, Quaternion.Euler(90, 0, 0));
            indicator.name = $"AttackIndicator{tile.q}{tile.r}";
            activeAttackIndicators.Add(indicator);
        }
    }

    public void HideAttackIndicators()
    {
        foreach (var ind in activeAttackIndicators)
            if (ind != null)
                Destroy(ind);

        activeAttackIndicators.Clear();
        tilesInAttackRange.Clear();
    }

    private void CalculateTilesInAttackRange()
    {
        tilesInAttackRange.Clear();

        foreach (HexTile tile in MapManager.Instance.GetTiles())
        {
            int dist = HexDistance(currentTile.q, currentTile.r, tile.q, tile.r);

            if (dist <= range && dist > 0) // cannot attack itself
            {
                if (tile.currentEnemyUnit != null ||
                    tile.currentEnemyBase != null ||
                    tile.currentSeaMonster != null)
                {
                    tilesInAttackRange.Add(tile);
                }
            }
        }
    }

    public void SetPositionToTile(int q, int r)
    {
        if (MapManager.Instance.TryGetTile(new Vector2Int(q, r), out HexTile tile))
        {
            transform.position = tile.transform.position + Vector3.up * 2f;
            currentTile = tile;
        }
        else
        {
            Debug.LogWarning($"Tile ({q}, {r}) not found!");
        }
    }

}
