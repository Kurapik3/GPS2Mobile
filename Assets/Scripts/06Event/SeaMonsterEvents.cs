using UnityEngine;

public static class SeaMonsterEvents
{
    public struct SeaMonsterTurnStartedEvent
    {
        public int Turn;
        public SeaMonsterTurnStartedEvent(int turn) => Turn = turn;
    }

    public struct SeaMonsterTurnEndEvent
    {
        public int Turn;
        public SeaMonsterTurnEndEvent(int turn) => Turn = turn;
    }

    public struct SeaMonsterSystemReadyEvent
    {
        public SeaMonsterManager Manager;
        public SeaMonsterSystemReadyEvent(SeaMonsterManager mgr) => Manager = mgr;
    }

    public struct SeaMonsterSpawnedEvent
    {
        public SeaMonsterBase Monster;
        public Vector2Int TilePos;
        public SeaMonsterSpawnedEvent(SeaMonsterBase monster, Vector2Int tilePos)
        {
            Monster = monster;
            TilePos = tilePos;
        }
    }

    public struct SeaMonsterMoveEvent
    {
        public SeaMonsterBase Monster;
        public Vector2Int From;
        public Vector2Int To;
        public SeaMonsterMoveEvent(SeaMonsterBase monster, Vector2Int from, Vector2Int to)
        {
            Monster = monster;
            From = from;
            To = to;
        }
    }

    public struct SeaMonsterKilledEvent
    {
        public SeaMonsterBase Monster;
        public Vector2Int TilePos;
        public SeaMonsterKilledEvent(SeaMonsterBase monster, Vector2Int tilePos)
        {
            Monster = monster;
            TilePos = tilePos;
        }
    }


    #region Kraken
    public struct KrakenPreSpawnWarningEvent
    {
        public int Turn;
        public KrakenPreSpawnWarningEvent(int turn) => Turn = turn;
    }

    //Event for UI layer to know Sea Monster targeting any unit and to show indicator
    public struct KrakenTargetsUnitEvent
    {
        public SeaMonsterBase Attacker;
        public GameObject Target;
        public KrakenTargetsUnitEvent(Kraken attacker, GameObject target)
        {
            Attacker = attacker;
            Target = target;
        }
    }

    //Event for Sea Monster attacking player or enemy unit
    public struct KrakenAttacksUnitEvent
    {
        public SeaMonsterBase Attacker;
        public GameObject Target;
        public int Damage;

        public KrakenAttacksUnitEvent(Kraken attacker, GameObject target, int damage)
        {
            Attacker = attacker;
            Target = target;
            Damage = damage;
        }
    }

    //Event for Sea Monster attacking another Sea Monster
    public struct KrakenAttacksMonsterEvent
    {
        public SeaMonsterBase Attacker;
        public SeaMonsterBase Target;
        public int Damage;

        public KrakenAttacksMonsterEvent(Kraken attacker, SeaMonsterBase target, int damage)
        {
            Attacker = attacker;
            Target = target;
            Damage = damage;
        }
    }
    #endregion

    #region TurtleWall
    public struct TurtleWallBlockEvent
    {
        public SeaMonsterBase Wall;
        public Vector2Int TilePos;
        public TurtleWallBlockEvent(SeaMonsterBase wall, Vector2Int tilePos)
        {
            Wall = wall;
            TilePos = tilePos;
        }
    }

    public struct TurtleWallUnblockEvent
    {
        public SeaMonsterBase Wall;
        public Vector2Int TilePos;
        public TurtleWallUnblockEvent(SeaMonsterBase wall, Vector2Int tilePos)
        {
            Wall = wall;
            TilePos = tilePos;
        }
    }
    #endregion

    #region TechTree
    public struct UnitSelectedEvent
    {
        public UnitBase Unit { get; }
        public bool IsSelected { get; }

        public UnitSelectedEvent(UnitBase unit, bool isSelected)
        {
            Unit = unit;
            IsSelected = isSelected;
        }
    }

    public struct TamingUnlockedEvent { }
    #endregion
}
