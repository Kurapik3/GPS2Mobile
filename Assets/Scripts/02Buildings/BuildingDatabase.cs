using UnityEngine;
using System.Collections.Generic;

public class BuildingDatabase : MonoBehaviour
{
    [SerializeField] private string csvPath = "Data/buildings";
    private List<BuildingData> buildings = new List<BuildingData>();

    void Awake()
    {
        TextAsset csvFile = Resources.Load<TextAsset>(csvPath);
        if (csvFile == null)
        {
            Debug.LogError($"Building CSV not found at Resources/{csvPath}");
            return;
        }

        string[] lines = csvFile.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++) // skip header
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] values = line.Split(',');
            if (values.Length < 4)
            {
                Debug.LogWarning($"Skipping malformed line {i + 1}: {line}");
                continue;
            }

            for (int j = 0; j < values.Length; j++)
                values[j] = values[j].Trim();

            int health = ParseIntSafe(values[1], i, "health");
            int developCost = ParseIntSafe(values[2], i, "developCost");
            int apPerTurn = ParseIntSafe(values[3], i, "apPerTurn");

            BuildingData data = new BuildingData
            {
                buildingName = values[0],
                health = health,
                developCost = developCost,
                apPerTurn = apPerTurn
            };

            buildings.Add(data);
        }

        Debug.Log($"Loaded {buildings.Count} buildings from CSV.");
    }

    private int ParseIntSafe(string s, int lineIndex, string fieldName)
    {
        if (int.TryParse(s, out int result))
            return result;

        Debug.LogWarning($"[BuildingDatabase] Line {lineIndex + 1}: Failed to parse '{fieldName}' from value '{s}'. Defaulting to 0.");
        return 0;
    }

    public BuildingData GetBuildingByName(string name)
    {
        return buildings.Find(b => b.buildingName == name);
    }

    public List<BuildingData> GetAllBuildings() => buildings;
}
