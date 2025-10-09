using UnityEngine;
using UnityEngine.InputSystem;
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
    [SerializeField] private Transform spawnPoint;

    public void Update()
    {

        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            if (player.currentAP >= 2)
            {

                TrySpawnUnit(BuilderPrefab,0,2);
            }
            else
            {
                Debug.Log("not enough AP");
            }

        }
        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            if (player.currentAP >= 3)
            {

                TrySpawnUnit(ScoutPrefab,1,3);
            }
            else
            {
                Debug.Log("not enough AP");
            }

        }

        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            player.addAP(1);
            Debug.Log("Added AP");
        }
    }
    private void TrySpawnUnit(GameObject prefab, int csvIndex, int cost)
    {
        if (player.currentAP >= cost)
        {
            CreateUnit(prefab, spawnPoint.position, csvIndex);
            player.useAP(cost);
        }
        else
        {
            Debug.Log("Not enough AP!");
        }
    }

    public void CreateUnit(GameObject unitPrefab, Vector3 position, int csvIndex)
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

        GameObject newUnit = Instantiate(unitPrefab, position, Quaternion.identity, spawnPoint);
        Debug.Log($"Spawned {unitPrefab.name} at {position}");

        // Load CSV row data
        UnitData data = unitDatabase.GetAllUnits()[csvIndex];

        // Initialize
        UnitBase unitBase = newUnit.GetComponent<UnitBase>();
        if (unitBase != null)
        {
            unitBase.Initialize(data);
            Debug.Log($"Initialized {unitBase.unitName} from CSV row {csvIndex + 2}");
        }
        else
        {
            Debug.LogWarning("Spawned unit has no UnitBase component.");
        }
    }
}
