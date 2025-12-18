using System.Collections.Generic;
using TurfSystem;
using UnityEngine;

public class TurfHexOverlayRenderer : MonoBehaviour
{
    public enum TurfType { Player, Enemy }
    public TurfType turfType = TurfType.Player;

    [Header("Visual")]
    [Tooltip("Assign your TurfHighlight prefab (must have SetSelectionIndex(int))")]
    public GameObject turfHighlightPrefab;

    [Header("Offset")]
    public float heightOffset = 0.01f;

    private List<GameObject> activeTurfHighlights = new List<GameObject>();
    private HashSet<Vector2Int> ownedSet = new HashSet<Vector2Int>();

    // Axial directions for flat-top hex
    private static readonly Vector2Int[] Directions = new Vector2Int[]
    {
        new Vector2Int( 0, +1),  // 0: South
        new Vector2Int(-1, +1),  // 1: South-West
        new Vector2Int(-1,  0),  // 2: North-West
        new Vector2Int( 0, -1),  // 3: North
        new Vector2Int(+1, -1),  // 4: North-East
        new Vector2Int(+1,  0),  // 5: South-East
    };

    private void OnEnable()
    {
        TurfManager.OnTurfChanged += RefreshTurfVisual;
        EnemyTurfManager.OnEnemyTurfChanged += RefreshTurfVisual;
    }

    private void OnDisable()
    {
        TurfManager.OnTurfChanged -= RefreshTurfVisual;
        EnemyTurfManager.OnEnemyTurfChanged -= RefreshTurfVisual;
    }

    private void OnDestroy()
    {
        ClearHighlights();
    }

    public void RefreshTurfVisual()
    {
        ClearHighlights();
        ownedSet.Clear();

        if (turfType == TurfType.Player)
        {
            var tiles = TurfManager.Instance?.GetAllTurfTiles();
            if (tiles != null)
            {
                foreach (var tile in tiles)
                {
                    if (tile != null)
                    {
                        ownedSet.Add(new Vector2Int(tile.q, tile.r));
                    }
                }
            }
        }
        else
        {
            if (EnemyTurfManager.Instance != null)
            {
                foreach (var coord in EnemyTurfManager.Instance.GetAllEnemyTurfCoords())
                {
                    ownedSet.Add(coord);
                }
            }
        }

        if (ownedSet.Count == 0) return;

        // Spawn highlight on each owned tile
        foreach (var coord in ownedSet)
        {
            if (!MapManager.Instance.TryGetTile(coord, out HexTile tile))
            {
                continue;
            }
            GameObject highlight = Instantiate(turfHighlightPrefab, tile.transform);
            highlight.transform.localPosition = Vector3.up * heightOffset;
            activeTurfHighlights.Add(highlight);

            // Calculate edge mask
            int edgeMask = 0;
            for (int i = 0; i < 6; i++)
            {
                Vector2Int neighborCoord = coord + Directions[i];
                if (!ownedSet.Contains(neighborCoord))
                {
                    edgeMask |= (1 << i);
                }
            }

            if (highlight.TryGetComponent(out TurfEdgeVisual turfVisual))
            {
                turfVisual.SetEdgeMask(edgeMask);
            }
            else
            {
                Debug.LogWarning($"TurfHighlight prefab missing component: {highlight.name}");
            }
        }
    }

    private void ClearHighlights()
    {
        foreach (var obj in activeTurfHighlights)
        {
            if (obj != null) Destroy(obj);
        }
        activeTurfHighlights.Clear();
    }
}