using UnityEngine;

public class CacheBase : BuildingBase
{
    [Header("Cache Properties")]
    [SerializeField] private bool isDeveloped = false;
    [SerializeField] private int apRewardOnDevelop = 10;
    private PlayerTracker player;

    public override void Initialize(BuildingData data, HexTile tile)
    {
        base.Initialize(data, tile);
        player = GetComponentInParent<PlayerTracker>();
        isDeveloped = false;
        Debug.Log($"Initialized Cache at ({tile.q}, {tile.r}) — waiting to be developed.");
    }

    public void Develop(UnitBase developer)
    {
        if (isDeveloped)
        {
            Debug.Log($"{buildingName} has already been developed.");
            return;
        }

        if (developer == null)
        {
            Debug.LogWarning("No developer unit provided!");
            return;
        }

        isDeveloped = true;

        // Reward player with AP
        player.addAP(apRewardOnDevelop);
        Debug.Log($"{developer.unitName} developed {buildingName} and gained {apRewardOnDevelop} AP!");

        // Remove cache from tile and destroy it
        if (currentTile != null)
        {
            currentTile.SetBuilding(null);
        }

        Destroy(gameObject);
    }

    public override void OnTurnStart()
    {
        // Cache doesnt generate AP per turn
    }

    protected override void DestroyBuilding() // can cache be destory?
    {
        // If destroyed before being developed, just remove it
        Debug.Log($"{buildingName} (Cache) destroyed before being developed.");
        if (currentTile != null)
            currentTile.SetBuilding(null);

        Destroy(gameObject);
    }
}
