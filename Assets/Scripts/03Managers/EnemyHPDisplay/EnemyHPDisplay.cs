using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHPDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private Image unitIconImage;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("Unit Icons")]
    [SerializeField] private Sprite scoutIcon;
    [SerializeField] private Sprite bomberIcon;
    [SerializeField] private Sprite builderIcon;
    [SerializeField] private Sprite tankerIcon;
    [SerializeField] private Sprite shooterIcon;
    [SerializeField] private Sprite defaultIcon;

    [Header("Settings")]
    [SerializeField] private Vector3 displayOffset = new Vector3(0, 0, 0);
    [SerializeField] private float canvasScale = 0.01f;

    private EnemyUnit enemyUnit;
    private Camera mainCamera;
    private Transform unitTransform;

    void Awake()
    {
        enemyUnit = GetComponentInParent<EnemyUnit>();
        unitTransform = enemyUnit != null ? enemyUnit.transform : transform.parent;
        mainCamera = Camera.main;
        SetupCanvas();
    }

    void Start()
    {
        if (enemyUnit != null)
        {
            SetUnitIcon(enemyUnit.unitType);
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
                canvasRect.sizeDelta = new Vector2(80, 20);
                canvasRect.localScale = new Vector3(canvasScale, canvasScale, canvasScale);
            }
        }
    }

    private void SetUnitIcon(string unitType)
    {
        if (unitIconImage == null) return;

        Sprite iconToUse = defaultIcon;

        // Match unit type to icon
        string lowerType = unitType.ToLower();

        if (lowerType.Contains("scout"))
            iconToUse = scoutIcon;
        else if (lowerType.Contains("bomber"))
            iconToUse = bomberIcon;
        else if (lowerType.Contains("builder"))
            iconToUse = builderIcon;
        else if (lowerType.Contains("tanker"))
            iconToUse = tankerIcon;
        else if (lowerType.Contains("shooter"))
            iconToUse = shooterIcon;

        unitIconImage.sprite = iconToUse != null ? iconToUse : defaultIcon;
    }

    public void UpdateHPDisplay()
    {
        if (enemyUnit == null || hpText == null) return;

        // Just show the current HP number
        hpText.text = enemyUnit.currentHP.ToString();
    }

    // Call this when enemy unit health changes
    public void OnHealthChanged()
    {
        UpdateHPDisplay();
    }
}
