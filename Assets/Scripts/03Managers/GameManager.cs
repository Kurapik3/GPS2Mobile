using UnityEngine;
using System.IO;
using System;
public class GameManager : MonoBehaviour
{
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private DynamicTileGenerator dynamicTileGen;
    [SerializeField] private FogSystem fogSystem;
    [SerializeField] private TurnManager turnManager;

    private PlayerTracker player;
    private EnemyTracker enemy;
    private string savePath => Path.Combine(Application.persistentDataPath, "save.json");
    public static GameManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        player = PlayerTracker.Instance;
        enemy = EnemyTracker.Instance;
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
    private void OnEnable()
    {
        EventBus.Subscribe<SaveGameEvent>(OnSaveGame);
        EventBus.Subscribe<LoadGameEvent>(OnLoadGame);
        EventBus.Subscribe<ActionMadeEvent>(OnAutoSave);
        EventBus.Subscribe<AllEnemyBasesDestroyed>(OnAllEnemyBaseDestroyed);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<SaveGameEvent>(OnSaveGame);
        EventBus.Unsubscribe<LoadGameEvent>(OnLoadGame);
        EventBus.Unsubscribe<ActionMadeEvent>(OnAutoSave);
        EventBus.Subscribe<AllEnemyBasesDestroyed>(OnAllEnemyBaseDestroyed);
    }
    private void OnSaveGame(SaveGameEvent evt) => SaveGame();
    private void OnLoadGame(LoadGameEvent evt) => LoadGame();
    private void OnAutoSave(ActionMadeEvent evt) => SaveGame(); 

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
        EventBus.Publish(new AllEnemyBasesDestroyed(false));

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

        //save other states here


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
    bool allEnemyBasesDestroyed = false;
    private void OnAllEnemyBaseDestroyed(AllEnemyBasesDestroyed destroyed)
    {
        allEnemyBasesDestroyed = destroyed.baseDestroyed;
    }

    public void CheckEnding()
    {
        int playerScore = player.getScore();
        int enemyScore = enemy.GetScore(); 

        //int playerBaseCount =  //for ltr when player base count is implemented

        if(allEnemyBasesDestroyed)
        {
            GenocideEnding();
        }
        else if(PlayerBasesDestroyed())
        {
            ExecutionEnding();
        }
        else if(turnManager.CurrentTurn == 31)
        {
            if(playerScore > enemyScore)
            {
                NormalEnding();
            }
            else
            {
                FailureEnding();
            }
        }
    }

    private bool PlayerBasesDestroyed()  // just a placeholder
    {
        var allBases = FindObjectsByType<TreeBase>(FindObjectsSortMode.None);
        return allBases.Length == 0;
    }

    private void NormalEnding()
    {

        Debug.Log("Normal Ending");
    }
    private void GenocideEnding()
    {

        Debug.Log("Genocide Ending");
    }
    private void ExecutionEnding()
    {

        Debug.Log("Execution Ending");
    }
    private void FailureEnding()
    {

        Debug.Log("Failure Ending");
    }
}
