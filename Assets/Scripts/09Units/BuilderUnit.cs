using UnityEngine;

public class BuilderUnit : UnitBase
{
    [SerializeField] private BuildingBase treeBasePrefab; // assign in Inspector

    protected override void Start()
    {
        base.Start();
        Debug.Log($"{unitName} is a Builder unit ready to build! \nHP:{hp}, Attack:{attack}, Movement:{movement}");
    }

    public void UpgradeStructure()
    {
        // Check if builder is standing on a tile
        if (currentTile == null)
        {
            Debug.LogWarning($"{unitName}: Not standing on any tile!");
            return;
        }

        // Check if that tile has a building
        if (currentTile.currentBuilding == null)
        {
            Debug.Log($"{unitName}: No building on this tile to upgrade!");
            return;
        }

        // Check if it’s a Grove
        GroveBase grove = currentTile.currentBuilding as GroveBase;
        if (grove == null)
        {
            Debug.Log($"{unitName}: Can only upgrade Grove into TreeBase!");
            return;
        }

        Debug.Log($"{unitName} is upgrading {grove.buildingName}!");

        // Remove the Grove
        Destroy(grove.gameObject);

        // Build the TreeBase on the same tile
        BuildingBase newBuilding = Instantiate(treeBasePrefab, currentTile.transform.position, Quaternion.identity);
        newBuilding.Initialize(newBuilding.GetComponent<BuildingData>(), currentTile);

        Debug.Log($"{unitName} upgraded Grove to TreeBase!");
    }
    public void TreeToTree2()
    {

    }
}
