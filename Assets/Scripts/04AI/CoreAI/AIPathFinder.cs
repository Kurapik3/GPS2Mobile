using System.Collections.Generic;
using UnityEngine;

public static class AIPathFinder
{
    public static List<Vector2Int> GetPath(Vector2Int start, Vector2Int goal)
    {
        var open = new List<Vector2Int> { start };
        var closed = new HashSet<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        var gScore = new Dictionary<Vector2Int, int> { [start] = 0 };
        var fScore = new Dictionary<Vector2Int, int> { [start] = Heuristic(start, goal) };

        while (open.Count > 0)
        {
            //Find the node in open with the lowest fScore
            Vector2Int current = open[0];
            foreach (var tile in open)
            {
                if (fScore.TryGetValue(tile, out int fs) && fScore.TryGetValue(current, out int fc) && fs < fc)
                    current = tile;
            }

            //Found goal
            if (current == goal)
                return ReconstructPath(cameFrom, current);

            open.Remove(current);
            closed.Add(current);

            foreach (var dir in HexCoordinates.Directions)
            {
                Vector2Int neighbor = current + dir;

                if (closed.Contains(neighbor))
                    continue;

                //Can't move here
                if (!MapManager.Instance.CanUnitStandHere(neighbor))
                    continue;

                //Occupied tile but not the goal
                if (MapManager.Instance.IsTileOccupied(neighbor) && neighbor != goal)
                    continue;

                int tentativeG = gScore[current] + 1;

                if (!gScore.TryGetValue(neighbor, out int oldG) || tentativeG < oldG)
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;

                    int fs = tentativeG + Heuristic(neighbor, goal);
                    fScore[neighbor] = fs;

                    if (!open.Contains(neighbor))
                        open.Add(neighbor);
                }
            }
        }

        return new List<Vector2Int>(); //No path
    }


    private static int Heuristic(Vector2Int a, Vector2Int b)
    {
        return HexCoordinates.Distance(a.x, a.y, b.x, b.y);
    }

    private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        List<Vector2Int> path = new();
        path.Add(current);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    //Calculate hex distance using axial or offset coordinates
    public static int GetHexDistance(Vector2Int a, Vector2Int b)
    {
        return MapManager.Instance.GetHexDistance(a, b);
    }

    //Returns all hex tiles that can be reached given movement range
    public static List<Vector2Int> GetReachableHexes(Vector2Int startHex, int moveRange, Vector2Int? target = null)
    {
        List<Vector2Int> reachable = new();

        for (int dx = -moveRange; dx <= moveRange; dx++)
        {
            for (int dy = Mathf.Max(-moveRange, -dx - moveRange); dy <= Mathf.Min(moveRange, -dx + moveRange); dy++)
            {
                Vector2Int hex = new(startHex.x + dx, startHex.y + dy);
                bool isTarget = target.HasValue && hex == target.Value;

                if (!MapManager.Instance.IsWalkable(hex) && !isTarget)
                    continue;

                if (MapManager.Instance.IsTileOccupied(hex) && !isTarget)
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
        if (monster.currentTile == null)
            return null;

        var reachable = GetReachableHexes(monster.currentTile.HexCoords, monster.movementRange);
        if (reachable == null || reachable.Count == 0)
            return null;

        Vector2Int choice = reachable[Random.Range(0, reachable.Count)];
        return MapManager.Instance.GetTile(choice);
    }


    public static List<Vector2Int> GetSmartMoves(Vector2Int from, Vector2Int target, int moveRange = 1)
    {
        List<Vector2Int> candidates = GetReachableHexes(from, moveRange, target);
        int currentDist = GetHexDistance(from, target);

        List<Vector2Int> smartMoves = new();
        foreach (var hex in candidates)
        {
            int distToTarget = GetHexDistance(hex, target);
            if (distToTarget <= currentDist)
            {
                smartMoves.Add(hex);
            }
        }

        if (smartMoves.Count == 0)
        {
            if (candidates.Contains(from) && MapManager.Instance.CanUnitStandHere(from) && !MapManager.Instance.GetTileAtHexPosition(target).IsBlockedByTurtleWall)
                smartMoves.Add(from);
            else if (candidates.Count > 0)
                smartMoves = candidates;
        }

        return smartMoves;
    }

    public static Vector2Int RandomChoice(List<Vector2Int> list)
    {
        if (list == null || list.Count == 0)
            return Vector2Int.zero;

        return list[Random.Range(0, list.Count)];
    }

    public static Vector2Int? TryMove(Vector2Int from, Vector2Int target, int moveRange = 1)
    {
        var options = GetSmartMoves(from, target, moveRange);
        if (options.Count == 0)
            return null;

        var shuffle = new List<Vector2Int>(options);

        System.Random rng = new System.Random();
        int num = shuffle.Count;
        while (num > 1)
        {
            num--;
            int k = rng.Next(num + 1);
            (shuffle[k], shuffle[num]) = (shuffle[num], shuffle[k]);
        }

        return shuffle[Random.Range(0, shuffle.Count)];
    }
}
