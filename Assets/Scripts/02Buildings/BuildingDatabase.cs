using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingDatabase", menuName = "Scriptable Objects/BuildingDatabase")]
public class BuildingDatabase : ScriptableObject
{
    [SerializeField] private TextAsset csvFile;
    private List<BuildingData> buildingList = new List<BuildingData>();

    public List<BuildingData> GetAllBuildings() => buildingList;

    [ContextMenu("Load Buildings From CSV")]
    public void LoadFromCSV()
    {
        buildingList.Clear();
        if (csvFile == null)
        {
            Debug.LogError("No CSV file assigned!");
            return;
        }

        string[] lines = csvFile.text.Split('\n');
        for (int i = 1; i < lines.Length; i++) // skip header
        {
            string[] values = lines[i].Split(',');
            if (values.Length < 4) continue;

            BuildingData data = new BuildingData
            {
                buildingName = values[0].Trim(),
                health = int.Parse(values[1]),
                developCost = int.Parse(values[2]),
                apPerTurn = int.Parse(values[3])
            };

            buildingList.Add(data);
        }

        Debug.Log($"Loaded {buildingList.Count} buildings from CSV");
    }
}
