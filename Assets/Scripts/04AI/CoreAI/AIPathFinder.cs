using System.Collections.Generic;
using UnityEngine;

public static class AIPathFinder
{
    //Calculate hex distance using axial or offset coordinates
    public static int GetHexDistance(Vector2Int a, Vector2Int b)
    {
        return MapManager.Instance.GetHexDistance(a, b);
    }

    //Returns all hex tiles that can be reached given movement range
    public static List<Vector2Int> GetReachableHexes(Vector2Int startHex, int moveRange)
    {
        List<Vector2Int> reachable = new();

        for (int dx = -moveRange; dx <= moveRange; dx++)
        {
            for (int dy = Mathf.Max(-moveRange, -dx - moveRange); dy <= Mathf.Min(moveRange, -dx + moveRange); dy++)
            {
                Vector2Int hex = new(startHex.x + dx, startHex.y + dy);

                if (!MapManager.Instance.IsWalkable(hex))
                    continue;

                if (MapManager.Instance.IsTileOccupied(hex))
                    continue;

                reachable.Add(hex);
            }
        }

        return reachable;
    }

    //Find nearest reachable hex towards target
    public static Vector2Int? FindNearestReachable(Vector2Int start, Vector2Int target, int moveRange)
    {
        var reachable = GetReachableHexes(start, moveRange);
        Vector2Int? best = null;
        int bestDist = int.MaxValue;

        foreach (var hex in reachable)
        {
            int dist = GetHexDistance(hex, target);
            if (dist < bestDist)
            {
                best = hex;
                bestDist = dist;
            }
        }

        return best;
    }

    public static HexTile GetRandomReachableTileForSeaMonster(SeaMonsterBase monster)
    {
        if (monster.CurrentTile == null)
            return null;

        var reachable = GetReachableHexes(monster.CurrentTile.HexCoords, monster.MovementRange);
        if (reachable == null || reachable.Count == 0)
            return null;

        Vector2Int choice = reachable[Random.Range(0, reachable.Count)];
        return MapManager.Instance.GetTile(choice);
    }
}
