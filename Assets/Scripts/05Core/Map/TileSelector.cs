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
        if (CurrentTile != null && CurrentTile != evt.Tile)
        {
            Hide();
        }
        CurrentTile = evt.Tile;
        gameObject.SetActive(true);
        transform.position = evt.Tile.transform.position + Vector3.up * 2.01f;
        GameObject target = CurrentTile.GetOutlineTarget();
        if (target != null)
        {
            QuickOutlinePlugin.Outline outline = target.GetComponent<QuickOutlinePlugin.Outline>();
            if (outline == null)
            {
                outline = target.AddComponent<QuickOutlinePlugin.Outline>();
                outline.OutlineMode = QuickOutlinePlugin.Outline.Mode.OutlineAll;
                outline.OutlineColor = Color.cyan;
                outline.OutlineWidth = 5f;
            }
            outline.enabled = true;
            PreviousOutline = outline;
        }

        Debug.Log("TileSelector RECEIVED event for tile: " + evt.Tile.name);
    }

    private void OnTileDeselected(TileDeselectedEvent evt)
    {
        if (CurrentTile != evt.Tile) return;
        if (PreviousOutline != null)
        {
            PreviousOutline.enabled = false;
            PreviousOutline = null;
        }
        CurrentTile = null;
        gameObject.SetActive(false);
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
}

//TileSelector.SelectTile(clickedTile);
//TileSelector.Hide();
