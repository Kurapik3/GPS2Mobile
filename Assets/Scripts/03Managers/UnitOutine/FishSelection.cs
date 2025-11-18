using UnityEngine;
using System.Collections.Generic;

public class FishSelection : MonoBehaviour
{
    public static FishSelection instance;

    [Header("Selection")]
    public List<GameObject> fishSelected = new List<GameObject>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddFishToSelection(GameObject fishObject)
    {
        fishSelected.Clear();
        fishSelected.Add(fishObject);
    }

    public void DevelopSelectedTile()
    {
        if (fishSelected.Count == 0 || fishSelected[0] == null)
        {
            Debug.Log("No fish selected.");
            return;
        }

        FishTile tile = fishSelected[0].GetComponent<FishTile>();
        if (tile != null)
        {
            bool success = tile.OnTileTapped();
            if (success)
            {
                Debug.Log("Developed fish tile: " + fishSelected[0].name);
                
                CloseStats();          
                CloseStructureInfoPanel(); 

                fishSelected.Clear();
            }
        }
        else
        {
            Debug.LogWarning("Selected object has no FishTile component: " + fishSelected[0].name);
        }
    }

    private void CloseStats()
    {
        // Implement your stats panel closing logic

    }

    private void CloseStructureInfoPanel()
    {
        // Implement your structure info panel closing logic
    }
}