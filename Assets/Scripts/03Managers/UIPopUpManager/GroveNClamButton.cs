using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GroveNClamButton : MonoBehaviour
{
    //[SerializeField] private GameObject harvestButton; // Assign in inspector
    //private HexTile cachedTile;

    //void Start()
    //{
    //    // Find the HexTile this resource sits on (should be parent or sibling)
    //    cachedTile = GetComponentInParent<HexTile>();
    //    if (cachedTile == null)
    //    {
    //        Debug.LogError("ResourceHarvestUI: No HexTile found in parent hierarchy!", this);
    //        return;
    //    }

    //    // Ensure button is hidden at start
    //    if (harvestButton != null)
    //        harvestButton.SetActive(false);
    //}

    //void Update()
    //{
    //    if (cachedTile == null) return;

    //    bool unitIsOnTile = cachedTile.currentUnit != null;
    //    bool shouldShow = unitIsOnTile;

    //    if (harvestButton != null && harvestButton.activeSelf != shouldShow)
    //    {
    //        harvestButton.SetActive(shouldShow);
    //    }
    //}

    [Header("UI References")]
    [SerializeField] private Canvas worldCanvas; // Assign this in inspector
    [SerializeField] private Image buttonIcon;   // Your button’s icon (e.g., clam/shell)
    [SerializeField] private TextMeshProUGUI buttonText; // Optional: e.g., "Harvest"

    [Header("Settings")]
    [SerializeField] private Sprite defaultIcon;     // Icon for Clam/Grove
    [SerializeField] private Vector3 displayOffset = new Vector3(0, 4.5f, 0); // Height above object
    [SerializeField] private float canvasScale = 0.01f; // Scale for World Space Canvas

    private HexTile cachedTile;
    private Camera mainCamera;
    private Transform resourceTransform;

    void Awake()
    {
        resourceTransform = transform;
        cachedTile = GetComponentInParent<HexTile>();
        if (cachedTile == null)
        {
            Debug.LogError("ResourceHarvestButton: No HexTile found in parent hierarchy!", this);
            return;
        }

        mainCamera = Camera.main;
        SetupCanvas();
    }

    void Start()
    {
        SetIcon();
        UpdateVisibility(); // Initial state
    }

    void LateUpdate()
    {
        if (worldCanvas != null && mainCamera != null && resourceTransform != null)
        {
            // Position canvas above the resource
            worldCanvas.transform.position = resourceTransform.position + displayOffset;

            // Always face camera
            worldCanvas.transform.rotation = Quaternion.LookRotation(
                worldCanvas.transform.position - mainCamera.transform.position,
                Vector3.up
            );
        }
    }

    void Update()
    {
        UpdateVisibility();
    }

    private void SetupCanvas()
    {
        if (worldCanvas == null)
        {
            Debug.LogError("ResourceHarvestButton: World Canvas not assigned!");
            return;
        }

        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.worldCamera = mainCamera;

        RectTransform rect = worldCanvas.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(100, 60); // Adjust as needed
            rect.localScale = new Vector3(canvasScale, canvasScale, canvasScale);
        }
    }

    private void SetIcon()
    {
        if (buttonIcon != null && defaultIcon != null)
            buttonIcon.sprite = defaultIcon;
    }

    private void UpdateVisibility()
    {
        bool shouldShow = cachedTile.currentUnit != null;

        if (worldCanvas != null)
        {
            worldCanvas.gameObject.SetActive(shouldShow);
        }
    }

    // Optional: Add click handler
    public void OnHarvestClicked()
    {
        Debug.Log($"Harvesting from {name} at ({cachedTile.q}, {cachedTile.r})");

        // TODO: Add your harvesting logic here
        // e.g., remove resource, give resources to player, play sound, etc.

        // Example: Destroy this resource after harvest
        Destroy(gameObject);
    }
}

