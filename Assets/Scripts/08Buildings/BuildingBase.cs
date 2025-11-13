using UnityEngine;

public class BuildingBase : MonoBehaviour
{
    [Header("Stats from CSV")]
    public string buildingName;
    public int health;
    public int developCost;
    public int apPerTurn;

    public int buildingId;
    public Vector3 RuntimePosition => transform.position;

    public HexTile currentTile;
    [SerializeField] private GameObject GrovePrefab;


    private void Start()
    {
        if (currentTile == null && MapManager.Instance != null)
        {
            Vector2Int hexCoord = MapManager.Instance.WorldToHex(transform.position);
            currentTile = MapManager.Instance.GetTile(hexCoord);

            if (currentTile != null)
            {
                currentTile.SetBuilding(this);
                Debug.Log($"{buildingName} placed at Hex {hexCoord} (auto-detected)");
            }
            else
            {
                Debug.LogWarning($"{buildingName} could NOT find a hex tile at position {transform.position}");
            }
        }
    }

    public virtual void Initialize(BuildingData data, HexTile tile)
    {
        buildingName = data.buildingName;
        health = data.health;
        apPerTurn = data.apPerTurn;
        developCost = data.developCost;

    }

    public virtual void OnTurnStart()
    {
        if (apPerTurn > 0)
        {
            PlayerTracker.Instance.addAP(apPerTurn);
            Debug.Log($"{buildingName} generated {apPerTurn} AP this turn.");
        }
    }

    public virtual void TakeDamage(int amount)
    {

        if (currentTile != null && currentTile.currentUnit != null)
        {
            currentTile.currentUnit.TakeDamage(amount);
            Debug.Log($"{buildingName} is protected by {currentTile.currentUnit.unitName}! Damage redirected to the unit.");
            return;
        }

        health -= amount;
        if (health <= 0)
        {
            DestroyBuilding();
        }
    }

    protected virtual void DestroyBuilding() //not correct
    {
        Debug.Log($"{buildingName} destroyed!");
        currentTile.BecomeRuin();

        if (GrovePrefab != null)
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            Instantiate(GrovePrefab, transform.position, Quaternion.identity, transform);
        }
        else
        {
            Debug.LogWarning($"No ruin prefab assigned for {buildingName}!");
        }
    }
}
