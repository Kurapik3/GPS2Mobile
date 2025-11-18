using UnityEngine;

public class GroveBase : BuildingBase
{
    private int formerTreeLevel = 1; // Store level of previous TreeBase

    public override void Initialize(BuildingData data, HexTile tile)
    {
        base.Initialize(data, tile);
        Debug.Log($"Initialized Grove at tile {tile.name}");
    }

    public bool CanBeDevelopedBy(UnitBase unit)
    {
        return unit != null && unit.unitName == "Builder";
    }

    public void SetFormerLevel(int level)
    {
        formerTreeLevel = level;
    }

    public void Develop()
    {
        if (PlayerTracker.Instance.currentAP < developCost)
        {
            Debug.Log("Not enough AP to develop Grove!");
            return;
        }

        PlayerTracker.Instance.useAP(developCost);

        // Replace with Tree Base
        GameObject newTreeBaseObj = Instantiate(
            BuildingFactory.Instance.TreeBasePrefab,
            transform.position,
            Quaternion.identity
        );

        TreeBase newTreeBase = newTreeBaseObj.GetComponent<TreeBase>();
        newTreeBase.Initialize(BuildingFactory.Instance.TreeBaseData, currentTile);

        // Restore previous level
        for (int i = 1; i < formerTreeLevel; i++)
        {
            newTreeBase.ChooseScore(); // or choose appropriate upgrade method
        }

        currentTile.SetBuilding(newTreeBase);

        Debug.Log("Grove developed back into Tree Base!");
        Destroy(gameObject);
    }
}
