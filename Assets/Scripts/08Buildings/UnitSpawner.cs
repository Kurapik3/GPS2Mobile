using UnityEngine;
using UnityEngine.UI;
public class UnitSpawner : MonoBehaviour
{
    public static UnitSpawner Instance;

    [Header("Initialize")]
    [SerializeField] private PlayerTracker player;
    [SerializeField] public UnitDatabase unitDatabase;
    [SerializeField] private TechTree techTree;

    [Header("Units Prefab")]
    [SerializeField] public GameObject BuilderPrefab;
    [SerializeField] public GameObject ScoutPrefab;
    [SerializeField] private GameObject TankerPrefab; 
    [SerializeField] private GameObject ShooterPrefab;
    [SerializeField] private GameObject BomberPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Transform fallbackSpawnPoint;

    [Header("UI Buttons")]
    [SerializeField] private Button builderButton;
    [SerializeField] private Button scoutButton;
    [SerializeField] private Button TankerButton;
    [SerializeField] private Button ShooterButton;
    [SerializeField] private Button BomberButton;

    // --------------------- Kenneth's --------------------------
    [Header("Status Buttons")]
    [SerializeField] private UnitButtonStatus builderStatus;
    [SerializeField] private UnitButtonStatus scoutStatus;
    [SerializeField] private UnitButtonStatus tankerStatus;
    [SerializeField] private UnitButtonStatus shooterStatus;
    [SerializeField] private UnitButtonStatus bomberStatus;
    // --------------------- Kenneth's --------------------------

    private TreeBase selectedTreeBase = null;
    private void Awake()
    {
        Instance = this;
    }
    public void Start()
    {
        if (builderButton != null)
            builderButton.onClick.AddListener(() => TrySpawnUnit(BuilderPrefab, 0, 2));

        if (scoutButton != null)
            scoutButton.onClick.AddListener(OnScoutButtonClicked);

        if (TankerButton != null)
            TankerButton.onClick.AddListener(OnTankButtonClicked);

        if (ShooterButton != null)
            ShooterButton.onClick.AddListener(OnShooterButtonClicked);

        if (BomberButton != null)
            BomberButton.onClick.AddListener(OnBomberButtonClicked);

        // --------------------- Kenneth's --------------------------
        if (builderStatus != null)
        {
            builderStatus.apCost = 2;
            builderStatus.techName = "builder";
        }

        if (scoutStatus != null)
        {
            scoutStatus.apCost = 3;
            scoutStatus.techName = "scouting";
        }

        if (player != null)
        {
            player.OnAPChanged += UpdateAllUnitButtons;
        }

        UpdateAllUnitButtons();
        // --------------------- Kenneth's --------------------------
    }

    // --------------------- Kenneth's --------------------------
    private void OnDestroy()
    {
        if (player != null)
        {
            player.OnAPChanged -= UpdateAllUnitButtons;
        }
    }

    private void UpdateAllUnitButtons()
    {
        if (builderStatus != null) builderStatus.UpdateStatus();
        if (scoutStatus != null) scoutStatus.UpdateStatus();
        if (tankerStatus != null) tankerStatus.UpdateStatus();
        if (shooterStatus != null) shooterStatus.UpdateStatus();
        if (bomberStatus != null) bomberStatus.UpdateStatus();
    }
    // --------------------- Kenneth's --------------------------
    public void SetSelectedTreeBase(TreeBase tb)
    {
        selectedTreeBase = tb;
        Debug.Log("[UnitSpawner] TreeBase selected as spawn point.");
    }

    private void OnScoutButtonClicked()
    {
        if (!techTree.IsScouting)
        {
            Debug.Log("You have not unlocked the Scout unit yet!");
            return;
        }

        TrySpawnUnit(ScoutPrefab, 1, 3);
    }

    private void OnTankButtonClicked()
    {
        if (!techTree.IsArmor)
        {
            Debug.Log("You have not unlocked the Scout unit yet!");
            return;
        }

        TrySpawnUnit(TankerPrefab, 2, 3);
    }

    private void OnShooterButtonClicked()
    {
        if (!techTree.IsShooter)
        {
            Debug.Log("You have not unlocked the Scout unit yet!");
            return;
        }

        TrySpawnUnit(ShooterPrefab, 3, 5);
    }
    private void OnBomberButtonClicked()
    {
        if (!techTree.IsNavalWarfare)
        {
            Debug.Log("You have not unlocked the Scout unit yet!");
            return;
        }

        TrySpawnUnit(BomberPrefab, 4, 12);
    }
    private void TrySpawnUnit(GameObject prefab, int csvIndex, int cost)
    {
        // Get the currently selected TreeBase from the selection manager
        TreeBase selected = SelectionOfStructureManager.instance.GetSelectedTreeBase();

        if (selected == null)
        {
            Debug.LogWarning("No TreeBase selected. Cannot spawn unit!");
            return;
        }

        if (player.currentAP < cost)
        {
            Debug.Log("Not enough AP!");
            return;
        }

        // Spawn the unit on the selected TreeBase
        UnitBase spawnedUnit = CreateUnit(prefab, csvIndex, selected);

        if (spawnedUnit != null)
        {
            player.useAP(cost);
            Debug.Log($"Unit {spawnedUnit.unitName} spawned successfully!");
        }
    }


    public UnitBase CreateUnit(GameObject unitPrefab, int csvIndex, TreeBase baseToSpawnAt)
    {
        if (unitPrefab == null)
        {
            Debug.LogError("CreateUnit failed: prefab is NULL");
            return null;
        }

        if (baseToSpawnAt == null)
        {
            Debug.LogError("CreateUnit failed: TreeBase is NULL");
            return null;
        }

        HexTile spawnTile = baseToSpawnAt.currentTile;

        if (spawnTile == null)
        {
            Debug.LogError($"TreeBase has NO TILE! Fix TreeBase.Initialize().");
            return null;
        }

        if (spawnTile.IsOccupiedByUnit)
        {
            foreach (var n in spawnTile.neighbours)
            {
                if (n != null && !n.IsOccupiedByUnit && n.IsWalkableForAI())
                {
                    spawnTile = n;
                    break;
                }
            }
        }

        if (spawnTile == null || spawnTile.IsOccupiedByUnit)
        {
            Debug.LogError("No place to spawn unit!");
            return null;
        }

        Vector3 pos = spawnTile.transform.position;
        pos.y += 2f;

        GameObject newUnit = Instantiate(unitPrefab, pos, Quaternion.identity);
        newUnit.layer = LayerMask.NameToLayer("Unit");

        UnitBase unit = newUnit.GetComponent<UnitBase>();

        if (unit == null)
        {
            Debug.LogError("Prefab has NO UnitBase component!");
            return null;
        }

        UnitData data = unitDatabase.GetAllUnits()[csvIndex];
        unit.Initialize(data, spawnTile);

        spawnTile.currentUnit = unit;
        spawnTile.SetOccupiedByUnit(true);

        Debug.Log($"Spawned {unit.unitName} at ({spawnTile.q}, {spawnTile.r})");
        return unit;
    }


    public void SpawnUnit(GameObject unitPrefab, int csvIndex, Vector2Int? spawnCoords = null)
    {
        if (unitPrefab == null)
        {
            Debug.LogError("Unit prefab is missing!");
            return;
        }

        if (csvIndex < 0 || csvIndex >= unitDatabase.GetAllUnits().Count)
        {
            Debug.LogError($"Invalid CSV index {csvIndex}");
            return;
        }

        // Determine spawn tile
        HexTile startingTile = null;

        // Use saved coordinates if provided
        if (spawnCoords.HasValue)
        {
            startingTile = MapManager.Instance.GetTile(spawnCoords.Value);
            if (startingTile == null)
            {
                Debug.LogWarning($"Saved tile {spawnCoords.Value} not found, will try neighbors or fallback point.");
            }
        }

        // If tile is null or occupied, try neighbors
        if (startingTile == null || startingTile.IsOccupiedByUnit)
        {
            bool found = false;

            if (startingTile != null)
            {
                foreach (var tile in startingTile.neighbours)
                {
                    if (tile != null && tile.IsWalkableForAI() && !tile.IsOccupiedByUnit)
                    {
                        startingTile = tile;
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                if (fallbackSpawnPoint == null)
                {
                    Debug.LogError("No available tile and no fallback spawn point assigned!");
                    return;
                }
                startingTile = null; // will spawn at fallback point
            }
        }

        // Calculate spawn position
        Vector3 spawnPos = startingTile != null ? startingTile.transform.position : fallbackSpawnPoint.position;
        if (startingTile != null) spawnPos.y += 0.5f; // optional offset

        // Instantiate the unit
        GameObject newUnit = Instantiate(unitPrefab, spawnPos, Quaternion.identity);
        UnitBase unitBase = newUnit.GetComponent<UnitBase>();

        if (unitBase != null)
        {
            UnitData data = unitDatabase.GetAllUnits()[csvIndex];
            unitBase.Initialize(data, startingTile);

            if (startingTile != null)
                startingTile.SetOccupiedByUnit(true);

            // Register the unit in UnitManager
            UnitManager.Instance?.RegisterUnit(unitBase);

            Debug.Log($"Spawned {unitBase.unitName} at {(startingTile != null ? $"tile ({startingTile.q}, {startingTile.r})" : "fallback point")}");
        }
        else
        {
            Debug.LogWarning("Spawned unit has no UnitBase component.");
        }
    }



    public GameObject GetUnitPrefabByName(string name)
    {
        if (name == "Builder") return BuilderPrefab;
        if (name == "Scout") return ScoutPrefab;
        if (name == "Tanker") return TankerPrefab;
        if (name == "Shooter") return ShooterPrefab;
        if (name == "Bomber") return BomberPrefab;

        Debug.LogWarning($"Unit prefab not found for name: {name}");
        return null;
    }
}
