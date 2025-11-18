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
    [SerializeField] private int currentPop = 0;
    [SerializeField] public int maxUpgrades = 3;

    [Header("Population needed per upgrade")]
    [SerializeField] private int popForLvl2 = 2;
    [SerializeField] private int popForLvl3 = 3;
    [SerializeField] private int popForLvlMore = 4;

    [Header("Tree Base Prefabs")]
    [SerializeField] private GameObject Level2Base;
    [SerializeField] private GameObject Level3Base;

    private EnemyHPDisplay hpDisplay;

    private int currentUnitsTrained = 0;
    public int TreeBaseId { get; private set; }

    [SerializeField] private int turfRadius = 2;

    private void Start()
    {
        TreeBaseId = GetInstanceID();

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
    void Update()
    {
        HandleMobileTap();
    }

    private void HandleMobileTap()
    {
        // Ignore if touching a UI button
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(0))
            return;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
        {
            ProcessRaycast(Input.GetTouch(0).position);
        }
    }

    private void ProcessRaycast(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.collider.gameObject == this.gameObject)
            {
                UnitSpawner.Instance.SetSelectedTreeBase(this);
                Debug.Log("[TreeBase] Selected TreeBase at tile: " + currentTile.q + "," + currentTile.r);
            }
        }
    }
    public override void Initialize(BuildingData data, HexTile tile)
    {
        base.Initialize(data, tile);

        buildingName = "Tree Base";
        apPerTurn = 2;
        health = baseHealth;
        TreeBaseId = GetInstanceID();

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
    }

    public bool CanUpgrade()
    {
        if (level >= maxUpgrades)
        {
            Debug.Log("Tree Base is fully upgraded!");
            return false;
        }

        int requiredPop = level switch
        {
            1 => popForLvl2,
            2 => popForLvl3,
            _ => popForLvlMore
        };

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

        // ---- KENNETH'S ----
        TreeBaseHPDisplay hpDisplay = FindObjectOfType<TreeBaseHPDisplay>();
        if (hpDisplay != null)
        {
            hpDisplay.ShowUpgradePopup();
        }
        // -------------------

        Debug.Log($"Tree Base upgraded to Level {level}");
        Debug.Log($"Tree Base upgraded to Level {level} (+{healthBonusPerUpgrade} HP)");
    }

    public void ChooseScore()
    {
        ApplyUpgradeBase();
        PlayerTracker.Instance.addScore(1000);
    }

    public void ChooseApPerTurn()
    {
        ApplyUpgradeBase();
        apPerTurn += apBonusPerUpgrade;
    }

    public void ChooseTurfUp()
    {
        ApplyUpgradeBase();
        turfRadius++;
        TurfManager.Instance.AddTurfArea(currentTile, turfRadius);
    }

    protected override void DestroyBuilding()
    {
        Debug.Log("Tree Base destroyed! Becomes Grove.");

        // Remove turf for this base
        TurfManager.Instance.ClearTurf(); // optionally, make a per-building RemoveTurfArea

        if (BuildingFactory.Instance.GrovePrefab != null)
        {
            GameObject grove = Instantiate(
                BuildingFactory.Instance.GrovePrefab,
                transform.position,
                Quaternion.identity
            );

            // Pass previous level to Grove so it can restore on rebuild
            GroveBase groveScript = grove.GetComponent<GroveBase>();
            if (groveScript != null)
                groveScript.SetFormerLevel(level, GroveBase.BaseOrigin.Player);
        }

        Destroy(gameObject);
    }

    // ---- KENNETH'S ----
    public void TakeDamage(int amount)
    {
        health -= amount;
        Debug.Log($"[TreeBase] Tree Base took {amount} damage (HP: {health})");

        if (hpDisplay != null)
        {
            hpDisplay.OnHealthChanged();
        }

        if (health <= 0)
            DestroyBuilding();
    }

    public int GetCurrentBuildingLevel()
    {
        return level;
    }
}
