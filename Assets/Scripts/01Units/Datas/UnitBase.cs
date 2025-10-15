using System.Collections.Generic;
using UnityEngine;
//using static UnityEditor.PlayerSettings;

public abstract class UnitBase : MonoBehaviour
{
    [Header("Base Stats (Loaded from CSV)")]
    public string unitName;
    public int cost;
    public int range;
    public int movement;
    public int hp;
    public int attack;
    public bool isCombat;

    public bool isSelected = false;

    public HexTile currentTile;
    private Renderer rend;
    protected virtual void Start()
    {
        rend = GetComponent<Renderer>();
        UpdateSelectionVisual();

    }
    public virtual void Initialize(UnitData data, HexTile startingTile)
    {
        unitName = data.unitName;
        hp = data.hp;
        attack = data.attack;
        movement = data.movement;
        range = data.range;
        isCombat = data.isCombat;

        currentTile = startingTile;
        if (startingTile != null)
        {
            Vector3 pos = startingTile.transform.position;
            pos.y = 2f;
            transform.position = pos;
        }

    }

    public virtual void Attack(UnitBase target)
    {
        if (currentTile == null || target.currentTile == null)
        {
            Debug.LogWarning("Either attacker or target is not on a tile!");
            return;
        }

        int distance = HexDistance(currentTile.q, currentTile.r, target.currentTile.q, target.currentTile.r);

        // Check if target is within attack range
        if (distance > range)
        {
            Debug.Log($"{unitName} tried to attack {target.unitName}, but target is out of range! (distance: {distance}, range: {range})");
            return;
        }

        target.TakeDamage(attack);
        Debug.Log($"{unitName} attacked {target.unitName} for {attack} damage!");
    }

    public virtual void TakeDamage(int amount)
    {

        hp -= amount;
        if (hp <= 0)
        {
            hp = 0;
            Die();
        }
        Debug.Log($"{unitName} took {amount} damage. Remaining HP: {hp}");
    }

    protected virtual void Die()
    {
        Debug.Log($"{unitName} has died!");
        Destroy(gameObject);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateSelectionVisual();
    }

    private void UpdateSelectionVisual()
    {
        if (rend != null)
            rend.material.color = isSelected ? Color.yellow : Color.white;
    }

    public List<HexTile> GetAvailableTiles()
    {
        List<HexTile> tiles = new List<HexTile>();
        foreach (var tile in MapManager.Instance.GetTiles())
        {
            if (HexDistance(currentTile.q, currentTile.r, tile.q, tile.r) <= movement && tile.IsWalkableForAI())
            {
                tiles.Add(tile);
            }
        }
        return tiles;
    }


    public virtual void Move(HexTile targetTile)
    {
        if (targetTile == null) return;
        transform.position = targetTile.transform.position + Vector3.up * 2f; // optional y offset
        currentTile = targetTile;
        Debug.Log($"{unitName} moved to ({currentTile.q}, {currentTile.r})");
    }

    protected int HexDistance(int q1, int r1, int q2, int r2)
    {
        int dq = q2 - q1;
        int dr = r2 - r1;
        int ds = (-q2 - r2) - (-q1 - r1);
        return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(ds)) / 2;
    }

}
