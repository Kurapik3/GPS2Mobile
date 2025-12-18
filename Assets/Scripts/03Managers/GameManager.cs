using System;
using System.Collections;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GameSaveData;
using static EnemyUnitManager;


#if UNITY_EDITOR
using UnityEditor;
#endif
public class GameManager : MonoBehaviour
{
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private DynamicTileGenerator dynamicTileGen;
    [SerializeField] private FogSystem fogSystem;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private UnitSpawner unitSpawner;

    private string savePath => Path.Combine(Application.persistentDataPath, "save.json");
    public string SavePath => savePath;
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
    public static bool SaveExists()
    {
        return System.IO.File.Exists(Instance?.SavePath);
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
            try
            {
                var json = File.ReadAllText(savePath);
                cachedLoadData = JsonConvert.DeserializeObject<GameSaveData>(json);
                waitingForMapReady = true;
                if (turnManager != null)
                    turnManager.LoadedFromSave = true;
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
      
    }

    private void OnSaveGame(SaveGameEvent evt) => SaveGame();
    private void OnLoadGame(LoadGameEvent evt) => ForceLoadNow();
    private void OnAutoSave(ActionMadeEvent evt) => SaveGame(); 

    private void StartNewGame()
    {
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

            // treebase
            foreach (var tile in FindObjectsOfType<HexTile>())
            {
                // ---------- PLAYER / GROVE ----------
                if (tile.currentBuilding != null)
                {
                    var save = new GameSaveData.BaseSave
                    {
                        q = tile.q,
                        r = tile.r,
                        baseId = 0,
                        owner = 0,
                        level = 1,
                        currentPop = 0,
                        health = 0,
                        apPerTurn = 0,
                        turfRadius = 0,
                        isDefaultBase = false
                    };

                    if (tile.currentBuilding is TreeBase tb)
                    {
                        save.baseId = tb.TreeBaseId;
                        save.owner = 1;
                        save.level = tb.level;
                        save.currentPop = tb.currentPop;
                        save.health = tb.health;
                        save.apPerTurn = tb.apPerTurn;
                        save.turfRadius = tb.turfRadius;
                    }
                    else if (tile.currentBuilding is GroveBase gb)
                    {
                        save.baseId = gb.GetInstanceID();
                        save.owner = gb.GetOrigin() == GroveBase.BaseOrigin.Player ? 1 :
                                     gb.GetOrigin() == GroveBase.BaseOrigin.Enemy ? 2 : 0;
                        save.level = gb.GetFormerLevel();
                        Debug.Log($"[Save] Grove at ({tile.q},{tile.r}) - Origin: {gb.GetOrigin()}, Owner: {save.owner}, Level: {save.level}");
                    }
                    else
                    {
                        continue;
                    }

                    data.bases.Add(save);
                }

                // ---------- ENEMY BASE ----------
                if (tile.currentEnemyBase != null)
                {
                    EnemyBase eb = tile.currentEnemyBase;

                    data.bases.Add(new GameSaveData.BaseSave
                    {
                        q = tile.q,
                        r = tile.r,
                        baseId = eb.baseId,
                        owner = 2,
                        level = eb.level,
                        currentPop = 0,
                        health = eb.health,
                        turfRadius = 1,
                        isDefaultBase = false
                    });
                }
            }

            //tech tree
            data.techTree.IsFishing = TechTree.Instance.IsFishing;
            data.techTree.IsMetalScraps = TechTree.Instance.IsMetalScraps;
            data.techTree.IsArmor = TechTree.Instance.IsArmor;

            data.techTree.IsScouting = TechTree.Instance.IsScouting;
            data.techTree.IsCamouflage = TechTree.Instance.IsCamouflage;
            data.techTree.IsClearSight = TechTree.Instance.IsClearSight;

            data.techTree.IsHomeDef = TechTree.Instance.IsHomeDef;
            data.techTree.IsShooter = TechTree.Instance.IsShooter;
            data.techTree.IsNavalWarfare = TechTree.Instance.IsNavalWarfare;

            data.techTree.IsCreaturesResearch = TechTree.Instance.IsCreaturesResearch;
            data.techTree.IsMutualism = TechTree.Instance.IsMutualism;
            data.techTree.IsHunterMask = TechTree.Instance.IsHunterMask;
            data.techTree.IsTaming = TechTree.Instance.IsTaming;

            //save other states here

            //sea creature
            if (SeaMonsterManager.Instance != null)
            {
                foreach (var sm in SeaMonsterManager.Instance.ActiveMonsters)
                {
                    if (sm == null || sm.currentTile == null) continue;

                    data.seaMonsters.Add(new SeaMonsterSave
                    {
                        monsterId = sm.MonsterId,
                        monsterType = sm.GetType().Name, // Kraken / TurtleWall
                        q = sm.currentTile.HexCoords.x,
                        r = sm.currentTile.HexCoords.y,
                        hp = sm.health,
                        isTamed = sm.State == SeaMonsterState.Tamed
                    });
                }
            }


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

            foreach (var id in EnemyUnitManager.Instance.GetOwnedUnitIds())
            {
                var pos = EnemyUnitManager.Instance.GetUnitPosition(id);
                var type = EnemyUnitManager.Instance.GetUnitType(id);
                var obj = EnemyUnitManager.Instance.UnitObjects[id];
                var enemyUnit = obj.GetComponent<EnemyUnit>();

                data.enemyUnits.Add(new EnemyUnitSave
                {
                    id = id,
                    unitName = type,
                    q = pos.x,
                    r = pos.y,
                    baseId = EnemyUnitManager.Instance.GetBaseId(id),
                    hp = enemyUnit.currentHP,
                    aiState = (int)EnemyUnitManager.Instance.GetUnitState(id),
                    justSpawned = EnemyUnitManager.Instance.IsJustSpawned(id)
                });
            }

            data.nextEnemyId = EnemyUnitManager.Instance.NextUnitId;
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

        waitingForMapReady = true;
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
                    var tileCoords = new Vector2Int(u.q, u.r);
                    if (MapManager.Instance.TryGetTile(tileCoords, out HexTile destTile))
                    {
                        if (destTile.IsOccupiedByUnit)
                        {
                            Debug.LogWarning($"[Load] Tile {tileCoords} already occupied, skipping spawn for {u.unitName}");
                            continue;
                        }
                    }
                    int countBefore = UnitManager.Instance.GetAllUnits().Count;
                    unitSpawner.SpawnUnit(prefab, csvIndex, new Vector2Int(u.q, u.r));

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

            EnemyUnitManager.Instance.FullReset();
            EnemyUnitManager.Instance.SetNextUnitId(cachedLoadData.nextEnemyId);

            // Spawn enemy units
            foreach (var u in cachedLoadData.enemyUnits)
            {
                var prefab = EnemyUnitManager.Instance.unitPrefabs.Find(p => p.name == u.unitName);
                if (prefab == null) continue;

                EnemyUnitManager.Instance.SpawnLoadedUnit(
                    prefab,
                    u.id,
                    u.baseId,
                    u.unitName,
                    new Vector2Int(u.q, u.r),
                    u.hp,
                    (AIState)u.aiState,
                    u.justSpawned
                );
            }

            // Restore turn / scores / AP
            if (turnManager != null)
            {
                turnManager.LoadedFromSave = true;
                turnManager.CurrentTurn = cachedLoadData.currentTurn;
                turnManager.ForceStartPlayerTurnFromLoad();
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
                foreach (var tile in FindObjectsOfType<HexTile>())
                {
                    if (tile.IsFogged && tile.dynamicInstance != null)
                    {
                        foreach (var renderer in tile.dynamicInstance.GetComponentsInChildren<Renderer>())
                        {
                            renderer.enabled = false;
                        }
                    }
                }
            }

            //bases
            foreach (var baseSave in cachedLoadData.bases)
            {
                var tile = MapManager.Instance.GetTile(baseSave.q, baseSave.r);
                if (tile == null) continue;

                /* ---------- PLAYER / GROVE BUILDINGS ---------- */
                BuildingBase building = tile.currentBuilding;

                if (building is TreeBase treeBase)
                {
                    treeBase.SetTreeBaseId(baseSave.baseId);
                    treeBase.SetLevelDirect(baseSave.level);
                    treeBase.currentPop = baseSave.currentPop;
                    treeBase.health = baseSave.health;
                    treeBase.apPerTurn = baseSave.apPerTurn;
                    treeBase.turfRadius = baseSave.turfRadius;
                }
                else if (building is GroveBase grove)
                {
                    GroveBase.BaseOrigin origin = baseSave.owner switch
                    {
                        1 => GroveBase.BaseOrigin.Player,
                        2 => GroveBase.BaseOrigin.Enemy,
                        _ => GroveBase.BaseOrigin.Player
                    };
                    grove.SetFormerLevel(baseSave.level, origin);
                }

                /* ---------- ENEMY BASE---------- */
                EnemyBase enemyBase = tile.currentEnemyBase;
                if (enemyBase != null)
                {
                    enemyBase.baseId = baseSave.baseId;
                    enemyBase.level = baseSave.level;
                    enemyBase.health = baseSave.health;

                    // optional: add setter if you want to persist population
                    // enemyBase.SetCurrentPop(baseSave.currentPop);

                    // register only if missing
                    if (!EnemyBaseManager.Instance.Bases.ContainsKey(enemyBase.baseId))
                    {
                        EnemyBaseManager.Instance.RegisterBase(enemyBase);
                    }
                }

            }

            //techtree
            TechTree tech = FindFirstObjectByType<TechTree>();
            if (tech != null && cachedLoadData != null)
            {
                tech.RestoreFromSave(cachedLoadData.techTree);
                foreach (var node in FindObjectsOfType<TechNode>(true))
                {
                    node.UpdateAll();
                }
            }
            else
            {
                Debug.LogWarning("[GameManager] TechTree instance not found during load.");
            }


            //sea creature
            if (SeaMonsterManager.Instance != null && cachedLoadData.seaMonsters != null)
            {
                foreach (var smSave in cachedLoadData.seaMonsters)
                {
                    var monster = SeaMonsterManager.Instance.SpawnMonsterFromLoad(
                        smSave.monsterType,
                        smSave.monsterId,
                        new Vector2Int(smSave.q, smSave.r)
                    );

                    monster.SetHP(smSave.hp);

                    if (smSave.isTamed)
                        monster.Tame();
                    else
                        monster.Untame();
                }
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
#if UNITY_EDITOR
    [ContextMenu("Clear Saved Data")]
    void EditorClearSave() => ClearSave();

#endif
}


#if UNITY_EDITOR
[CustomEditor(typeof(GameManager))]
public class GameManagerInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameManager editor = (GameManager)target;
        GUILayout.Space(10);
        if (GUILayout.Button("Clear Saved Data"))
        {
            editor.ClearSave();
        }
    }
}
#endif
