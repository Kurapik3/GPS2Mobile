using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyBaseHPDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private Image baseIconImage;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("Icon Settings")]
    [SerializeField] private Sprite enemyBaseIcon;

    [Header("Settings")]
    [SerializeField] private Vector3 displayOffset = new Vector3(0, 4.5f, 0);
    [SerializeField] private float canvasScale = 0.01f;

    private EnemyBase enemyBase;
    private Camera mainCamera;
    private Transform baseTransform;

    void Awake()
    {
        enemyBase = GetComponentInParent<EnemyBase>();
        if (enemyBase == null)
        {
            Debug.LogError("EnemyBaseHPDisplay: Could not find EnemyBase on or above this object!");
            return;
        }

        else
        {
            Debug.LogWarning("EnemyBaseHPDisplay: Upgrade popup not found (tag 'BaseUpgradePopup' missing).");
        }

        baseTransform = enemyBase.transform;
        mainCamera = Camera.main;
        SetupCanvas();
    }

    void Start()
    {
        SetBaseIcon();
        UpdateHPDisplay();
    }

    void LateUpdate()
    {
        if (worldCanvas != null && mainCamera != null && baseTransform != null)
        {
            worldCanvas.transform.position = baseTransform.position + displayOffset;
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
                canvasRect.sizeDelta = new Vector2(100, 600);
                canvasRect.localScale = new Vector3(canvasScale, canvasScale, canvasScale);
            }
        }
    }

    private void SetBaseIcon()
    {
        if (baseIconImage != null && enemyBaseIcon != null)
            baseIconImage.sprite = enemyBaseIcon;
    }

    public void UpdateHPDisplay()
    {
        if (enemyBase == null || hpText == null) return;
        hpText.text = $"{enemyBase.health}";
    }

    public void OnHealthChanged()
    {
        UpdateHPDisplay();
    }

    public void OnLevelChanged()
    {
        Debug.Log("[EnemyBaseHPDisplay] OnLevelChanged called");
        UpdateHPDisplay();
    }

}
