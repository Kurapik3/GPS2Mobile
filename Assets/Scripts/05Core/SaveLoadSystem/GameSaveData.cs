using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public List<FogTileData> revealedTiles = new();
    public List<DynamicObjectData> dynamicObjects = new();

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
}
