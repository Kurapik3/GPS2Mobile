using UnityEngine;
using UnityEngine.EventSystems;

public class TreeBase : BuildingBase
{
    [Header("Tree Base Properties")]
    [SerializeField] private int maxUnits = 3;
    [SerializeField] private int baseHealth = 20;
    [SerializeField] private int apBonusPerUpgrade = 1;
    [SerializeField] private int healthBonusPerUpgrade = 5;
    [SerializeField] public int level = 1;
    [SerializeField] public int currentPop = 0;
    //[SerializeField] public int maxUpgrades = 4;

    [Header("Population needed per upgrade")]
    [SerializeField] public int popForLvl2 = 2;
    [SerializeField] public int popForLvl3 = 3;
    [SerializeField] public int popForLvlMore = 4;

    [Header("Tree Base Prefabs")]
    public GameObject[] levelModels;

    private EnemyHPDisplay enemyBaseHPDisplay;
    private TreeBaseHPDisplay treeBaseHPDisplay;

    private int currentUnitsTrained = 0;
    public int TreeBaseId { get; private set; }
    public void SetTreeBaseId(int id) => TreeBaseId = id;

    [SerializeField] public int turfRadius = 2;

    private void Start()
    {
        TreeBaseId = GetInstanceID();
        Debug.Log($"[TreeBase] Initialized with ID: {TreeBaseId}");

        if (currentTile == null && MapManager.Instance != null)
        {
            Vector2Int hexCoord = MapManager.Instance.WorldToHex(transform.position);
            currentTile = MapManager.Instance.GetTile(hexCoord);
        }

        if (currentTile != null)
        {
            currentTile.SetBuilding(this);
            TurfManager.Instance.AddTurfArea(currentTile, turfRadius);
            Debug.Log($"{buildingName} placed at Hex ({currentTile.q},{currentTile.r}) with turf radius {turfRadius}");
        }
    }


    public override void Initialize(BuildingData data, HexTile tile)
    {
        base.Initialize(data, tile);

        currentTile = tile;         
        if (tile != null)
        {
            tile.SetBuilding(this);
        }
        buildingName = "Tree Base";
        apPerTurn = 2;
        health = baseHealth;
        TreeBaseId = GetInstanceID();
        UpdateModel();

        // Claim turf on initialization
        TurfManager.Instance.AddTurfArea(currentTile, turfRadius);
    }

    public override void OnTurnStart()
    {
        PlayerTracker.Instance.addAP(apPerTurn);
    }

    public bool CanTrainUnit()
    {
        return currentUnitsTrained < maxUnits;
    }

    public void TrainUnit(GameObject unitPrefab)
    {
        if (!CanTrainUnit())
        {
            Debug.Log("Training limit reached!");
            return;
        }

        currentUnitsTrained++;
        Instantiate(unitPrefab, transform.position, Quaternion.identity);
        Debug.Log($"Unit trained! ({currentUnitsTrained}/{maxUnits})");
    }

    public void GainPop(int amount)
    {
        currentPop += amount;
        Debug.Log($"Gained {amount} population. Total: {currentPop}");

        while (CanUpgrade())
        {
            ApplyUpgradeBase();
            ShowUpgradePopup();
        }

        TreeBaseHPDisplay hpDisplay = FindObjectOfType<TreeBaseHPDisplay>();
        hpDisplay.OnPopulationChanged();
        if (hpDisplay != null)
        {
            Debug.Log("[TreeBase] Found TreeBaseHPDisplay, calling OnPopulationChanged");
        }
        else
        {
            Debug.LogError("[TreeBase] TreeBaseHPDisplay NOT FOUND!");
        }
    }

    public bool CanUpgrade()
    {
        int requiredPop = level switch
        {
            1 => popForLvl2,
            2 => popForLvl3,
            _ => popForLvlMore
        };

        Debug.Log($"[CanUpgrade] Level={level}, currentPop={currentPop}, requiredPop={requiredPop}");

        if (currentPop < requiredPop)
        {
            Debug.Log($"Not enough population to upgrade! Need {requiredPop}, have {currentPop}");
            return false;
        }

        return true;
    }

    private void ApplyUpgradeBase()
    {
        int requiredPop = level switch
        {
            1 => popForLvl2,
            2 => popForLvl3,
            _ => popForLvlMore
        };

        currentPop -= requiredPop;
        level++;
        baseHealth += healthBonusPerUpgrade;
        health = baseHealth;

        UpdateModel();
        var hpDisplay = FindObjectOfType<TreeBaseHPDisplay>();
        hpDisplay?.OnLevelChanged();

        Debug.Log($"[TreeBase] Upgraded to Level {level}, currentPop = {currentPop}");
    }

    private void ShowUpgradePopup()
    {
        var upgradeUI = FindObjectOfType<TreeBaseUpgradeProgressUI>();
        if (upgradeUI != null)
        {
            upgradeUI.ShowPopup(level);
        }
        else
        {
            Debug.LogWarning("[TreeBase] TreeBaseUpgradeProgressUI not found in scene!");
        }
    }

    public void ChooseScore()
    {
        PlayerTracker.Instance.addScore(1000);
    }

    public void ChooseApPerTurn()
    {
        apPerTurn += apBonusPerUpgrade;
    }

    public void ChooseTurfUp()
    {
        turfRadius++;
        TurfManager.Instance.AddTurfArea(currentTile, turfRadius);
    }

    protected override void DestroyBuilding()
    {
        HexTile tile = currentTile;

        TurfManager.Instance.ClearTurf();

        Destroy(gameObject);
        Debug.Log("Tree Base destroyed! Becomes Grove.");

        if (BuildingFactory.Instance.GrovePrefab != null)
        {
            GameObject newGroveObj = Instantiate(
                BuildingFactory.Instance.GrovePrefab,
                tile.transform.position,
                Quaternion.identity
            );

            GroveBase groveScript = newGroveObj.GetComponent<GroveBase>();
            if (groveScript != null)
            {
                groveScript.SetFormerLevel(level, GroveBase.BaseOrigin.Player);
            }


            tile.SetBuilding(groveScript);
        }
    }


    // ---- KENNETH'S ----
    public void TakeDamage(int amount)
    {
        health -= amount;
        Debug.Log($"[TreeBase] Tree Base took {amount} damage (HP: {health})");

        if (enemyBaseHPDisplay != null || treeBaseHPDisplay !=null)
        {
            enemyBaseHPDisplay.UpdateHPDisplay();
            treeBaseHPDisplay.UpdateHPDisplay();
        }

        if (health <= 0)
            DestroyBuilding();
    }

    public int GetCurrentBuildingLevel()
    {
        return level;
    }

    public void UpdateModel()
    {
        if (levelModels == null || levelModels.Length == 0)
        {
            Debug.LogError("[TreeBase] No level models assigned!");
            return;
        }

        // Disable ALL models first
        foreach (var model in levelModels)
            model.SetActive(false);

        // Calculate which model to show (level 1 -> index 0)
        int idx = Mathf.Clamp(level - 1, 0, levelModels.Length - 1);

        // Activate correct model
        levelModels[idx].SetActive(true);

        Debug.Log($"[TreeBase] Updated model to Level {level}");
    }

    public void SetLevelDirect(int targetLevel)
    {
        //level = targetLevel > 0 ? Mathf.Clamp(targetLevel, 1, maxUpgrades) : 1;
        level = targetLevel > 0 ? targetLevel : 1;

        baseHealth = 20 + (healthBonusPerUpgrade * (level - 1));
        health = baseHealth;

        apPerTurn = 2 + (apBonusPerUpgrade * (level - 1));

        UpdateModel();
    }



}
