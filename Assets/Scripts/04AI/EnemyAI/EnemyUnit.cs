using UnityEngine;

/// <summary>
/// Represents an AI-controlled enemy base on the map.
/// Does not generate AP; used for enemy spawning and AI logic.
/// </summary>
public class EnemyUnit : MonoBehaviour
{
    [Header("Enemy Base Settings")]
    [Header("Enemy Unit Settings")]
    public int unitId;
    public string unitType;
    public int maxHP;
    public int currentHP;
    public HexTile currentTile;
    public bool IsDestroyed => currentHP <= 0;

    public float baseHeightOffset = 2.0f;

    // ---- KENNETH'S ----
    private EnemyHPDisplay hpDisplay;
    // -------------------

    private float GetHeightOffset(HexTile tile)
    {
        return 2.0f;
    }

    public void Initialize(int id, string type, int hp, HexTile tile)
    {
        unitId = id;
        unitType = type;
        maxHP = hp;
        currentHP = hp;

        currentTile = tile;
        if (currentTile != null)
            currentTile.currentEnemyUnit = this;

        Vector3 pos = MapManager.Instance.HexToWorld(tile.HexCoords);
        pos.y += GetHeightOffset(tile);
        transform.position = pos;

        // ---- KENNETH'S ----
        hpDisplay = GetComponentInChildren<EnemyHPDisplay>();
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        Debug.Log($"[EnemyUnit] {unitType} took {amount} damage (HP: {currentHP})");

        // ---- KENNETH'S ----
        if (hpDisplay != null)
        {
            hpDisplay.OnHealthChanged();
        }
        // -------------------

        if (currentHP <= 0)
            Die();
    }

    private void Die()
    {
        Debug.Log($"[EnemyUnit] {unitType} destroyed!");
        EnemyUnitManager.Instance?.KillUnit(unitId);
    }

    public void UpdatePosition(HexTile newTile)
    {
        if (currentTile != null && currentTile.currentEnemyUnit == this)
            currentTile.currentEnemyUnit = null;

        currentTile = newTile;
        currentTile.currentEnemyUnit = this;

        Vector3 pos = MapManager.Instance.HexToWorld(newTile.HexCoords);
        pos.y += GetHeightOffset(newTile);
        transform.position = pos;
    }
}