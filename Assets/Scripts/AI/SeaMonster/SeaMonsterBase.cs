using UnityEngine;

public abstract class SeaMonsterBase : MonoBehaviour
{
    [Header("Stats")]
    public string MonsterName;
    public int Attack;
    public int Health;
    public int KillPoints;
    public int KillAP;
    public int MovementRange;
    public int AttackRange;

    [HideInInspector] public HexTile CurrentTile;
    [HideInInspector] public int CurrentTurn;

    //Unified ID for all sea monsters
    [HideInInspector] public int MonsterId;
    private static int nextMonsterId = 1;

    protected virtual void Awake()
    {
        MonsterId = nextMonsterId++;
    }

    public virtual void TakeDamage(int dmg)
    {
        Health -= dmg;
        if (Health <= 0)
            Die();
    }

    protected virtual void Die()
    {
        Vector2Int pos = CurrentTile != null ? CurrentTile.HexCoords : Vector2Int.zero;
        EventBus.Publish(new SeaMonsterEvents.SeaMonsterKilledEvent(this, pos));
        Destroy(gameObject);
    }
}
