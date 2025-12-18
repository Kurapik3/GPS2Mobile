using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public List<HexTileData> mapTiles = new();
    public List<FogTileData> revealedTiles = new();
    public List<DynamicObjectData> dynamicObjects = new();
    public List<UnitData> playerUnits = new();
    public List<EnemyUnitSave> enemyUnits = new();
    public List<BaseSave> bases = new();
    public TechTreeSave techTree = new();
    public List<SeaMonsterSave> seaMonsters = new();


    public int currentTurn;
    public int playerScore;
    public int playerAP;
    public int enemyScore;
    public int nextEnemyId;
    public enum BaseType
    {
        Grove,
        TreeBase,
        EnemyBase
    }

    [Serializable]
    public class DynamicObjectData
    {
        public int q;
        public int r;
        public string resourceId;
    }

    [Serializable]
    public class FogTileData
    {
        public int q;
        public int r;
    }
    
    [Serializable]
    public class UnitData
    {
        public string unitName;
        public int q;
        public int r;
        public int hp;
        public int movement;
        public int range;
        public bool isCombat;
    }

    [System.Serializable]
    public class EnemyUnitSave
    {
        public int id;
        public string unitName;
        public int q;
        public int r;
        public int baseId;
        public int hp;
        public int aiState;
        public bool justSpawned;
    }

    [Serializable]
    public class BaseSave
    {
        public BaseType baseType;

        public int baseId;
        public int owner; // 0=neutral, 1=player, 2=enemy
        public int q;
        public int r;

        public int level;
        public int currentPop;
        public int health;
        public int apPerTurn;
        public int turfRadius;
    }

    [Serializable]
    public class TechTreeSave
    {
        public bool IsFishing;
        public bool IsMetalScraps;
        public bool IsArmor;

        public bool IsScouting;
        public bool IsCamouflage;
        public bool IsClearSight;

        public bool IsHomeDef;
        public bool IsShooter;
        public bool IsNavalWarfare;

        public bool IsCreaturesResearch;
        public bool IsMutualism;
        public bool IsHunterMask;
        public bool IsTaming;
    }
    [Serializable]
    public class SeaMonsterSave
    {
        public int monsterId;
        public string monsterType;
        public int q;
        public int r;
        public int hp;
        public bool isTamed;
    }

}
