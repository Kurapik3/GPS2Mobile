using UnityEngine;

public static class SeaMonsterEvents
{
    public struct TurnStartedEvent
    {
        public int Turn;
        public TurnStartedEvent(int turn) => Turn = turn;
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

    public struct AllSeaMonstersClearedEvent
    {
        public int Turn;
        public AllSeaMonstersClearedEvent(int turn) => Turn = turn;
    }

    public struct SeaMonsterAttacksUnitEvent
    {
        public SeaMonsterBase Attacker;
        public UnitBase Target;

        public SeaMonsterAttacksUnitEvent(SeaMonsterBase attacker, UnitBase target)
        {
            Attacker = attacker;
            Target = target;
        }
    }

    // Event for Sea Monster attacking another Sea Monster
    public struct SeaMonsterAttacksMonsterEvent
    {
        public SeaMonsterBase Attacker;
        public SeaMonsterBase Target;

        public SeaMonsterAttacksMonsterEvent(SeaMonsterBase attacker, SeaMonsterBase target)
        {
            Attacker = attacker;
            Target = target;
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

    public struct KrakenPreSpawnWarningEvent
    {
        public int Turn;
        public KrakenPreSpawnWarningEvent(int turn) => Turn = turn;
    }
}
