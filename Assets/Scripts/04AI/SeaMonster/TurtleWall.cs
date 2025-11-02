using UnityEngine;
using static SeaMonsterEvents;

/// <summary>
/// Defensive sea monster that blocks tiles but can move slowly.
/// </summary>
public class TurtleWall : SeaMonsterBase
{
    [Header("TurtleWall Stats")]
    [SerializeField] private int baseHealth = 35;
    [SerializeField] private int baseMovementRange = 1;
    [SerializeField] private int rewardAP = 20;
    [SerializeField] private int rewardPoints = 2000;

    protected override void Awake()
    {
        base.Awake();
        Health = baseHealth;
        MovementRange = baseMovementRange;
        AttackRange = 0;
        Attack = 0;
        KillAP = rewardAP;
        KillPoints = rewardPoints;
        isBlocking = true;
    }

    public override void Initialize(HexTile spawnTile)
    {
        base.Initialize(spawnTile);

        //Publish block event so pathfinding knows to mark this tile as blocked
        EventBus.Publish(new TurtleWallBlockEvent(this, spawnTile.HexCoords));
        Debug.Log($"[TurtleWall] Spawned at {spawnTile.HexCoords}. Blocking tile.");
    }

    public override void PerformTurnAction()
    {
        if (hasActedThisTurn || CurrentTile == null)
            return;

        HexTile target = AIPathFinder.GetRandomReachableTileForSeaMonster(this); 
        if (target != null && target != CurrentTile)
        {
            MoveTo(target);
            Debug.Log($"[TurtleWall] Moves to {target.HexCoords}");
        }
        else
        {
            Debug.Log("[TurtleWall] No valid move found or chooses to stay.");
        }

        hasActedThisTurn = true;
    }

    protected override void Die()
    {
        if (isBlocking && CurrentTile != null)
        {
            EventBus.Publish(new TurtleWallUnblockEvent(this, CurrentTile.HexCoords));
            isBlocking = false;
        }

        base.Die();
    }
}
