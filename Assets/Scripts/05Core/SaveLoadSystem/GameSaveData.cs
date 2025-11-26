using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public List<HexTileData> mapTiles = new();
    public List<FogTileData> revealedTiles = new();
    public List<DynamicObjectData> dynamicObjects = new();
    public List<UnitData> playerUnits = new();
    public List<UnitData> enemyUnits = new();
    public int currentTurn;
    public int playerScore;
    public int playerAP;
    public int enemyScore;

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
}
