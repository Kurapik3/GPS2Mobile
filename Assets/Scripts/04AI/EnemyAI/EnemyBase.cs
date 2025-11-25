using UnityEngine;

/// <summary>
/// Represents an AI-controlled enemy base on the map.
/// Does not generate AP; used for enemy spawning and AI logic.
/// </summary>
public class EnemyBase : MonoBehaviour
{
    [Header("Enemy Base Settings")]
    public string baseName = "Enemy Base";
    public int health;
    public int maxUnits = 3;
    public int currentUnits = 0; //To track how many units are housed in each base
    public HexTile currentTile;
    [SerializeField] public int level = 1;
    [SerializeField] private int currentPop = 0;

    [Header("Different Models for Each Level")]
    public GameObject[] levelModels;

    [HideInInspector] public int baseId;
    public bool IsDestroyed => health <= 0;

    private int currentTurfRadius = 1;

    private void Start()
    {
        if (currentTile == null)
        {
            if (MapManager.Instance != null)
            {
                Vector2Int hexCoord = MapManager.Instance.WorldToHex(transform.position);
                HexTile tile = MapManager.Instance.GetTile(hexCoord);
                if (tile != null)
                    currentTile = tile;
                else
                    Debug.LogWarning($"[EnemyBase] No HexTile found at hex {hexCoord} for base '{baseName}' (world pos {transform.position}).");
            }
            else
            {
                Debug.LogError("[EnemyBase] MapManager.Instance is null!");
            }
        }

        if (currentTile != null)
        {
            if (currentTile.currentEnemyBase != null)
            {
                Debug.LogWarning($"[EnemyBase] Tile {currentTile.HexCoords} already has a base, skipping registration.");
                return;
            }
            currentTile.currentEnemyBase = this;
        }

        //Randomize HP between 20 and 35
        health = Random.Range(20, 36);

        //Register this base
        if (EnemyBaseManager.Instance != null)
            baseId = EnemyBaseManager.Instance.RegisterBase(this);
        else
            Debug.LogError("[EnemyBase] EnemyBaseManager not found in scene!");

        if (EnemyTurfManager.Instance != null && currentTile != null)
            EnemyTurfManager.Instance.RegisterBaseArea(currentTile.HexCoords, currentTurfRadius, this);


        UpdateModel();
        Debug.Log($"[EnemyBase] Spawned {baseName} with {health} HP.");
    }

    private void OnDestroy()
    {
        if (EnemyBaseManager.Instance != null)
            EnemyBaseManager.Instance.UnregisterBase(this);

        if (EnemyTurfManager.Instance != null && currentTile != null)
            EnemyTurfManager.Instance.UnregisterBaseArea(currentTile.HexCoords, currentTurfRadius);
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        Debug.Log($"[EnemyBase] {baseName} took {amount} damage (HP: {health})");

        if (health <= 0)
            DestroyBase();
    }

    private void DestroyBase()
    {
        Debug.Log($"[EnemyBase] {baseName} destroyed!");
        EnemyBaseManager.Instance?.OnBaseDestroyed(this);
        if (currentTile != null)
            SpawnGroveAt(currentTile);
        else
            Debug.LogWarning("[EnemyBase] Cannot spawn Groove — currentTile is null!");

        if (currentTile != null)
            currentTile.currentEnemyBase = null;

        Destroy(gameObject);
    }

    private void SpawnGroveAt(HexTile tile)
    {
        GameObject groveObj = Instantiate(BuildingFactory.Instance.GrovePrefab, tile.transform.position, Quaternion.identity);
        GroveBase newGroveBase = groveObj.GetComponent<GroveBase>();

        //Pass the current EnemyBase level to the Grove
        newGroveBase.SetFormerLevel(level, GroveBase.BaseOrigin.Enemy);

        newGroveBase.Initialize(BuildingFactory.Instance.GroveData, currentTile);
        currentTile.SetBuilding(newGroveBase);
        Debug.Log($"[EnemyBase] Spawned Groove at {tile.HexCoords}");
    }

    public void OnUnitSpawned()
    {
        currentUnits++;
    }

    public void OnUnitDestroyed()
    {
        currentUnits = Mathf.Max(0, currentUnits - 1);
    }

    public void OnTurnStart()
    {
        if (IsDestroyed) 
            return;

        Debug.Log($"[EnemyBase] Turn start for {baseName} (Tile: {currentTile?.HexCoords})");

        TryUpgradeBase();
    }

    public void AddPopulation(int population)
    {
        currentPop += population;
        Debug.Log($"[EnemyBase] {baseName} in ({currentTile.HexCoords}) gained {population} population. CurrentPop = {currentPop}");
        TryUpgradeBase();
    }

    private void TryUpgradeBase()
    {
        int popRequired = level == 1 ? 2 : level == 2 ? 3 : 4;

        if (currentPop >= popRequired)
        {
            currentPop -= popRequired;

            UpgradeBase();
        }
        else
        {
            Debug.Log($"[EnemyBase] Not enough population to upgrade {baseName}. Current: {currentPop}, Required: {popRequired}");
        }
    }

    private void UpgradeBase()
    {
        level++;
        health += 5;
        UpdateModel();
        Debug.Log($"[EnemyBase] Base upgraded! Base current level is {level}. Health +5, current HP = {health}");

        bool chooseScore = Random.value < 0.5f;

        if (chooseScore)
        {
            EnemyTracker.Instance.AddScore(400);
            Debug.Log("[EnemyBase] Base upgrade choice: +400 score");
        }
        else
        {
            if (currentTile != null && EnemyTurfManager.Instance != null)
            {
                currentTurfRadius++;

                //Remove old turf radius before expanding to new radius to keep territory data consistent
                EnemyTurfManager.Instance.UnregisterBaseArea(currentTile.HexCoords, currentTurfRadius - 1);
                EnemyTurfManager.Instance.RegisterBaseArea(currentTile.HexCoords, currentTurfRadius, this);
                Debug.Log($"[EnemyBase] Base upgrade choice: turf increased! New radius: {currentTurfRadius}");
            }
            else
            {
                Debug.LogWarning($"[EnemyBase] Cannot increase turf! Tile or manager missing.");
            }
        }
    }

    public void UpdateModel()
    {
        //Disable all models
        foreach (var model in levelModels)
            model.SetActive(false);

        //Enable current level model
        int idx = Mathf.Clamp(level - 1, 0, levelModels.Length - 1);
        levelModels[idx].SetActive(true);

        if (currentTile != null && currentTile.IsFogged)
            currentTile.SetContentsVisible(false);
    }
}
