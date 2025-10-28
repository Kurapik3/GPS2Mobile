using UnityEngine;
using System.IO;
public class GameManager : MonoBehaviour
{
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private DynamicTileGenerator dynamicTileGen;
    [SerializeField] private FogSystem fogSystem;

    private string savePath => Path.Combine(Application.persistentDataPath, "save.json");

    void Start()
    {
        // Try to load saved game (runtime data)
        if (File.Exists(savePath))
        {
            Debug.Log("Loading runtime save...");
            LoadGame();
        }
        else
        {
            StartNewGame();
        }
    }
    private void StartNewGame()
    {
        MapData loadedData = Resources.Load<MapData>("DefaultBaseMap");
        //MapData loadedData = MapSaveLoad.Load("MySavedMap");
        if (loadedData == null)
        {
            Debug.LogError("No base map found!");
            return;
        }

        mapGenerator.SetMapData(loadedData);
        mapGenerator.GenerateFromData();

        dynamicTileGen.GenerateDynamicElements();
        fogSystem.InitializeFog();

        Debug.Log("Started new game!");
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

    public void SaveGame()
    {
        GameSaveData data = new();

        // Save revealed fog tiles
        data.revealedTiles.Clear();
        foreach (var tile in fogSystem.revealedTiles)
        {
            data.revealedTiles.Add(new GameSaveData.FogTileData { q = tile.x, r = tile.y });
        }

        // Save dynamic tiles
        dynamicTileGen.SaveDynamicObjects(data);

        File.WriteAllText(savePath, JsonUtility.ToJson(data, true));
        Debug.Log($"Game saved to {savePath}");
    }

    public void LoadGame()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogWarning("No save file found!");
            return;
        }

        string json = File.ReadAllText(savePath);
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

        if (mapGenerator.MapData == null)
        {
            var loadedData = Resources.Load<MapData>("DefaultBaseMap");
            mapGenerator.SetMapData(loadedData);
            mapGenerator.GenerateFromData();
        }

        // Reveal fog tiles
        foreach (var tileData in data.revealedTiles)
        {
            fogSystem.RevealTilesAround(new Vector2Int(tileData.q, tileData.r), 0);
        }

        // Load dynamic objects
        dynamicTileGen.LoadDynamicObjects(data);
        Debug.Log("Game loaded!");
    }
}
