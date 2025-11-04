using UnityEngine;

public class Ruin : BuildingBase
{
    [Header("Ruin Properties")]
    [SerializeField] private bool isDeveloped = false;

    [SerializeField] private int apWhenRuin = 1;
    [SerializeField] private int apWhenDeveloped = 2;
    //[SerializeField] private GameObject developedPrefab;

    [SerializeField] private PlayerTracker player;
    public override void Initialize(BuildingData data, HexTile tile)
    {
        base.Initialize(data, tile);
        isDeveloped = false;
        apPerTurn = apWhenRuin;

        Debug.Log($"Initialized Ruin at ({tile.q}, {tile.r}) with {apPerTurn} AP/turn.");
    }

    //public override void OnTurnStart() // need Turf to work
    //{
    //    // Only provide AP if ruin is inside a turf
    //    if (currentTile != null && currentTile.IsWithinTurf)
    //    {
    //        playeraddAP(apPerTurn);
    //        Debug.Log($"{buildingName} (Ruin) generated {apPerTurn} AP this turn.");
    //    }
    //}

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

        // Check if unit has enough AP or resources
        if (player.currentAP < developCost)
        {
            Debug.Log($"{developer.unitName} does not have enough AP to develop {buildingName}!");
            return;
        }

        player.useAP(developCost);

        // Develop the ruin
        isDeveloped = true;
        apPerTurn = apWhenDeveloped;
        buildingName = "Developed Ruin";

        //  Replace model NOT SURE YET
        //if (developedPrefab != null)
        //{
        //    foreach (Transform child in transform)
        //        Destroy(child.gameObject);

        //    Instantiate(developedPrefab, transform.position, Quaternion.identity, transform);
        //}

        Debug.Log($"{developer.unitName} developed {buildingName}! Now produces {apPerTurn} AP/turn.");
    }

    protected override void DestroyBuilding()
    {
        Debug.Log($"{buildingName} has already been reduced to ruins — cannot be destroyed further.");
    }
}
