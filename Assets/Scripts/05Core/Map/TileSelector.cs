using UnityEngine;

public class TileSelector : MonoBehaviour
{
    private static TileSelector instance;
    public static QuickOutlinePlugin.Outline PreviousOutline;
    public static void SelectTile(HexTile tile)
    {
        if(instance == null)
        {
            instance = Instantiate(Resources.Load<TileSelector>("TileSelector"));
        }
        instance.gameObject.SetActive(true);
        instance.transform.position = tile.transform.position + Vector3.up * 2.01f;
    }
    public static void Hide()
    {
        if(instance != null)
        {
            instance.gameObject.SetActive(false);
        }
    }
}

//TileSelector.SelectTile(clickedTile);
//TileSelector.Hide();
