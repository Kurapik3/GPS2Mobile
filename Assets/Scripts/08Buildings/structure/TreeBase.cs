using UnityEngine;

public class TreeBase : BuildingBase
{
    [Header("Tree Base Properties")]
    [SerializeField] private int maxUnits = 3;
    [SerializeField] private int populationLevel = 1;
    [SerializeField] private int baseHealth = 20;
    [SerializeField] private int apBonusPerUpgrade = 1;
    [SerializeField] private int healthBonusPerUpgrade = 5;

    private int currentUnitsTrained = 0;
    public int TreeBaseId { get; private set; }


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

    public void UpgradeBase()
    {
        populationLevel++;

        // Apply bonuses per upgrade
        apPerTurn += apBonusPerUpgrade;
        baseHealth += healthBonusPerUpgrade;
        health = baseHealth; // restore health after upgrade

        PlayerTracker.Instance.addScore(1000);
        //PlayerTracker.Instance.IncreaseTurfRadius(1);

        Debug.Log($"Tree Base upgraded! Population {populationLevel}, AP {apPerTurn}, Max HP {baseHealth}");
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
