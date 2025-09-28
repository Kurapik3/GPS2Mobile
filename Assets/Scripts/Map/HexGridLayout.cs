using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
public class HexGridLayout : MonoBehaviour
{
    /*
    [Header("Grid Settings")]
    public Vector2Int gridSize;
    public float radius = 1f;

    //[Header("Tile Settings")]
    //public float outerSize = 1f;
    //public float innerSize = 0f;
    //public float height = 1f;
    public bool isFlatTopped;

    public HexTileGenerationSettings settings;

    //public Material material;
    public void Clear()
    {
        List<GameObject> children = new List<GameObject>();
        for(int i=0; i<transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            children.Add(child);
        }
        foreach(GameObject child in children)
        {
            DestroyImmediate(child, true); 
        }
    }
    private void OnEnable()
    {
        LayoutGrid();
    }

    public void OnValidate()//Delete after?
    {
        LayoutGrid();
    }
    private void LayoutGrid()
    {
        Clear();

        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                GameObject tile = new GameObject($"Hex C{x},R{y}");
                HexTile hextile = tile.AddComponent<HexTile>();
                hextile.settings = settings;
                hextile.RollTileType();
                hextile.AddTile();

                //Assign its offset coordinates for human parsing (Col,Row)
                hextile.offsetCoordinate = new Vector2Int(x, y);

                //Assign/convert these to cube coordinates for navigation
                hextile.cubeCoordinate = OffsetToCube(hextile.offsetCoordinate);

                //tile.transform.position =GetPositionForHexFromCoordinate(new Vector2Int(x, y));

                //HexRenderer hexRenderer = tile.GetComponent<HexRenderer>();
                //hexRenderer.isFlatTopped = isFlatTopped;
                //hexRenderer.outerSize = outerSize;
                //hexRenderer.innerSize = innerSize;
                //hexRenderer.height = height;
                //hexRenderer.SetMaterial(material);
                //hexRenderer.DrawMesh();

                //tile.transform.SetParent(transform, true);
            }
        }
    }

    public Vector3 GetPositionForHexFromCoordinate(Vector2Int coordinate)
    {
        int col = coordinate.x;
        int row = coordinate.y;
        float width;
        float height;
        float xPosition;
        float yPosition;
        bool shouldOffset;
        float horizontalDistance;
        float verticalDistance;
        float offset;
        float size = 0;//outerSize;
        if(!isFlatTopped)
        {
            shouldOffset = (row % 2) == 0;
            width = Mathf.Sqrt(3) * size;
            height = 2f * size;
            horizontalDistance = width;
            verticalDistance = height * (3f / 4f);
            offset = (shouldOffset) ? width / 2 : 0;

            xPosition = (col * (horizontalDistance)) + offset;
            yPosition = (row * verticalDistance);
         }
        else
        {
            shouldOffset = (col % 2) == 0;
            width = 2f * size;
            height = Mathf.Sqrt(3) * size;
            horizontalDistance = width * (3f / 4f);
            verticalDistance = height;
            offset = (shouldOffset) ? height / 2 : 0;

            xPosition = (col * (horizontalDistance));
            yPosition = (row * verticalDistance) - offset;
        }
        return new Vector3(xPosition, 0, -yPosition);
    }

    public static Vector3Int OffsetToCube(Vector2Int offset)
    {
        var q = offset.x - (offset.y + (offset.y % 2)) / 2;
        var r = offset.y;
        return new Vector3Int(q, r, -q - r);
    }
    */
}
