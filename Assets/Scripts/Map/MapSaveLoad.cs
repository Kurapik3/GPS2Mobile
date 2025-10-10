using UnityEngine;
using System.IO;

public static class MapSaveLoad
{
    private static string GetPath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, fileName + ".json");
    }

    public static void Save(MapData mapData, string fileName)
    {
        string json = JsonUtility.ToJson(mapData, true);
        File.WriteAllText(GetPath(fileName), json);
        Debug.Log($"Map saved to {GetPath(fileName)}");
    }

    public static MapData Load(string fileName)
    {
        string path = GetPath(fileName);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"No saved map found at: {path}");
            return null;
        }

        string json = File.ReadAllText(path);
        MapData mapData = ScriptableObject.CreateInstance<MapData>();
        JsonUtility.FromJsonOverwrite(json, mapData);
        Debug.Log($"Map loaded from JSON: {path}");
        return mapData;
    }
}
