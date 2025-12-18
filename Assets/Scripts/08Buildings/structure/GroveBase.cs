using UnityEngine;

public class GroveBase : BuildingBase
{
    public enum BaseOrigin { Player, Enemy }
    public BaseOrigin Origin { get; private set; }

    private int formerTreeLevel = 1;
    private int formerEnemyBaseLevel = 1;

    public override void Initialize(BuildingData data, HexTile tile)
    {
        base.Initialize(data, tile);
        Debug.Log($"Initialized Grove at tile {tile.name}");
    }

    // Only Builder can develop
    public bool CanBeDevelopedBy(UnitBase unit)
    {
        return unit != null && unit.unitName == "Builder";
    }

    public void SetFormerLevel(int level, BaseOrigin origin)
    {
        Origin = origin;
        if (origin == BaseOrigin.Player)
            formerTreeLevel = level;
        else
            formerEnemyBaseLevel = level;
    }

    public int GetFormerLevel()
    {
        return Origin == BaseOrigin.Player ? formerTreeLevel : formerEnemyBaseLevel;
    }

    public void Develop(UnitBase developer)
    {
        Destroy(gameObject);
        if (!CanBeDevelopedBy(developer))
        {
            Debug.Log("Only a Builder can develop this Grove!");
            return;
        }

        if (PlayerTracker.Instance.currentAP < developCost)
        {
            Debug.Log("Not enough AP to develop Grove!");
            return;
        }

        PlayerTracker.Instance.useAP(developCost);

        HexTile tile = currentTile;

        // Remove Grove from the tile
        if (tile != null)
            tile.ClearBuilding();

        // Instantiate new TreeBase
        GameObject newTreeBaseObj = Instantiate(
            BuildingFactory.Instance.TreeBasePrefab,
            transform.position,
            Quaternion.identity
        );

        TreeBase newTreeBase = newTreeBaseObj.GetComponent<TreeBase>();

        // Initialize TreeBase on tile
        newTreeBase.Initialize(BuildingFactory.Instance.TreeBaseData, tile);

        // Assign TreeBase to tile
        tile.SetBuilding(newTreeBase);

        int formerLevel = GetFormerLevel();
        if (formerLevel <= 0) formerLevel = 1; // default to 1 if no previous level
        newTreeBase.SetLevelDirect(formerLevel);

        Debug.Log($"Grove developed back into Tree Base at level {formerLevel}!");

        Destroy(gameObject);
    }
    public BaseOrigin GetOrigin()
    {
        return Origin;
    }
}
