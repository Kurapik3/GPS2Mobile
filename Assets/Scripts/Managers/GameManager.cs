using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private MapGenerator mapGenerator;

    void Start()
    {
        // Try to load saved map data
        MapData loadedData = MapSaveLoad.Load("MySavedMap");

        if (loadedData != null)
        {
            mapGenerator.SetMapData(loadedData);
            mapGenerator.GenerateFromData();
        }
        else
        {
            // No save found, so maybe generate a default map or show error
            Debug.Log("No saved map found. Generating default map.");
            mapGenerator.GenerateDefaultMap();
        }
    }

    public void SaveMap() //Can be called wif button press for debug
    {
        if (mapGenerator.MapData != null)
        {
            MapSaveLoad.Save(mapGenerator.MapData, "MySavedMap");
        }
        else
        {
            Debug.LogWarning("No MapData found to save!");
        }
    }
}
