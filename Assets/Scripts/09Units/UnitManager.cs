using System.Collections.Generic;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance { get; private set; }

    private Dictionary<int, UnitBase> unitsById = new Dictionary<int, UnitBase>();
    private int nextUnitId = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public int RegisterUnit(UnitBase unit)
    {
        int id = nextUnitId++;
        unit.unitId = id;
        unitsById[id] = unit;

        Debug.Log($"Registered Unit ID {id} ({unit.unitName})");
        return id;
    }
    public void UnregisterUnit(int id)
    {
        if (unitsById.ContainsKey(id))
        {
            Debug.Log($"Unregistered Unit ID {id} ({unitsById[id].unitName})");
            unitsById.Remove(id);
        }
    }
    public UnitBase GetUnitById(int id)
    {
        unitsById.TryGetValue(id, out UnitBase unit);
        return unit;
    }
    public List<UnitBase> GetAllUnits()
    {
        return new List<UnitBase>(unitsById.Values);
    }
}
