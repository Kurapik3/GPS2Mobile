using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TurfHexOverlayRenderer : MonoBehaviour
{
    public enum TurfType { Player, Enemy }
    public TurfType turfType = TurfType.Player;

    [Header("Visual Settings")]
    [Range(0.001f, 0.1f)]
    public float heightOffset = 0.01f;

    [Header("Border")]
    public bool showBorder = true;
    [Range(0.02f, 0.15f)]
    public float borderWidth = 0.06f; // Width of the border stroke

    private Mesh overlayMesh;
    private MapGenerator mapGenerator;

    // Axial directions for flat-top hex (E, SE, SW, W, NW, NE)
    private static readonly Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(+1,  0), // 0: E
        new Vector2Int( 0, +1), // 1: SE
        new Vector2Int(-1, +1), // 2: SW
        new Vector2Int(-1,  0), // 3: W
        new Vector2Int( 0, -1), // 4: NW
        new Vector2Int(+1, -1)  // 5: NE
    };

    private void Awake()
    {
        overlayMesh = new Mesh();
        overlayMesh.name = $"{turfType}TurfBlob";
        GetComponent<MeshFilter>().mesh = overlayMesh;
        mapGenerator = FindFirstObjectByType<MapGenerator>();
    }

    private void OnEnable()
    {
        TurfManager.OnTurfChanged += RefreshOverlaySafe;
        EnemyTurfManager.OnEnemyTurfChanged += RefreshOverlaySafe;
    }

    private void OnDisable()
    {
        TurfManager.OnTurfChanged -= RefreshOverlaySafe;
        EnemyTurfManager.OnEnemyTurfChanged -= RefreshOverlaySafe;
    }

    private void RefreshOverlaySafe()
    {
        if (TurfManager.Instance == null || MapManager.Instance == null)
            return;
        if (FindFirstObjectByType<MapGenerator>() == null)
            return;
        RefreshOverlay();
    }

    public void RefreshOverlay()
    {
        HashSet<Vector2Int> ownedSet = GetOwnedSet();

        if (ownedSet.Count == 0)
        {
            overlayMesh.Clear();
            return;
        }

        // Generate a single mesh representing the entire territory blob
        GenerateBlobMesh(ownedSet);
    }

    private HashSet<Vector2Int> GetOwnedSet()
    {
        HashSet<Vector2Int> owned = new HashSet<Vector2Int>();
        if (turfType == TurfType.Player)
        {
            foreach (var tile in TurfManager.Instance.GetAllTurfTiles())
            {
                if (tile != null)
                    owned.Add(new Vector2Int(tile.q, tile.r));
            }
        }
        else
        {
            if (EnemyTurfManager.Instance != null)
            {
                foreach (var coord in EnemyTurfManager.Instance.GetAllEnemyTurfCoords())
                {
                    owned.Add(coord);
                }
            }
        }
        return owned;
    }

    private void GenerateBlobMesh(HashSet<Vector2Int> ownedSet)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();

        // Step 1: Get the outer contour as a list of 3D points (in CCW order)
        List<Vector3> outline = GetOuterContour(ownedSet);
        if (outline.Count < 3)
        {
            overlayMesh.Clear();
            return;
        }
        //outline.Reverse();
        if (showBorder)
        {
            // Create a hollow border loop
            Vector3 centroid = Vector3.zero;
            foreach (var p in outline) centroid += p;
            centroid /= outline.Count;

            AddBorder(vertices, triangles, normals, outline, centroid);
        }
        else
        {
            overlayMesh.Clear();
            return;
        }

        // Finalize mesh
        overlayMesh.Clear();
        overlayMesh.SetVertices(vertices);
        overlayMesh.SetTriangles(triangles, 0);
        overlayMesh.SetNormals(normals);
        overlayMesh.RecalculateBounds();
    }

    private List<Vector3> GetOuterContour(HashSet<Vector2Int> ownedSet)
    {
        if (ownedSet.Count == 0) return new List<Vector3>();

        // Find a starting exterior edge
        Vector2Int startHex = new Vector2Int();
        int startDir = -1;

        foreach (var hex in ownedSet)
        {
            for (int d = 0; d < 6; d++)
            {
                Vector2Int neighbor = hex + directions[d];
                if (!ownedSet.Contains(neighbor))
                {
                    startHex = hex;
                    startDir = d;
                    break;
                }
            }
            if (startDir != -1) break;
        }

        if (startDir == -1) return new List<Vector3>(); // No exterior (shouldn't happen)

        List<Vector3> contour = new List<Vector3>();
        Vector2Int currentHex = startHex;
        int currentDir = startDir;

        Vector3 firstCorner = GetHexCornerWorld(startHex, currentDir);
        contour.Add(firstCorner);

        do
        {
            // Move to next edge
            Vector2Int nextHex = currentHex + directions[currentDir];
            if (!ownedSet.Contains(nextHex))
            {
                // Turn left (CCW): (currentDir + 1) % 6
                currentDir = (currentDir + 1) % 6;
            }
            else
            {
                // Step forward and turn right (CW): (currentDir + 4) % 6 == (currentDir - 2)
                currentHex = nextHex;
                currentDir = (currentDir + 4) % 6; // = -2 mod 6
            }

            Vector3 corner = GetHexCornerWorld(currentHex, currentDir);
            contour.Add(corner);

        } while (!(currentHex == startHex && currentDir == startDir));

        // Remove last duplicate point
        if (contour.Count > 1) contour.RemoveAt(contour.Count - 1);

        return contour;
    }

    private Vector3 GetHexCornerWorld(Vector2Int hex, int cornerIndex)
    {
        // Get world position of hex center
        if (!MapManager.Instance.TryGetTile(hex, out HexTile tile))
        {
            // Fallback: compute position from axial coords
            float size = GetHexRadius();
            float x = size * (Mathf.Sqrt(3) * hex.x + Mathf.Sqrt(3) / 2 * hex.y);
            float z = size * (3f / 2f * hex.y);
            return new Vector3(x, 0, z);
        }
        Vector3 center = tile.transform.position;
        return GetHexCornerAtRadius(center, cornerIndex, GetHexRadius());
    }

    private Vector3 GetHexCornerAtRadius(Vector3 center, int cornerIndex, float radius)
    {
        float angleDeg = 60f * cornerIndex;
        float angleRad = angleDeg * Mathf.Deg2Rad;
        float x = Mathf.Cos(angleRad) * radius;
        float z = Mathf.Sin(angleRad) * radius;
        return center + new Vector3(x, 0, z);
    }

    private void AddBorder(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector3> normals,
        List<Vector3> outline,
        Vector3 centroid)
    {
        if (outline.Count < 3) return;

        float radius = GetHexRadius();
        float offset = borderWidth; // distance to push border outward

        // Create outer ring by moving each outline point outward from centroid
        List<int> outerIndices = new List<int>();
        Vector3 centroidFlat = new Vector3(centroid.x, 0, centroid.z);

        foreach (var innerPoint in outline)
        {
            Vector3 dir = (new Vector3(innerPoint.x, 0, innerPoint.z) - centroidFlat).normalized;
            if (dir.magnitude < 0.01f) dir = Vector3.forward; // fallback

            Vector3 outerPoint = innerPoint - new Vector3(dir.x * offset, 0, dir.z * offset);
            outerPoint.y = heightOffset + 0.001f; // slightly above fill

            vertices.Add(outerPoint);
            normals.Add(Vector3.up);
            outerIndices.Add(vertices.Count - 1);
        }

        // Add inner ring (original outline, y-offset for layering)
        List<int> innerIndices = new List<int>();
        foreach (var innerPoint in outline)
        {
            Vector3 p = innerPoint;
            p.y = heightOffset + 0.001f;
            vertices.Add(p);
            normals.Add(Vector3.up);
            innerIndices.Add(vertices.Count - 1);
        }

        // Create quads between outer and inner rings (CCW)
        for (int i = 0; i < outline.Count; i++)
        {
            int next = (i + 1) % outline.Count;

            int outerCurr = outerIndices[i];
            int outerNext = outerIndices[next];
            int innerNext = innerIndices[next];
            int innerCurr = innerIndices[i];

            // Triangle 1: outer[i] -> outer[next] -> inner[next]
            triangles.Add(outerCurr);
            triangles.Add(outerNext);
            triangles.Add(innerNext);

            // Triangle 2: outer[i] -> inner[next] -> inner[i]
            triangles.Add(outerCurr);
            triangles.Add(innerNext);
            triangles.Add(innerCurr);
        }
    }

    private float GetHexRadius()
    {
        if (mapGenerator == null)
        {
            mapGenerator = FindFirstObjectByType<MapGenerator>();
            if (mapGenerator == null) return 1f;
        }
        return mapGenerator.GetHexSize();
    }
}