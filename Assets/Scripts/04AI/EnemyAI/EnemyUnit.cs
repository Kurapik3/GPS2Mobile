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

    private int currentTurfRadius = 1;
    private int upgradeInterval = 3; //Number of turns between each base upgrade, will increase by 1 after each upgrade
    private int turnsSinceUpgrade = 0;

    public void Initialize(int id, string type, int hp, HexTile tile)
    {
        unitId = id;
        unitType = type;
        maxHP = hp;
        currentHP = hp;

        currentTile = tile;
        if (currentTile != null)
            currentTile.currentEnemyUnit = this;

        transform.position = MapManager.Instance.HexToWorld(tile.HexCoords);
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        Debug.Log($"[EnemyUnit] {unitType} took {amount} damage (HP: {currentHP})");

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

        transform.position = MapManager.Instance.HexToWorld(newTile.HexCoords);
    }
}