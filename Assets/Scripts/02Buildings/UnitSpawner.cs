using UnityEngine;
using UnityEngine.UI;
public class UnitSpawner : MonoBehaviour
{
    [Header("Initialize")]
    [SerializeField] private PlayerTracker player;
    [SerializeField] private UnitDatabase unitDatabase;

    [Header("Units Prefab")]
    [SerializeField] private GameObject BuilderPrefab;
    [SerializeField] private GameObject ScoutPrefab;
    //[SerializeField] private GameObject TankerPrefab; 
    //[SerializeField] private GameObject ShooterPrefab;
    //[SerializeField] private GameObject BomberPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Vector2Int spawnCoord = new Vector2Int(6, -1); 
    [SerializeField] private Transform fallbackSpawnPoint;

    [Header("UI Buttons")]
    [SerializeField] private Button builderButton;
    [SerializeField] private Button scoutButton;

    private void Start()
    {
        if (builderButton != null)
            builderButton.onClick.AddListener(() => TrySpawnUnit(BuilderPrefab, 0, 2));

        if (scoutButton != null)
            scoutButton.onClick.AddListener(() => TrySpawnUnit(ScoutPrefab, 1, 3));
    }
    
    private void TrySpawnUnit(GameObject prefab, int csvIndex, int cost)
    {
        if (player.currentAP >= cost)
        {
            CreateUnit(prefab, csvIndex);
            player.useAP(cost);
        }
        else
        {
            Debug.Log("Not enough AP!");
        }
    }

    public void CreateUnit(GameObject unitPrefab, int csvIndex)
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

        // Get starting tile
        HexTile startingTile = MapManager.Instance.GetTile(spawnCoord);

        // If the tile is occupied (structure or unit), find a free neighbor
        if (startingTile == null || startingTile.HasStructure || startingTile.IsOccupiedByUnit)
        {
            bool found = false;
            foreach (var tile in startingTile.neighbours)
            {
                if (tile.IsWalkableForAI())
                {
                    startingTile = tile;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                // If no free neighbors, fallback
                startingTile = null;
            }
        }

        Vector3 spawnPos = startingTile != null
            ? startingTile.transform.position + Vector3.up * 2f // spawn above tile
            : fallbackSpawnPoint.position;

        GameObject newUnit = Instantiate(unitPrefab, spawnPos, Quaternion.identity);
        Debug.Log($"Spawned {unitPrefab.name} at {spawnPos}");
        SetLayerRecursively(newUnit, LayerMask.NameToLayer("Unit"));

        UnitData data = unitDatabase.GetAllUnits()[csvIndex];
        UnitBase unitBase = newUnit.GetComponent<UnitBase>();
        if (unitBase != null)
        {
            unitBase.Initialize(data, startingTile);
            Debug.Log($"Initialized {unitBase.unitName} from CSV row {csvIndex + 2}");
        }
        else
        {
            Debug.LogWarning("Spawned unit has no UnitBase component.");
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
}
