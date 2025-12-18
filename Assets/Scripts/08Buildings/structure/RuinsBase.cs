using UnityEngine;

public class Ruin : BuildingBase
{
    [Header("Ruin Properties")]
    [SerializeField] private bool isDeveloped = false;

    [SerializeField] private int apWhenRuin = 1;
    [SerializeField] private int apWhenDeveloped = 2;
    //[SerializeField] private GameObject developedPrefab;

    private PlayerTracker player;
    public override void Initialize(BuildingData data, HexTile tile)
    {
        base.Initialize(data, tile);
        player = GetComponentInParent<PlayerTracker>();
        isDeveloped = false;
        apPerTurn = apWhenRuin;

        Debug.Log($"Initialized Ruin at ({tile.q}, {tile.r}) with {apPerTurn} AP/turn.");
    }

    public void Develop(UnitBase developer)
    {
        if (isDeveloped)
        {
            Debug.Log($"{buildingName} is already developed!");
            return;
        }

        if (developer == null)
        {
            Debug.LogWarning("No unit provided to develop the ruin!");
            return;
        }
        if (developer.currentTile == null || !developer.currentTile.isPlayerTurf)
        {
            Debug.LogWarning($"{developer.unitName} must be on turf to develop {buildingName}!");
            return;
        }
        // Check if unit has enough AP or resources
        if (PlayerTracker.Instance.currentAP < developCost)
        {
            Debug.Log($"{developer.unitName} does not have enough AP to develop {buildingName}!");
            return;
        }

        PlayerTracker.Instance.useAP(developCost);

        // Develop the ruin
        isDeveloped = true;
        apPerTurn = apWhenDeveloped;
        buildingName = "Developed Ruin";


        Debug.Log($"{developer.unitName} developed {buildingName}! Now produces {apPerTurn} AP/turn.");
    }

    protected override void DestroyBuilding()
    {
        Debug.Log($"{buildingName} has already been reduced to ruins — cannot be destroyed further.");
    }
}
