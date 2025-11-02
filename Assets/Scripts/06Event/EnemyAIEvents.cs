using UnityEngine;

public static class EnemyAIEvents
{
    #region Turn
    //Turn
    public struct EnemyTurnStartEvent 
    { 
        public int Turn; 
        public EnemyTurnStartEvent(int turn) => Turn = turn; 
    }
    public struct EnemyTurnEndEvent 
    { 
        public int Turn; 
        public EnemyTurnEndEvent(int turn) => Turn = turn; 
    }
    #endregion

    #region Phase
    public struct ExecuteBasePhaseEvent 
    { 
        public int Turn; 
        public ExecuteBasePhaseEvent(int turn) => Turn = turn; 
    }
    public struct BasePhaseEndEvent
    {
        public int Turn;
        public BasePhaseEndEvent(int turn) => Turn = turn;
    }

    public struct ExecuteDormantPhaseEvent 
    { 
        public int Turn; 
        public ExecuteDormantPhaseEvent(int turn) => Turn = turn; 
    }
    public struct DormantPhaseEndEvent
    {
        public int Turn;
        public DormantPhaseEndEvent(int turn) => Turn = turn;
    }

    public struct ExecuteAggressivePhaseEvent 
    { 
        public int Turn; 
        public ExecuteAggressivePhaseEvent(int turn) => Turn = turn; 
    }
    public struct AggressivePhaseEndEvent
    {
        public int Turn;
        public AggressivePhaseEndEvent(int turn) => Turn = turn;
    }

    public struct ExecuteBuilderPhaseEvent
    {
        public int Turn;
        public ExecuteBuilderPhaseEvent(int turn) => Turn = turn;
    }
    public struct BuilderPhaseEndEvent
    {
        public int Turn;
        public BuilderPhaseEndEvent(int turn) => Turn = turn;
    }
    #endregion

    public struct MapReadyEvent 
    { 
        public MapGenerator Map; 
        public MapReadyEvent(MapGenerator m) => Map = m; 
    }

    #region Request
    //Actions - published by AI modules, handled by Unit/Base managers
    public struct EnemySpawnRequestEvent 
    { 
        public int BaseId; 
        public string UnitType; 
        public EnemySpawnRequestEvent(int baseId, string unitType) 
        { 
            BaseId = baseId; UnitType = unitType; 
        } 
    }
    public struct EnemyMoveRequestEvent 
    { 
        public int UnitId; 
        public Vector2Int Destination; 
        public EnemyMoveRequestEvent(int unitId, Vector2Int dest) 
        { 
            UnitId = unitId; 
            Destination = dest; 
        } 
    }
    public struct EnemyAttackRequestEvent 
    {
        public int AttackerId; 
        public int TargetId; 
        public EnemyAttackRequestEvent(int attackerId, int targetId) 
        { 
            AttackerId = attackerId; TargetId = targetId; 
        } 
    }
    #endregion

    #region NotificationAfterPerformingActions
    //Notifications after managers perform actions
    public struct EnemySpawnedEvent 
    { 
        public int UnitId; 
        public int BaseId; 
        public string UnitType; 
        public Vector2Int Position; 
        public EnemySpawnedEvent(int unitId, int baseId, string unitType, Vector2Int pos) 
        { 
            UnitId = unitId; 
            BaseId = baseId; 
            UnitType = unitType; 
            Position = pos; 
        } 
    }
    public struct EnemyMovedEvent 
    { 
        public int UnitId; 
        public Vector2Int From; 
        public Vector2Int To; 
        public EnemyMovedEvent(int id, Vector2Int from, Vector2Int to) 
        { 
            UnitId = id; 
            From = from; 
            To = to; 
        } 
    }
    public struct EnemyAttackedEvent 
    { 
        public int AttackerId; 
        public int TargetId; 
        public EnemyAttackedEvent(int a, int t) 
        { 
            AttackerId = a; 
            TargetId = t; 
        } 
    }
    #endregion

    public struct BuilderDevelopGroveEvent
    {
        public int UnitId;
        public Vector2Int GrovePosition;

        public BuilderDevelopGroveEvent(int unitId, Vector2Int grovePos)
        {
            UnitId = unitId;
            GrovePosition = grovePos;
        }
    }
}
