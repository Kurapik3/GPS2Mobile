    using UnityEngine;

public class DebrisTile : MonoBehaviour
{
    [Header("Development Settings")]
    [SerializeField] private int apCost = 5;
    [SerializeField] private int populationGain = 2;
    [SerializeField] private bool removeAfterDevelop = true;

    private PlayerTracker player;
    private TechTree techTree;
    private TreeBase nearbyBase;
    private HexTile myHex;
    private void Start()
    {
        player = PlayerTracker.Instance;
        if (TechTree.Instance == null)
        {
            Debug.LogError("TechTree not found in scene! Make sure one exists.");
        }
        techTree = TechTree.Instance;
        myHex = GetComponentInParent<HexTile>();
        if (myHex != null)
        {
            myHex.debrisTile = this;
        }
        else
        {
            Debug.LogWarning("DebrisTile has no HexTile parent!");
        }
    }

    //call this when player wants to develop tile
    public bool OnTileTapped()
    {
        return TryDevelop();
    }

    private bool TryDevelop()
    {
        CheckIfWithinTurf();
        if (techTree == null)
        {
            Debug.LogError("TechTree is null! Cannot develop tile.");
            return false;
        }
        if (!techTree.IsMetalScraps)
        {
            Debug.Log("Metal Scraps tech not researched yet");
            return false;
        }

        if (player.getAp() < apCost)
        {
            Debug.Log("Not enough AP");
            return false;
        }

        player.useAP(apCost);

        if (nearbyBase != null)
        {
            nearbyBase.GainPop(populationGain); // here should be population gain
            Debug.Log($"Developed Debris Tile, +{populationGain} population to base.");
        }
        else
        {
            Debug.Log("Not within Turf!");
        }

        if (removeAfterDevelop)
        {
            RemoveDebrisTile();
        }
        return true;
    }
    private void RemoveDebrisTile()
    {
        Debug.Log("Removed Debris Tile");
        Destroy(gameObject);
    }
    private void CheckIfWithinTurf()
    {
        if (myHex == null)
        {
            Debug.LogWarning("DebrisTile has no HexTile parent!");
            return;
        }
        if (TurfManager.Instance.IsInsideTurf(myHex))
        {
            nearbyBase = FindNearestBase(myHex);
            if (nearbyBase != null)
            {
                Debug.Log("Found nearby TreeBase for DebrisTile.");
            }
            else
            {
                Debug.Log("No TreeBase in turf nearby.");
            }
        }
        else
        {
            nearbyBase = null;
            Debug.Log("DebrisTile is NOT inside turf!");
        }

    }
    private TreeBase FindNearestBase(HexTile tile)
    {
        if (tile == null)
            return null;
        TreeBase closest = null;
        float minDist = float.MaxValue;

        // Check all tiles in turf
        foreach (var t in TurfManager.Instance.GetAllTurfTiles())
        {
            if (t == null) continue;
            if (t.currentBuilding is TreeBase treeBase)
            {
                float dist = Vector3.Distance(tile.transform.position, t.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = treeBase;
                }
            }
        }
        return closest;
    }
}
