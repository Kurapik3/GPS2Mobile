using UnityEngine;

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


    protected virtual void Start()
    {
        
    }
    public virtual void Initialize(UnitData data)
    {
        unitName = data.unitName;
        hp = data.hp;
        attack = data.attack;
        movement = data.movement;
        range = data.range;
        isCombat = data.isCombat;    
    }

    public virtual void Attack(UnitBase target)
    {
        if (!isCombat)
        {
            Debug.Log($"{unitName} cannot attack!");
            return;
        }

        if (target == null)
        {
            Debug.LogWarning($"{unitName} has no target to attack!");
            return;
        }

        // range 
        // if not in range , return

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

    //public void move()
    //{
        // work with ashley's hex thingy
        // move from hex to hex 
    //}
}
