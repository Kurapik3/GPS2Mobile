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


    //==================== Kraken only ====================
    public struct KrakenPreSpawnWarningEvent
    {
        public int Turn;
        public KrakenPreSpawnWarningEvent(int turn) => Turn = turn;
    }

    //Event for Sea Monster attacking player or enemy unit
    public struct KrakenAttacksUnitEvent
    {
        public SeaMonsterBase Attacker;
        public UnitBase Target;

        public KrakenAttacksUnitEvent(SeaMonsterBase attacker, UnitBase target)
        {
            Attacker = attacker;
            Target = target;
        }
    }

    //Event for Sea Monster attacking another Sea Monster
    public struct KrakenAttacksMonsterEvent
    {
        public SeaMonsterBase Attacker;
        public SeaMonsterBase Target;

        public KrakenAttacksMonsterEvent(SeaMonsterBase attacker, SeaMonsterBase target)
        {
            Attacker = attacker;
            Target = target;
        }
    }


    //==================== Turtle Wall only ====================
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
}
