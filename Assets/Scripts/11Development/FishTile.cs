using System.Threading;
using UnityEngine;

public class FishTile : MonoBehaviour
{
    [Header("Development Settings")]
    [SerializeField] private int apCost = 2;              // AP cost to develop
    [SerializeField] private int populationGain = 1;      // Population reward
    [SerializeField] private bool removeAfterDevelop = true;

    private PlayerTracker player;
    private TechTree techTree;
    private TreeBase nearbyBase; // reference to nearby base (TreeBase) to check if its in turf
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
        if (!techTree.IsFishing)
        {
            Debug.Log("Fishing tech not researched yet");
            return false;
        }

        if (player.getAp() < apCost)
        {
            Debug.Log("Not enough AP");
            return false;
        }

        player.useAP(apCost);

        // Give population to nearby base if found
        if (nearbyBase != null)
        {
            nearbyBase.GainPop(populationGain);// here should be population gain
            Debug.Log($"Developed Fish Tile, +{populationGain} population to base.");
        }
        else
        {
            Debug.Log("Not within Turf!");
        }

        if (removeAfterDevelop)
        {
            RemoveFishTile();
            EventBus.Publish(new ActionMadeEvent());
        }
        return true;
    }
    private void RemoveFishTile()
    {
        Debug.Log("Removed Fish Tile");
        Destroy(gameObject);
    }

    private void CheckIfWithinTurf()
    {
        if (myHex == null)
        {
            Debug.LogWarning("FishTile has no HexTile parent!");
            return;
        }
        if (TurfManager.Instance.IsInsideTurf(myHex))
        {
            nearbyBase = FindNearestBase(myHex);
            if (nearbyBase != null)
            {
                Debug.Log("Found nearby TreeBase for FishTile.");
            }
            else
            {
                Debug.Log("No TreeBase in turf nearby.");
            }
        }
        else
        {
            nearbyBase = null;
            Debug.Log("FishTile is NOT inside turf!");
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
