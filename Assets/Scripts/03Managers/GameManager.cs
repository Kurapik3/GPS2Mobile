using System;
using System.Collections;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private DynamicTileGenerator dynamicTileGen;
    [SerializeField] private FogSystem fogSystem;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private UnitSpawner unitSpawner;

    private string savePath => Path.Combine(Application.persistentDataPath, "save.json");
    public static GameManager Instance { get; private set; }
    private GameSaveData cachedLoadData;
    private bool waitingForMapReady = false;

    private TribeStatsUI tribeStats;

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
    //void Start()
    //{
    //    player = PlayerTracker.Instance;
    //    enemy = EnemyTracker.Instance;
    //}
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        MapGenerator.OnMapReady += OnMapReady;
        EventBus.Subscribe<SaveGameEvent>(OnSaveGame);
        EventBus.Subscribe<LoadGameEvent>(OnLoadGame);
        EventBus.Subscribe<ActionMadeEvent>(OnAutoSave);
        EventBus.Subscribe<AllEnemyBasesDestroyed>(OnAllEnemyBaseDestroyed);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        MapGenerator.OnMapReady -= OnMapReady;
        EventBus.Unsubscribe<SaveGameEvent>(OnSaveGame);
        EventBus.Unsubscribe<LoadGameEvent>(OnLoadGame);
        EventBus.Unsubscribe<ActionMadeEvent>(OnAutoSave);
        EventBus.Unsubscribe<AllEnemyBasesDestroyed>(OnAllEnemyBaseDestroyed);
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        mapGenerator = FindFirstObjectByType<MapGenerator>();
        dynamicTileGen = FindFirstObjectByType<DynamicTileGenerator>();
        fogSystem = FindFirstObjectByType<FogSystem>();
        turnManager = FindFirstObjectByType<TurnManager>();
        unitSpawner = FindFirstObjectByType<UnitSpawner>();
        // Refresh reference because scene reload destroys old objects
        mapGenerator = FindFirstObjectByType<MapGenerator>();
        if (mapGenerator == null)
        {
            Debug.LogWarning("[GameManager] No MapGenerator found in scene.");
            return;
        }
        if (EnemyUnitManager.Instance != null)
        {
            EnemyUnitManager.Instance.RefreshReferences();
        }
        if (File.Exists(savePath))
        {
            // Read save file into cache (do not apply yet)
            try
            {
                var json = File.ReadAllText(savePath);
                cachedLoadData = JsonConvert.DeserializeObject<GameSaveData>(json);
                waitingForMapReady = true;

                var runtimeMap = ScriptableObject.CreateInstance<MapData>();
                runtimeMap.tiles = cachedLoadData.mapTiles.Select(t => t.Clone()).ToList();
                mapGenerator.SetMapData(runtimeMap);
                mapGenerator.GenerateFromData();
                return;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameManager] Save corrupted: {ex}");
            }
        }
        var defaultMap = Resources.Load<MapData>("DefaultBaseMap");
        if (defaultMap == null)
        {
            Debug.LogError("DefaultBaseMap.asset is missing from Resources folder!");
            return;
        }

        mapGenerator.SetMapData(defaultMap);
        mapGenerator.GenerateFromData();

        // Initialize fresh game systems
        dynamicTileGen.GenerateDynamicElements();
        fogSystem.InitializeFog();
        EventBus.Publish(new AllEnemyBasesDestroyed(false));
        Debug.Log($"UnitManager found: {FindFirstObjectByType<UnitManager>() != null}");
        //else
        //{
        //    // No runtime save: start a fresh game. Use editor-saved base map if available, else default
        //    MapData loadedData = MapSaveLoad.Load("MySavedMap");
        //    if (loadedData == null)
        //        loadedData = Resources.Load<MapData>("DefaultBaseMap");

        //    if (loadedData == null)
        //    {
        //        Debug.LogError("[GameManager] No base map found to start a new game.");
        //        return;
        //    }

        //    mapGenerator.SetMapData(loadedData);
        //    mapGenerator.GenerateFromData();

        //    // initialize runtime systems for a fresh start
        //    dynamicTileGen.GenerateDynamicElements();
        //    fogSystem.InitializeFog();
        //    EventBus.Publish(new AllEnemyBasesDestroyed(false));

        //    Debug.Log("[GameManager] Started new game (no runtime save).");
        //}
    }

    private void OnSaveGame(SaveGameEvent evt) => SaveGame();
    private void OnLoadGame(LoadGameEvent evt) => ForceLoadNow();
    private void OnAutoSave(ActionMadeEvent evt) => SaveGame(); 

    private void StartNewGame()
    {
        //MapData loadedData = Resources.Load<MapData>("DefaultBaseMap");
        //MapData loadedData = MapSaveLoad.Load("MySavedMap");
        MapData loadedData = MapSaveLoad.Load("MySavedMap");

        if (loadedData == null)
        {
            loadedData = Resources.Load<MapData>("DefaultBaseMap");
        }
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
        try
        {
            GameSaveData data = new();
            if (mapGenerator?.MapData?.tiles != null)
            {
                data.mapTiles = mapGenerator.MapData.tiles.Select(t => t.Clone()).ToList();
            }
            // Save revealed fog tiles
            //data.revealedTiles.Clear();
            foreach (var tile in fogSystem.revealedTiles)
            {
                data.revealedTiles.Add(new GameSaveData.FogTileData { q = tile.x, r = tile.y });
            }

            // Save dynamic tiles
            dynamicTileGen.SaveDynamicObjects(data);

            //save other states here

            // Player + enemy units
            foreach (var unit in UnitManager.Instance.GetAllUnits())
            {
                data.playerUnits.Add(new GameSaveData.UnitData
                {
                    unitName = unit.unitName,
                    q = unit.currentTile.q,
                    r = unit.currentTile.r,
                    hp = unit.hp,
                    movement = unit.movement,
                    range = unit.range,
                    isCombat = unit.isCombat
                });
            }

            foreach (var unit in EnemyUnitManager.Instance.GetAllUnits())
            {
                data.enemyUnits.Add(new GameSaveData.UnitData
                {
                    unitName = unit.unitName,
                    q = unit.currentTile.HexCoords.x,
                    r = unit.currentTile.HexCoords.y,
                    hp = unit.hp
                });
            }

            data.currentTurn = turnManager.CurrentTurn;
            data.playerScore = PlayerTracker.Instance?.getScore() ?? 0;
            data.playerAP = PlayerTracker.Instance?.getAp() ?? 0;
            data.enemyScore = EnemyTracker.Instance?.GetScore() ?? 0;

            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(savePath, json);
            Debug.Log($"Game saved to {savePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameManager] Save failed: {ex}");
        }
    }
    // --- Loading flow control ---
    // Called from EventBus if player triggers a load mid-game
    // We will decode file and set cachedLoadData, then regenerate the map to trigger OnMapReady.
    private void ForceLoadNow()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogWarning("[GameManager] No save file found to load.");
            return;
        }
        try
        {
            cachedLoadData = JsonConvert.DeserializeObject<GameSaveData>(File.ReadAllText(savePath));
            waitingForMapReady = true;

            MapData placeholderMap = ScriptableObject.CreateInstance<MapData>();
            placeholderMap.tiles = cachedLoadData.mapTiles.Select(t => t.Clone()).ToList();
            mapGenerator.SetMapData(placeholderMap);
            mapGenerator.GenerateFromData();
            Debug.Log("[GameManager] ForceLoadNow: regeneration started, waiting for MapReady.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameManager] ForceLoadNow failed: {ex}");
            cachedLoadData = null;
            waitingForMapReady = false;
        }
    }
    public void LoadGame()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogWarning("No save file found!");
            return;
        }
        cachedLoadData = JsonConvert.DeserializeObject<GameSaveData>(File.ReadAllText(savePath));

        // Map will load dynamic elements only AFTER map is ready
        waitingForMapReady = true;
        //string json = File.ReadAllText(savePath);
        //GameSaveData data = JsonConvert.DeserializeObject<GameSaveData>(json);

        //if (mapGenerator.MapData == null)
        //{
        //    var baseMap = Resources.Load<MapData>("DefaultBaseMap");
        //    mapGenerator.SetMapData(baseMap);
        //    mapGenerator.GenerateFromData();
        //}

        //// Reveal fog tiles
        //fogSystem.InitializeFog();
        //foreach (var tileData in data.revealedTiles)
        //{
        //    fogSystem.RevealTilesAround(new Vector2Int(tileData.q, tileData.r), 0);
        //}

        //// Load units
        //UnitManager.Instance.ClearAllUnits();
        //foreach (var u in data.playerUnits)
        //{
        //    GameObject prefab = null;

        //    // Match the prefab based on unitName
        //    if (u.unitName == "Builder")
        //    {
        //        prefab = unitSpawner.BuilderPrefab;
        //    }
        //    else if (u.unitName == "Scout")
        //    {
        //        prefab = unitSpawner.ScoutPrefab;
        //    }
        //    // add more if you have other unit types

        //    if (prefab == null)
        //    {
        //        Debug.LogWarning($"Prefab for {u.unitName} not found, skipping.");
        //        continue;
        //    }

        //    // Find CSV index for this unit
        //    int csvIndex = unitSpawner.unitDatabase.GetAllUnits().FindIndex(d => d.unitName == u.unitName);
        //    if (csvIndex < 0)
        //    {
        //        Debug.LogWarning($"Unit {u.unitName} not found in database, skipping.");
        //        continue;
        //    }

        //    unitSpawner.CreateUnit(prefab, csvIndex);
        //    var spawnedUnit = UnitManager.Instance.GetAllUnits().Last();

        //    spawnedUnit.SetPositionToTile(u.q, u.r);
        //}

        //EnemyUnitManager.Instance.ClearAll();
        //foreach (var u in data.enemyUnits)
        //{
        //    var prefab = EnemyUnitManager.Instance.unitPrefabs
        //        .Find(p => p.name == u.unitName);
        //    if (prefab != null)
        //        EnemyUnitManager.Instance.RegisterUnit(
        //            Instantiate(prefab),
        //            0,
        //            u.unitName,
        //            new Vector2Int(u.q, u.r)
        //        );
        //}

        //turnManager.CurrentTurn = data.currentTurn;
        //player.currentScore = data.playerScore;
        //player.currentAP = data.playerAP;
        //enemy.currentScore = data.enemyScore;

        //// Load dynamic objects
        //// dynamicTileGen.LoadDynamicObjects(data);
        //cachedLoadData = data;            
        //waitForMapReady= true;

        //EnemyUnitManager.Instance.UpdateEnemyVisibility();
        //Debug.Log("Game loaded!");
    }

    bool allEnemyBasesDestroyed = false;
    private void OnAllEnemyBaseDestroyed(AllEnemyBasesDestroyed destroyed)
    {
        allEnemyBasesDestroyed = destroyed.baseDestroyed;
    }

    public void ClearSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("Save data cleared!");
        }
    }


    public void CheckEnding()
    {
        int playerScore = PlayerTracker.Instance?.getScore() ?? 0;
        int enemyScore = EnemyTracker.Instance?.GetScore() ?? 0;
        Debug.Log($"[GameManager] Checking ending condition at Turn {turnManager.CurrentTurn}");

        //int playerBaseCount =  //for ltr when player base count is implemented

        if (allEnemyBasesDestroyed)
        {
            GenocideEnding();
            tribeStats?.ShowAsEndGameResult(isVictory: true);
        }
        else if(PlayerBasesDestroyed())
        {
            ExecutionEnding();
            tribeStats?.ShowAsEndGameResult(isVictory: false);
        }
        else if(turnManager.CurrentTurn == 30)
        {
            Debug.Log($"[GameManager] Turn 30 reached! Player Score: {playerScore}, Enemy Score: {enemyScore}");
            if (playerScore > enemyScore)
            {
                NormalEnding();
                tribeStats?.ShowAsEndGameResult(isVictory: true);
            }
            else
            {
                FailureEnding();
                tribeStats?.ShowAsEndGameResult(isVictory: false);
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
        Debug.Log("Normal Ending - Victory");
    }

    private void GenocideEnding()
    {
        Debug.Log("Genocide Ending - Victory");
    }

    private void ExecutionEnding()
    {
        Debug.Log("Execution Ending - Defeat");
    }

    private void FailureEnding()
    {
        Debug.Log("Failure Ending - Defeat");
    }

#if UNITY_EDITOR
    [ContextMenu("Clear Saved Data")]
        void EditorClearSave() => ClearSave();
#endif

    private void OnMapReady(MapGenerator gen)
    {
        if (!waitingForMapReady || cachedLoadData == null)
        {
            return;
        }
        StartCoroutine(RestoreGameStateAfterDelay());
    }

    private IEnumerator RestoreGameStateAfterDelay()
    {
        yield return null;
        try
        {
            //Restore fog
            if (fogSystem != null)
            {
                //fogSystem.InitializeFog(); // hide all fog
                //fogSystem.revealedTiles = cachedLoadData.revealedTiles
                //.Select(t => new Vector2Int(t.q, t.r))
                //.ToList();
                //foreach (var tileData in cachedLoadData.revealedTiles)
                //{
                //    Vector2Int coord = new Vector2Int(tileData.q, tileData.r);
                //    if (MapManager.Instance.TryGetTile(coord, out HexTile tile))
                //    {
                //        tile.RemoveFog();
                //    }
                //}
                fogSystem.InitializeFog();
                foreach (var tileData in cachedLoadData.revealedTiles)
                {
                    fogSystem.RevealTilesAround(new Vector2Int(tileData.q, tileData.r), 0);
                }
            }

            // Clear existing units
            if (UnitManager.Instance != null)
            {
                UnitManager.Instance.ClearAllUnits();
                Debug.Log("SuccessfullyCleared All previous player units");
            }
            if (EnemyUnitManager.Instance != null)
            {
                EnemyUnitManager.Instance.ClearAll();
                Debug.Log("SuccessfullyCleared All previous enemy units");
            }

            // Spawn player units and move them to saved tiles
            if (unitSpawner != null && UnitManager.Instance != null)
            {
                foreach (var u in cachedLoadData.playerUnits)
                {
                    GameObject prefab = unitSpawner.GetUnitPrefabByName(u.unitName);
                    if (prefab == null)
                    {
                        Debug.LogWarning($"[GameManager] Prefab for {u.unitName} not found (player). Skipping.");
                        continue;
                    }

                    int csvIndex = unitSpawner.unitDatabase?.GetAllUnits().FindIndex(d => d.unitName == u.unitName) ?? -1;
                    if (csvIndex < 0)
                    {
                        Debug.LogWarning($"[GameManager] Unit {u.unitName} not found in database, skipping.");
                        continue;
                    }
                    int countBefore = UnitManager.Instance.GetAllUnits().Count;
                    unitSpawner.CreateUnit(prefab, csvIndex);

                    var allUnits = UnitManager.Instance.GetAllUnits();
                    if (allUnits.Count <= countBefore)
                    {
                        Debug.LogWarning($"Unit {u.unitName} not registered in UnitManager.");
                        continue;
                    }
                    var spawned = allUnits[allUnits.Count - 1];
                    // restore hp/movement/range if you have setters, else those values are loaded when unit is created from CSV
                    spawned.SetPositionToTile(u.q, u.r);
                    spawned.hp = u.hp;
                    spawned.movement = u.movement;
                    spawned.range = u.range;
                    spawned.isCombat = u.isCombat;
                }
            }


            // Spawn enemy units
            if (EnemyUnitManager.Instance != null)
            {
                foreach (var u in cachedLoadData.enemyUnits)
                {
                    var prefab = EnemyUnitManager.Instance.unitPrefabs.Find(p => p.name == u.unitName);
                    if (prefab != null)
                    {
                        EnemyUnitManager.Instance.RegisterUnit(
                            Instantiate(prefab),
                            0,
                            u.unitName,
                            new Vector2Int(u.q, u.r)
                        );
                    }
                    else
                    {
                        Debug.LogWarning($"[GameManager] Prefab for {u.unitName} not found (enemy).");
                    }
                }
            }

            // Restore turn / scores / AP
            if (turnManager != null)
            {
                turnManager.CurrentTurn = cachedLoadData.currentTurn;
            }
            if (PlayerTracker.Instance != null)
            {
                PlayerTracker.Instance.currentScore = cachedLoadData.playerScore;
                PlayerTracker.Instance.currentAP = cachedLoadData.playerAP;
            }
            if (EnemyTracker.Instance != null)
            {
                EnemyTracker.Instance.currentScore = cachedLoadData.enemyScore;
            }

            // Load dynamic objects AFTER tiles exist
            if (dynamicTileGen != null)
            {
                dynamicTileGen.LoadDynamicObjects(cachedLoadData);
            }

            // Refresh enemy visibility
            StartCoroutine(DelayedUpdateEnemyVisibility());

            Debug.Log("[GameManager] Runtime save restored successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameManager] Error during map reconstruction: {ex}");
        }
        finally
        {
            cachedLoadData = null;
            waitingForMapReady = false;
        }
    }
    private void OnApplicationQuit()
    {
        SaveGame();
    }
    private IEnumerator DelayedUpdateEnemyVisibility()
    {
        yield return null;
        if (EnemyUnitManager.Instance != null)
        {
            EnemyUnitManager.Instance.UpdateEnemyVisibility();
        }
    }
    //GameManager.Instance?.SaveGame(); <- use for settings to main menu button
    //also add EventBus.Publish(new ActionMadeEvent()); to after player movement, player attack, after tech tree researched, after tree base upgrade, after extract tiles, after tame sea creature

}
