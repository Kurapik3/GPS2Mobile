using UnityEngine;

public class TreeBase : BuildingBase
{
    [Header("Tree Base Properties")]
    [SerializeField] private int maxUnits = 3;
    [SerializeField] private int baseHealth = 20;
    [SerializeField] private int apBonusPerUpgrade = 1;
    [SerializeField] private int healthBonusPerUpgrade = 5;
    [SerializeField] private int level = 1;
    [SerializeField] private int currentPop = 0;
    [SerializeField] private int maxUpgrades = 3;
    [SerializeField] private int ToLvlBase2 = 2;
    [SerializeField] private int ToLvlBase3 = 3;
    [SerializeField] private int ToLvlBaseMore = 4;

    [Header("Tree Base Prefabs")]
    [SerializeField] private GameObject Level2Base;
    [SerializeField] private GameObject Level3Base;

    private int currentUnitsTrained = 0;
    public int TreeBaseId { get; private set; }

    [SerializeField] private int turfRadius=2 ;

    private void Start()
    {
        // Only assign currentTile if it hasn't been set by Initialize or elsewhere
        if (currentTile == null && MapManager.Instance != null)
        {
            Vector2Int hexCoord = MapManager.Instance.WorldToHex(transform.position);
            currentTile = MapManager.Instance.GetTile(hexCoord);

            if (currentTile != null)
            {
                currentTile.SetBuilding(this);
                TurfManager.Instance.AddTurfArea(currentTile, turfRadius);
                 Debug.Log($"{buildingName} placed at Hex {hexCoord} (auto-detected)");
                Debug.Log($"<color=green>{buildingName} initialized at tile {currentTile.name} with turf radius {turfRadius}</color>");
            }
            else
            {
                Debug.LogWarning($"{buildingName} could NOT find a hex tile at position {transform.position}");
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
        // if 
       if(level==1 && currentPop >= 2 )
        {

        }
        return true;
    }

    private void ApplyUpgradeBase()
    {
        level++;
        baseHealth += healthBonusPerUpgrade;
        health = baseHealth;

        Debug.Log($"Tree Base upgraded to Level {level}");
    }

    //baseUpgrade for KENNEHTH TO USE 
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

        turfRadius += 1;
        
    }

    protected override void DestroyBuilding()
    {
        Debug.Log("Tree Base destroyed! Becomes Grove.");

        if (BuildingFactory.Instance.GrovePrefab != null)
        {
            Instantiate(BuildingFactory.Instance.GrovePrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}
