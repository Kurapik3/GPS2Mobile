using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "TileGen/Structure Database")]
public class StructureDatabase : ScriptableObject
{
    public List<StructureData> structures = new();
    public StructureData GetByName(string name)
    {
        return structures.Find(s => s.structureName == name);
    }

    public string[] GetAllNames()
    {
        string[] names = new string[structures.Count];
        for (int i = 0; i < structures.Count; i++)
        {
            names[i] = structures[i].structureName;
        }
        return names;
    }
}
