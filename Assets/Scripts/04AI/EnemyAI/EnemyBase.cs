using UnityEngine;

/// <summary>
/// Represents an AI-controlled enemy base on the map.
/// Does not generate AP; used for enemy spawning and AI logic.
/// </summary>
public class EnemyBase : MonoBehaviour
{
    [Header("Enemy Base Settings")]
    public string baseName = "Enemy Base";
    public int health;
    public int maxUnits = 3;
    public int currentUnits = 0; //To track how many units are housed in each base
    public HexTile currentTile;

    [HideInInspector] public int baseId;
    public bool IsDestroyed => health <= 0;

    private void Start()
    {
        if (currentTile == null)
        {
            if (MapManager.Instance != null)
            {
                Vector2Int hexCoord = MapManager.Instance.WorldToHex(transform.position);
                HexTile tile = MapManager.Instance.GetTile(hexCoord);
                if (tile != null)
                {
                    currentTile = tile;
                }
                else
                {
                    Debug.LogWarning($"[EnemyBase] No HexTile found at hex {hexCoord} for base '{baseName}' (world pos {transform.position}).");
                }
            }
            else
            {
                Debug.LogError("[EnemyBase] MapManager.Instance is null!");
            }
        }

        if (currentTile != null)
        {
            currentTile.currentEnemyBase = this;
        }

        //Randomize HP between 20 and 35
        health = Random.Range(20, 36);

        //Register this base
        if (EnemyBaseManager.Instance != null)
        {
            baseId = EnemyBaseManager.Instance.RegisterBase(this);
        }
        else
        {
            Debug.LogError("[EnemyBase] EnemyBaseManager not found in scene!");
        }

        Debug.Log($"[EnemyBase] Spawned {baseName} with {health} HP.");
    }

    private void OnDestroy()
    {
        if (EnemyBaseManager.Instance != null)
            EnemyBaseManager.Instance.UnregisterBase(this);
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        Debug.Log($"[EnemyBase] {baseName} took {amount} damage (HP: {health})");

        if (health <= 0)
        {
            DestroyBase();
        }
    }

    private void DestroyBase()
    {
        Debug.Log($"[EnemyBase] {baseName} destroyed!");
        EnemyBaseManager.Instance?.OnBaseDestroyed(this);
        if (currentTile != null)
        {
            SpawnGroveAt(currentTile);
        }
        else
        {
            Debug.LogWarning("[EnemyBase] Cannot spawn Groove — currentTile is null!");
        }

        if (currentTile != null)
            currentTile.currentEnemyBase = null;

        Destroy(gameObject);
    }

    private void SpawnGroveAt(HexTile tile)
    {
        GameObject grovePrefab = Resources.Load<GameObject>("Structures/Grove");
        if (grovePrefab == null)
        {
            Debug.LogError("[EnemyBase] Groove prefab not found! (Expected at Resources/Structures/Grove)");
            return;
        }

        GameObject groveObj = Instantiate(grovePrefab, tile.transform.position, Quaternion.identity);
        BuildingBase groveBuilding = groveObj.GetComponent<BuildingBase>();
        BuildingData data = groveObj.GetComponent<BuildingData>();

        if (groveBuilding == null)
        {
            Debug.LogWarning("[EnemyBase] Spawned Groove prefab but no BuildingBase/GrooveBase component found!");
            return;
        }
        if (data == null)
        {
            Debug.LogWarning("[EnemyBase] Groove prefab missing BuildingData component!");
            return;
        }
        groveBuilding.Initialize(data, tile);
        Debug.Log($"[EnemyBase] Spawned Groove at {tile.HexCoords}");
    }

    public void OnUnitSpawned()
    {
        currentUnits++;
    }

    public void OnUnitDestroyed()
    {
        currentUnits = Mathf.Max(0, currentUnits - 1);
    }

    public void OnTurnStart()
    {
        Debug.Log($"[EnemyBase] Turn start for {baseName} (Tile: {currentTile?.HexCoords})");
    }
}
