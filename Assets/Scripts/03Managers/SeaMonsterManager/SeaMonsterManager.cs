using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static SeaMonsterEvents;

/// <summary>
/// Manages all active sea monsters, their spawn, death, and provides a central list for AI.
/// </summary>
public class SeaMonsterManager : MonoBehaviour
{
    public static SeaMonsterManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private SeaMonsterSpawner spawner;
    [SerializeField] private AudioClip krakenWarningSound;

    [Header("Feedback Settings")]
    [SerializeField] private float preSpawnDelay = 1.5f;
    [SerializeField] private float shakeIntensity = 0.6f;
    [SerializeField] private float shakeDuration = 0.5f;

    private readonly List<SeaMonsterBase> activeMonsters = new();
    public IReadOnlyList<SeaMonsterBase> ActiveMonsters => activeMonsters;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);
        else
            Instance = this;
    }

    private void OnEnable()
    {
        EventBus.Subscribe<SeaMonsterTurnStartedEvent>(OnTurnStarted);
        EventBus.Subscribe<SeaMonsterKilledEvent>(OnSeaMonsterKilled);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<SeaMonsterTurnStartedEvent>(OnTurnStarted);
        EventBus.Unsubscribe<SeaMonsterKilledEvent>(OnSeaMonsterKilled);
    }

    private void Start()
    {
        EventBus.Publish(new SeaMonsterSystemReadyEvent(this));
    }

    private void OnTurnStarted(SeaMonsterTurnStartedEvent evt)
    {
        int turn = evt.Turn;

        //Start at turn 10, then every 4 turns
        if (turn == 10 || (turn > 10 && (turn - 10) % 4 == 0))
        {
            StartCoroutine(SpawnSequence(turn));
        }
    }

    private IEnumerator SpawnSequence(int turn)
    {
        //SeaMonster warning (turn 10 only)
        if (turn == 10)
        {
            EventBus.Publish(new KrakenPreSpawnWarningEvent(turn));
            if (krakenWarningSound)
                AudioSource.PlayClipAtPoint(krakenWarningSound, Camera.main.transform.position);

            yield return StartCoroutine(ShakeCamera());
            yield return new WaitForSeconds(preSpawnDelay);
        }

        //Spawn random monster
        SeaMonsterBase monster = spawner.SpawnRandomMonster();
        if (monster != null)
        {
            RegisterMonster(monster);

            Vector2Int tilePos = monster.CurrentTile != null ? monster.CurrentTile.HexCoords : Vector2Int.zero;
            EventBus.Publish(new SeaMonsterSpawnedEvent(monster, tilePos));
        }
    }

    public void RegisterMonster(SeaMonsterBase monster)
    {
        if (monster == null) 
            return;
        if (!activeMonsters.Contains(monster))
            activeMonsters.Add(monster);
    }

    private IEnumerator ShakeCamera()
    {
        if (Camera.main == null) 
            yield break;

        Vector3 origin = Camera.main.transform.position;
        float t = 0f;

        while (t < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;
            Camera.main.transform.position = origin + new Vector3(x, y, 0);
            t += Time.deltaTime;
            yield return null;
        }

        Camera.main.transform.position = origin;
    }

    private void OnSeaMonsterKilled(SeaMonsterKilledEvent evt)
    {
        if (evt.Monster == null) 
            return;

        if (activeMonsters.Contains(evt.Monster))
            activeMonsters.Remove(evt.Monster);
    }

    public List<SeaMonsterBase> GetAllMonsters()
    {
        return new List<SeaMonsterBase>(activeMonsters);
    }

    public SeaMonsterBase GetMonsterById(int monsterId)
    {
        return activeMonsters.Find(m => m.MonsterId == monsterId);
    }
}
