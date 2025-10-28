using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class UnitBase : MonoBehaviour
{
    [Header("Indicators")]
    [SerializeField] private GameObject rangeIndicatorPrefab;
    private List<GameObject> activeIndicators = new List<GameObject>();

    private List<HexTile> tilesInRange = new List<HexTile>();

    private static int nextUnitID = 0;
    public int UnitID { get; private set; }


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

    [Header("Fog of War Settings")]
    [SerializeField] private int fogRevealRadius = 1;

    private void Awake()
    {
        UnitID = nextUnitID++;
        Debug.Log($"Spawned Unit ID: {UnitID}");
    }
    protected virtual void Start()
    {
        rend = GetComponent<Renderer>();
        UpdateSelectionVisual();

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

    public virtual void Attack(UnitBase target)
    {
        if (currentTile == null || target.currentTile == null)
        {
            Debug.LogWarning("Either attacker or target is not on a tile!");
            return;
        }

        int distance = HexDistance(currentTile.q, currentTile.r, target.currentTile.q, target.currentTile.r);

        // Check if target is within attack range
        if (distance > range)
        {
            Debug.Log($"{unitName} tried to attack {target.unitName}, but target is out of range! (distance: {distance}, range: {range})");
            return;
        }

        target.TakeDamage(attack);
        Debug.Log($"{unitName} attacked {target.unitName} for {attack} damage!");
    }

    public virtual void TakeDamage(int amount)
    {

        hp -= amount;
        if (hp <= 0)
        {
            hp = 0;
            Die();
        }
        Debug.Log($"{unitName} took {amount} damage. Remaining HP: {hp}");
    }

    protected virtual void Die()
    {
        Debug.Log($"{unitName} has died!");

        HideRangeIndicators();
        Destroy(gameObject);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateSelectionVisual();
        HideRangeIndicators();
        if (isSelected)
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

        if (targetTile.HasStructure || targetTile.IsOccupiedByUnit)
        {
            Debug.Log($"{unitName} can't move there — tile is blocked or occupied!");
            return;
        }

        Move(targetTile);
    }


    public virtual void Move(HexTile targetTile)
    {
        if (targetTile == null) return;
        transform.position = targetTile.transform.position + Vector3.up * 2f; // optional y offset
        currentTile = targetTile;
        Debug.Log($"{unitName} moved to ({currentTile.q}, {currentTile.r})");

        RevealNearbyFog(currentTile);
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

        //FogSystem fog = FindAnyObjectByType<FogSystem>();
        //if (fog != null)
        //{
        //    fog.RevealTilesAround(centerTile.HexCoords, fogRevealRadius);
        //}

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

    // Kenneth's //
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

        // Calculate tiles in range
        CalculateTilesInRange();

        Debug.Log($"{unitName}: Showing {tilesInRange.Count} range indicators");

        // Spawn indicators
        foreach (var tile in tilesInRange)
        {
            if (tile == null) continue;

            //Vector3 spawnPos = tile.transform.position + Vector3.up;
            Vector3 spawnPos = new Vector3(tile.transform.position.x, 2.0f, tile.transform.position.z);

           // Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, tile.transform.up);

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

        Dictionary<Vector2Int, HexTile> allTiles = (Dictionary<Vector2Int, HexTile>)MapManager.Instance.GetAllTiles();

        foreach (var kvp in allTiles)
        {
            HexTile tile = kvp.Value;
            int distance = HexDistance(currentTile.q, currentTile.r, tile.q, tile.r);

            // Only include tiles within movement range
            if (distance > 0 && distance <= movement)
            {
                tilesInRange.Add(tile);
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up indicators when unit is destroyed
        HideRangeIndicators();
    }
}
