using System.Collections.Generic;
using UnityEngine;

public class MovementRangeUI : MonoBehaviour
{
    public static MovementRangeUI Instance { get; private set; }

    [Header("Indicator Settings")]
    [SerializeField] private GameObject rangeIndicatorPrefab;
    [SerializeField] private float indicatorYOffset = 0.05f;

    [Header("Movement Cost Colors")]
    [SerializeField] private Color moveCost1Color = new Color(0.2f, 0.8f, 0.2f, 0.5f); // Green
    [SerializeField] private Color moveCost2Color = new Color(0.8f, 0.8f, 0.2f, 0.5f); // Yellow
    [SerializeField] private Color moveCostMaxColor = new Color(0.8f, 0.4f, 0.2f, 0.5f); // Orange
    [SerializeField] private Color unreachableColor = new Color(0.8f, 0.2f, 0.2f, 0.3f); // Red (optional)

    private Dictionary<HexTile, GameObject> activeIndicators = new Dictionary<HexTile, GameObject>();
    private UnitBase currentSelectedUnit;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Shows movement range for a selected unit
    /// </summary>
    public void ShowMovementRange(UnitBase unit)
    {
        if (unit == null || unit.currentTile == null)
        {
            Debug.LogWarning("Cannot show range: unit or currentTile is null");
            return;
        }

        ClearIndicators();
        currentSelectedUnit = unit;

        // Get all reachable tiles with their movement costs
        Dictionary<HexTile, int> reachableTiles = CalculateReachableTiles(unit);

        // Create indicators for each reachable tile
        foreach (var kvp in reachableTiles)
        {
            HexTile tile = kvp.Key;
            int moveCost = kvp.Value;

            // Don't show indicator on the tile where unit currently stands
            if (tile == unit.currentTile)
                continue;

            CreateIndicator(tile, moveCost, unit.movement);
        }
    }

    /// <summary>
    /// Calculates all tiles reachable by the unit using BFS/Dijkstra-like algorithm
    /// Returns dictionary of tiles with their movement cost to reach
    /// </summary>
    private Dictionary<HexTile, int> CalculateReachableTiles(UnitBase unit)
    {
        Dictionary<HexTile, int> reachable = new Dictionary<HexTile, int>();
        Queue<HexTile> frontier = new Queue<HexTile>();

        HexTile startTile = unit.currentTile;
        reachable[startTile] = 0;
        frontier.Enqueue(startTile);

        while (frontier.Count > 0)
        {
            HexTile current = frontier.Dequeue();
            int currentCost = reachable[current];

            // Check all neighbors
            foreach (HexTile neighbor in current.neighbours)
            {
                if (neighbor == null)
                    continue;

                // Calculate movement cost to this neighbor
                int moveCost = GetMovementCost(current, neighbor);
                int newCost = currentCost + moveCost;

                // Check if this tile is within movement range
                if (newCost > unit.movement)
                    continue;

                // Check if tile is walkable (not occupied by other units or blocked)
                if (!IsTileWalkable(neighbor, unit))
                    continue;

                // If we haven't visited this tile or found a cheaper path
                if (!reachable.ContainsKey(neighbor) || newCost < reachable[neighbor])
                {
                    reachable[neighbor] = newCost;
                    frontier.Enqueue(neighbor);
                }
            }
        }

        return reachable;
    }

    /// <summary>
    /// Gets the movement cost to enter a tile
    /// You can modify this to add terrain costs (e.g., forest = 2, mountain = 3)
    /// </summary>
    private int GetMovementCost(HexTile from, HexTile to)
    {
        // Base cost is 1
        int cost = 1;

        // Example: Add terrain-based costs here
        // if (to.tileType == HexTile.TileType.Forest) cost = 2;
        // if (to.HasStructure) cost += 1;

        return cost;
    }

    /// <summary>
    /// Checks if a tile can be walked on by this unit
    /// </summary>
    private bool IsTileWalkable(HexTile tile, UnitBase movingUnit)
    {
        // Basic walkability check
        if (!tile.IsWalkable)
            return false;

        // Check if occupied by another unit
        if (tile.currentUnit != null && tile.currentUnit != movingUnit)
            return false;

        // Check MapManager occupation status
        Vector2Int coord = new Vector2Int(tile.q, tile.r);
        if (MapManager.Instance.IsTileOccupied(coord))
            return false;

        return true;
    }

    /// <summary>
    /// Creates a visual indicator on a tile
    /// </summary>
    private void CreateIndicator(HexTile tile, int moveCost, int maxMovement)
    {
        if (rangeIndicatorPrefab == null)
        {
            Debug.LogWarning("Range indicator prefab not assigned!");
            return;
        }

        GameObject indicator = Instantiate(rangeIndicatorPrefab, tile.transform);
        indicator.transform.localPosition = new Vector3(0, indicatorYOffset, 0);
        indicator.name = $"MoveIndicator_{tile.q}_{tile.r}";

        // Set color based on movement cost
        Color indicatorColor = GetColorForMoveCost(moveCost, maxMovement);
        ApplyColorToIndicator(indicator, indicatorColor);

        activeIndicators[tile] = indicator;
    }

    /// <summary>
    /// Returns appropriate color based on movement cost
    /// </summary>
    private Color GetColorForMoveCost(int cost, int maxMovement)
    {
        float ratio = (float)cost / maxMovement;

        if (ratio <= 0.33f)
            return moveCost1Color;
        else if (ratio <= 0.66f)
            return moveCost2Color;
        else
            return moveCostMaxColor;
    }

    /// <summary>
    /// Applies color to the indicator (works with different renderer types)
    /// </summary>
    private void ApplyColorToIndicator(GameObject indicator, Color color)
    {
        // Try MeshRenderer first
        MeshRenderer meshRenderer = indicator.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            Material mat = meshRenderer.material;
            mat.color = color;
            return;
        }

        // Try SpriteRenderer
        SpriteRenderer spriteRenderer = indicator.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
            return;
        }

        // Check children
        meshRenderer = indicator.GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null)
        {
            Material mat = meshRenderer.material;
            mat.color = color;
        }
    }

    /// <summary>
    /// Clears all active movement indicators
    /// </summary>
    public void ClearIndicators()
    {
        foreach (var indicator in activeIndicators.Values)
        {
            if (indicator != null)
                Destroy(indicator);
        }
        activeIndicators.Clear();
        currentSelectedUnit = null;
    }

    /// <summary>
    /// Checks if a tile is within the shown movement range
    /// </summary>
    public bool IsTileInRange(HexTile tile)
    {
        return activeIndicators.ContainsKey(tile);
    }

    /// <summary>
    /// Gets the movement cost to reach a specific tile (if in range)
    /// </summary>
    public int GetMoveCostToTile(HexTile tile)
    {
        if (currentSelectedUnit == null || !activeIndicators.ContainsKey(tile))
            return -1;

        Dictionary<HexTile, int> reachable = CalculateReachableTiles(currentSelectedUnit);
        return reachable.ContainsKey(tile) ? reachable[tile] : -1;
    }

    private void OnDestroy()
    {
        ClearIndicators();
    }

}
