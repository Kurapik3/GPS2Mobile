using System.Collections.Generic;
using UnityEngine;

public class PlayerEntityRegistry : MonoBehaviour
{
    public static PlayerEntityRegistry Instance { get; private set; }

    private Dictionary<int, Vector2Int> playerUnitPositions = new();
    private Dictionary<int, Vector2Int> playerBasePositions = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }


    public void RegisterPlayerUnit(GameObject go)
    {
        int id = go.GetInstanceID();
        Vector2Int hex = MapManager.Instance.WorldToHex(go.transform.position);
        playerUnitPositions[id] = hex;
    }

    public void RegisterPlayerBase(GameObject go)
    {
        int id = go.GetInstanceID();
        Vector2Int hex = MapManager.Instance.WorldToHex(go.transform.position);
        playerBasePositions[id] = hex;
    }

    public void Unregister(GameObject go)
    {
        int id = go.GetInstanceID();
        playerUnitPositions.Remove(id);
        playerBasePositions.Remove(id);
    }

    public Vector2Int GetEntityPosition(int id)
    {
        if (playerUnitPositions.TryGetValue(id, out var unitHex))
            return unitHex;
        if (playerBasePositions.TryGetValue(id, out var baseHex))
            return baseHex;
        return Vector2Int.zero;
    }

    public List<int> GetAllPlayerUnitIds() => new(playerUnitPositions.Keys);
    public List<int> GetAllPlayerBaseIds() => new(playerBasePositions.Keys);

    // fallback: rebuild cache via tags (optional, when no managers exist)
    public void RefreshFromScene()
    {
        playerUnitPositions.Clear();
        playerBasePositions.Clear();

        foreach (var go in GameObject.FindGameObjectsWithTag("PlayerUnit"))
            RegisterPlayerUnit(go);

        foreach (var go in GameObject.FindGameObjectsWithTag("PlayerBase"))
            RegisterPlayerBase(go);
    }
}
