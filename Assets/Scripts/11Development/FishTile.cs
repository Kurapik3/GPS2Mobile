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

    private void Start()
    {
        player = PlayerTracker.Instance;
        techTree = FindFirstObjectByType<TechTree>();
        //checks if its in turf
        CheckIfWithinTurf();
    }

    //call this when player wants to develop tile
    private void OnTileTapped()
    {
        TryDevelop();
    }

    private void TryDevelop()
    {
        if (!techTree.IsFishing)
        {
            Debug.Log("Fishing tech not researched yet");
            return;
        }

        if (player.getAp() < apCost)
        {
            Debug.Log("Not enough AP");
            return;
        }

        player.useAP(apCost);

        // Give population to nearby base if found
        if (nearbyBase != null)
        {
            //nearbyBase.UpgradeBase(); // here should be population gain
            Debug.Log($"Developed Fish Tile, +{populationGain} population to base.");
        }
        else
        {
            Debug.Log("Not within Turf!");
        }

        if (removeAfterDevelop)
        {
            Debug.Log("Removed Fish Tile");
            Destroy(gameObject);
        }
    }

    private void CheckIfWithinTurf()
    {
        //connect to turf system
    }
}
