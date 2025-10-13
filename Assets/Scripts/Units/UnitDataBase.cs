using UnityEngine;
using System.Collections.Generic;

public class UnitDatabase : MonoBehaviour
{
    [SerializeField] private string csvPath = "Data/units";
    private List<UnitData> units = new List<UnitData>();

    void Awake()
    {
        TextAsset csvFile = Resources.Load<TextAsset>(csvPath);
        if (csvFile == null)
        {
            Debug.LogError($"CSV not found at Resources/{csvPath}");
            return;
        }

        string[] lines = csvFile.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++) // skip header
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] values = line.Split(',');
            if (values.Length < 8)
            {
                Debug.LogWarning($"Skipping malformed CSV line {i + 1}: {line}");
                continue;
            }

            for (int j = 0; j < values.Length; j++)
                values[j] = values[j].Trim();

            int cost = ParseIntOr(values[1], 0);
            int range = ParseIntOr(values[2], 0);
            int movement = ParseIntOr(values[3], 0);
            int hp = ParseIntOr(values[4], 0);
            int atk = ParseIntOr(values[5], 0);
            bool isCombat = ParseBoolOr(values[6], false);
            string ability = values[7]; 

            UnitData data = new UnitData(values[0], cost, range, movement, hp, atk, isCombat, ability);
            units.Add(data);
        }
    }

    private int ParseIntOr(string s, int fallback)
    {
        if (int.TryParse(s, out int v)) return v;
        Debug.LogWarning($"Failed to parse int from '{s}', using {fallback}");
        return fallback;
    }

    private bool ParseBoolOr(string s, bool fallback)
    {
        if (bool.TryParse(s, out bool v)) return v;
        Debug.LogWarning($"Failed to parse bool from '{s}', using {fallback}");
        return fallback;
    }

    public UnitData GetUnitByName(string name)
    {
        return units.Find(u => u.unitName == name);
    }

    public List<UnitData> GetAllUnits() => units;
}
