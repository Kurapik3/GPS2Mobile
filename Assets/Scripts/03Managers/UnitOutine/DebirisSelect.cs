using UnityEngine;
using System.Collections.Generic;

public class DebirisSelect : MonoBehaviour
{
    public static DebirisSelect instance;

    [Header("Selection")]
    public List<GameObject> debrisSelected = new List<GameObject>();

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

    public void AddDebrisToSelection(GameObject debrisObject)
    {
        // Clear previous selection and add new one
        debrisSelected.Clear();
        debrisSelected.Add(debrisObject);
    }

    public void DevelopSelectedTile()
    {
        if (debrisSelected.Count == 0 || debrisSelected[0] == null)
        {
            Debug.Log("No debris selected.");
            return;
        }

        DebrisTile tile = debrisSelected[0].GetComponent<DebrisTile>();
        if (tile != null)
        {
            bool success = tile.OnTileTapped();
            if (success)
            {
                Debug.Log("Developed debris tile: " + debrisSelected[0].name);
                CloseStats();
                CloseStructureInfoPanel();

                debrisSelected.Clear();
            }
        }
        else
        {
            Debug.LogWarning("Selected object has no DebrisTile component: " + debrisSelected[0].name);
        }
    }

    // Placeholder methods - implement based on your UI system
    private void CloseStats()
    {
        // Implement your stats panel closing logic
    }

    private void CloseStructureInfoPanel()
    {
        // Implement your structure info panel closing logic
    }
}