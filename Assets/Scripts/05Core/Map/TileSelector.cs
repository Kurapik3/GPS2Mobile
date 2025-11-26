using UnityEngine;

public class TileSelector : MonoBehaviour
{
    private static TileSelector instance;
    public static HexTile CurrentTile { get; private set; }
    public static QuickOutlinePlugin.Outline PreviousOutline;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        
    }
    private void OnEnable()
    {
        Debug.Log("TileSelector OnEnable Fired");
        EventBus.Subscribe<TileSelectedEvent>(OnTileSelected);
        EventBus.Subscribe<TileDeselectedEvent>(OnTileDeselected);
        Debug.Log("TileSelector Enabled — Subscribing to events.");
    }
    private void OnDisable()
    {
        EventBus.Unsubscribe<TileSelectedEvent>(OnTileSelected);
        EventBus.Unsubscribe<TileDeselectedEvent>(OnTileDeselected);
    }
    
    private void OnTileSelected(TileSelectedEvent evt)
    {
        if (evt.Tile == null)
        {
            return;
        }
        CurrentTile = evt.Tile;
        gameObject.SetActive(true);
        transform.position = evt.Tile.transform.position + Vector3.up * 2.01f;
        Debug.Log("TileSelector RECEIVED event for tile: " + evt.Tile.name);
    }

    private void OnTileDeselected(TileDeselectedEvent evt)
    {
        if (CurrentTile == evt.Tile)
        {
            CurrentTile = null;
            gameObject.SetActive(false);
        }
    }
    public static void SelectTile(HexTile tile)
    {
        if (instance == null)
        {
            instance = Instantiate(Resources.Load<TileSelector>("TileSelector"));
        }

        if (!instance.gameObject.activeSelf)
        {
            instance.gameObject.SetActive(true);
        }

        EventBus.Publish(new TileSelectedEvent(tile));
    }

    public static void Hide()
    {
        if (CurrentTile != null)
        {
            EventBus.Publish(new TileDeselectedEvent(CurrentTile));
        }
    }
    //public static void SelectTile(HexTile tile)
    //{
    //    CurrentTile = tile;
    //    if (instance == null)
    //    {
    //        instance = Instantiate(Resources.Load<TileSelector>("TileSelector"));
    //    }
    //    instance.gameObject.SetActive(true);
    //    instance.transform.position = tile.transform.position + Vector3.up * 2.01f;
    //}
    //public static void Hide()
    //{
    //    if(instance != null)
    //    {
    //        instance.gameObject.SetActive(false);
    //    }
    //    CurrentTile = null;
    //}
}

//TileSelector.SelectTile(clickedTile);
//TileSelector.Hide();
