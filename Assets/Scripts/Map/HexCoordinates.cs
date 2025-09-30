using UnityEngine;

public static class HexCoordinates
{
    // Axial (q, r) to World (pointy-top)
    public static Vector3 ToWorld(int q, int r, float size)
    {
        float x = size * (Mathf.Sqrt(3) * q + Mathf.Sqrt(3) / 2f * r);
        float z = size * (3f / 2f * r);
        return new Vector3(x, 0, z);
    }

    // Axial distance (for hex-shaped chunks/maps)
    public static int Distance(int q1, int r1, int q2, int r2)
    {
        int dq = q1 - q2;
        int dr = r1 - r2;
        return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(dq + dr)) / 2;
    }

    // Axial directions (for neighbors)
    public static readonly Vector2Int[] Directions = new Vector2Int[]
    {
        new(1, 0), new(1, -1), new(0, -1),
        new(-1, 0), new(-1, 1), new(0, 1)
    };
}
