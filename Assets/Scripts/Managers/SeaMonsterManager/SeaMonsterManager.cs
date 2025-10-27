using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SeaMonsterManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SeaMonsterSpawner spawner;
    [SerializeField] private AudioClip krakenWarningSound;

    [Header("Feedback Settings")]
    [SerializeField] private float preSpawnDelay = 1.5f;
    [SerializeField] private float shakeIntensity = 0.6f;
    [SerializeField] private float shakeDuration = 0.5f;

    private List<SeaMonsterBase> activeMonsters = new List<SeaMonsterBase>();

    private void OnEnable()
    {
        EventBus.Subscribe<SeaMonsterEvents.TurnStartedEvent>(OnTurnStarted);
        EventBus.Subscribe<SeaMonsterEvents.SeaMonsterKilledEvent>(OnSeaMonsterKilled);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<SeaMonsterEvents.TurnStartedEvent>(OnTurnStarted);
        EventBus.Unsubscribe<SeaMonsterEvents.SeaMonsterKilledEvent>(OnSeaMonsterKilled);
    }

    private void Start()
    {
        EventBus.Publish(new SeaMonsterEvents.SeaMonsterSystemReadyEvent(this));
    }

    private void OnTurnStarted(SeaMonsterEvents.TurnStartedEvent evt)
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
        //Kraken warning (turn 10 only)
        if (turn == 10)
        {
            EventBus.Publish(new SeaMonsterEvents.KrakenPreSpawnWarningEvent(turn));
            if (krakenWarningSound)
                AudioSource.PlayClipAtPoint(krakenWarningSound, Camera.main.transform.position);

            yield return StartCoroutine(ShakeCamera());
            yield return new WaitForSeconds(preSpawnDelay);
        }

        //Randomly spawn
        SeaMonsterBase monster = spawner.SpawnRandomMonster();
        if (monster != null)
        {
            activeMonsters.Add(monster);

            Vector2Int tilePos = Vector2Int.zero;
            if (monster.CurrentTile != null)
            {
                tilePos = monster.CurrentTile.HexCoords;
            }
            EventBus.Publish(new SeaMonsterEvents.SeaMonsterSpawnedEvent(monster, tilePos));
        }
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

    private void OnSeaMonsterKilled(SeaMonsterEvents.SeaMonsterKilledEvent evt)
    {
        if (activeMonsters.Contains(evt.Monster))
            activeMonsters.Remove(evt.Monster);

        if (activeMonsters.Count == 0)
        {
            EventBus.Publish(new SeaMonsterEvents.AllSeaMonstersClearedEvent(evt.Monster != null ? evt.Monster.CurrentTurn : 0));
        }
    }
}
