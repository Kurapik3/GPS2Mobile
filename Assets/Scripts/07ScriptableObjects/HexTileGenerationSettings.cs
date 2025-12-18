using UnityEngine;
[CreateAssetMenu(menuName = "TileGen/GenerationSettings")]
public class HexTileGenerationSettings : ScriptableObject
{
    [SerializeField] private GameObject normalPrefab;
    [SerializeField] private GameObject structurePrefab;

    public GameObject GetTile(HexTile.TileType tileType)
    {
        switch (tileType)
        {
            case HexTile.TileType.Normal:
                return normalPrefab;
            case HexTile.TileType.Structure:
                return structurePrefab;
        }
        return null;
    }
}
