using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using System.Linq;
//THIS SCRIPT IS FOR EDITOR MAPS OR START OF GAME SAVES NOT RUNTIME SAVE MAPS
//Used for base map layout at the start of a game
public static class MapSaveLoad
{
    private class SerializableMapWrapper
    {
        public int version = 1;
        public MapData map;
    }

    private static string GetPath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, fileName + ".json");
    }

    public static void Save(MapData mapData, string fileName)
    {
        if (mapData == null)
        {
            Debug.LogError("MapSaveLoad.Save called with null MapData!");
            return;
        }

        var wrapper = new SerializableMapWrapper
        {
            map = mapData,
            version = 1
        };

        string json = JsonConvert.SerializeObject(wrapper, Formatting.Indented);
        File.WriteAllText(GetPath(fileName), json);

        Debug.Log($"[MapSaveLoad] Map saved: {GetPath(fileName)}");
    }

    public static MapData Load(string fileName)
    {
        string path = GetPath(fileName);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[MapSaveLoad] No saved map found at: {path}");
            return null;
        }

        string json = File.ReadAllText(path);
        var wrapper = JsonConvert.DeserializeObject<SerializableMapWrapper>(json);

        if (wrapper?.map == null)
        {
            Debug.LogError("[MapSaveLoad] JSON loaded but MapData is null!");
            return null;
        }

        // Create a new ScriptableObject instance and copy values
        MapData mapData = ScriptableObject.CreateInstance<MapData>();
        mapData.tiles = wrapper.map.tiles.Select(t => t.Clone()).ToList();

        Debug.Log($"[MapSaveLoad] Map loaded from: {path}");
        return mapData;
    }
}
//private static string GetPath(string fileName)
    //{
    //    return Path.Combine(Application.persistentDataPath, fileName + ".json");
    //}

    //public static void Save(MapData mapData, string fileName)
    //{
    //    string json = JsonUtility.ToJson(mapData, true);
    //    File.WriteAllText(GetPath(fileName), json);
    //    Debug.Log($"Map saved to {GetPath(fileName)}");
    //}

    //public static MapData Load(string fileName)
    //{
    //    string path = GetPath(fileName);
    //    if (!File.Exists(path))
    //    {
    //        Debug.LogWarning($"No saved map found at: {path}");
    //        return null;
    //    }

    //    string json = File.ReadAllText(path);
    //    MapData mapData = ScriptableObject.CreateInstance<MapData>();
    //    JsonUtility.FromJsonOverwrite(json, mapData);
    //    Debug.Log($"Map loaded from JSON: {path}");
    //    return mapData;
    //}