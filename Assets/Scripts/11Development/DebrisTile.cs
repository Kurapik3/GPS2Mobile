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

    private void Start()
    {
        player = PlayerTracker.Instance;
        techTree = FindFirstObjectByType<TechTree>();
        //checks if its in turf
        CheckIfWithinTurf();
    }

    //call this when player wants to develop tile
    public void OnTileTapped()
    {
        TryDevelop();
    }

    private void TryDevelop()
    {
        if (!techTree.IsMetalScraps)
        {
            Debug.Log("Metal Scraps tech not researched yet");
            return;
        }

        if (player.getAp() < apCost)
        {
            Debug.Log("Not enough AP");
            return;
        }

        player.useAP(apCost);

        if (nearbyBase != null)
        {
            //nearbyBase.UpgradeBase(); // here should be population gain
            Debug.Log($"Developed Debris Tile, +{populationGain} population to base.");
        }
        else
        {
            Debug.Log("Not within Turf!");
        }

        if (removeAfterDevelop)
        {
            Debug.Log("Removed Debris Tile");
            Destroy(gameObject);
        }
    }
    private void CheckIfWithinTurf()
    {
        //connect to turf system
    }
}
