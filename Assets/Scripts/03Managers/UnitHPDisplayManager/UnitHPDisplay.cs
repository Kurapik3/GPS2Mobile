using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitHPDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private Image unitIconImage;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("Unit Icons")]
    [SerializeField] private Sprite scoutIcon;
    [SerializeField] private Sprite bomberIcon;
    [SerializeField] private Sprite builderIcon;
    [SerializeField] private Sprite defaultIcon;

    [Header("Settings")]
    [SerializeField] private Vector3 displayOffset = new Vector3(0, 0, 0);
    [SerializeField] private float canvasScale = 0.01f;

    private UnitBase unit;
    private Camera mainCamera;
    private Transform unitTransform;

    void Awake()
    {
        unit = GetComponentInParent<UnitBase>();
        unitTransform = unit != null ? unit.transform : transform.parent;
        mainCamera = Camera.main;
        SetupCanvas();
    }

    void Start()
    {
        if (unit != null)
        {
            SetUnitIcon(unit.unitName);
            UpdateHPDisplay();
        }
    }

    void LateUpdate()
    {
        // Make canvas face camera
        if (worldCanvas != null && mainCamera != null && unitTransform != null)
        {
            worldCanvas.transform.position = unitTransform.position + displayOffset;
            worldCanvas.transform.LookAt(worldCanvas.transform.position + mainCamera.transform.forward, Vector3.up);
        }
    }

    private void SetupCanvas()
    {
        if (worldCanvas != null)
        {
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.worldCamera = mainCamera;

            RectTransform canvasRect = worldCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.sizeDelta = new Vector2(100, 20);
                canvasRect.localScale = new Vector3(canvasScale, canvasScale, canvasScale);
            }
        }
    }

    private void SetUnitIcon(string unitName)
    {
        if (unitIconImage == null) return;

        Sprite iconToUse = defaultIcon;

        // Match unit name to icon
        string lowerName = unitName.ToLower();

        if (lowerName.Contains("scout"))
            iconToUse = scoutIcon;
        else if (lowerName.Contains("bomber"))
            iconToUse = bomberIcon;
        else if (lowerName.Contains("builder"))
            iconToUse = builderIcon;

        unitIconImage.sprite = iconToUse != null ? iconToUse : defaultIcon;
    }

    public void UpdateHPDisplay()
    {
        if (unit == null || hpText == null) return;

        // Just show the current HP number
        hpText.text = unit.hp.ToString();
    }
}
