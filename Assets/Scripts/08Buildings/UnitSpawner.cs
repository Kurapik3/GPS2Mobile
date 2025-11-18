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
        if (player.currentAP >= cost)
        {
            CreateUnit(prefab, csvIndex, selectedTreeBase);
            player.useAP(cost); 
        }
        else
        {
            Debug.Log("Not enough AP!");
        }
    }

    public void CreateUnit(GameObject unitPrefab, int csvIndex, TreeBase baseToSpawnAt)
    {
        if (unitPrefab == null) return;
        if (baseToSpawnAt == null) return;

        HexTile spawnTile = baseToSpawnAt.currentTile;

        if (spawnTile == null || spawnTile.IsOccupiedByUnit)
        {
            Debug.Log("Spawn tile is occupied!");
            return;
        }

        Vector3 spawnPos = spawnTile.transform.position;
        spawnPos.y += 2f;

        GameObject newUnit = Instantiate(unitPrefab, spawnPos, Quaternion.identity);
        newUnit.layer = LayerMask.NameToLayer("Unit");

        UnitData data = unitDatabase.GetAllUnits()[csvIndex];
        UnitBase unitBase = newUnit.GetComponent<UnitBase>();

        if (unitBase != null)
        {
            unitBase.Initialize(data, spawnTile);
            spawnTile.SetOccupiedByUnit(true);
        }

        Debug.Log($"Spawned {unitPrefab.name} on base at tile ({spawnTile.q}, {spawnTile.r})");
    }

    //public void SpawnUnit(GameObject unitPrefab, int csvIndex)
    //{
    //    if (unitPrefab == null)
    //    {
    //        Debug.LogError("Unit prefab is missing!");
    //        return;
    //    }

    //    if (csvIndex < 0 || csvIndex >= unitDatabase.GetAllUnits().Count)
    //    {
    //        Debug.LogError($"Invalid CSV index {csvIndex}");
    //        return;
    //    }

    //    // Get starting tile
    //    HexTile startingTile = MapManager.Instance.GetTile(spawnCoord);

    //    // Check if the tile is free
    //    if (startingTile == null || startingTile.IsOccupiedByUnit)
    //    {
    //        bool found = false;
    //        foreach (var tile in startingTile.neighbours)
    //        {
    //            if (tile != null && tile.IsWalkableForAI())
    //            {
    //                startingTile = tile;
    //                found = true;
    //                break;
    //            }
    //        }

    //        if (!found)
    //        {
    //            Debug.LogWarning("No available tile found near spawn coordinates!");
    //            if (fallbackSpawnPoint == null)
    //            {
    //                Debug.LogError("No fallback spawn point assigned!");
    //                return;
    //            }

    //            // Spawn at fallback
    //            GameObject fallbackUnit = Instantiate(unitPrefab, fallbackSpawnPoint.position, Quaternion.identity);
    //            Debug.Log($"Spawned {unitPrefab.name} at fallback point {fallbackSpawnPoint.position}");
    //            return;
    //        }
    //    }

    //    Vector3 spawnPos = startingTile.transform.position;
    //    spawnPos.y += 2f; // optional small height offset
    //    GameObject newUnit = Instantiate(unitPrefab, spawnPos, Quaternion.identity);

    //    Debug.Log($"Spawned {unitPrefab.name} on tile ({startingTile.q}, {startingTile.r})");

    //        UnitData data = unitDatabase.GetAllUnits()[csvIndex];
    //    UnitBase unitBase = newUnit.GetComponent<UnitBase>();
    //    if (unitBase != null)
    //    {
    //        unitBase.Initialize(data, startingTile);
    //        startingTile.SetOccupiedByUnit(true);
    //        Debug.Log($"Initialized {unitBase.unitName} from CSV row {csvIndex + 2}");
    //    }   
    //    else
    //    {
    //        Debug.LogWarning("Spawned unit has no UnitBase component.");
    //    }
    //}


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
