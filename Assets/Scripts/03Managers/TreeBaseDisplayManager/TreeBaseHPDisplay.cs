using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

public class TreeBaseHPDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private Image baseIconImage;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("Progress Bar")]
    [SerializeField] private TreeBaseLevelProgressUI levelProgress;

    [Header("Popup Reference")]
    [SerializeField] public TreeBaseUpgradeProgressUI upgradePopup;

    [Header("Icon Settings")]
    [SerializeField] private Sprite treeBaseIcon;

    [Header("Settings")]
    [SerializeField] private Vector3 displayOffset = new Vector3(0, 4.5f, 0);
    [SerializeField] private float canvasScale = 0.01f;

    private TreeBase treeBase;
    private Camera mainCamera;
    private Transform baseTransform;

    void Awake()
    {
        treeBase = FindObjectOfType<TreeBase>();
        if (treeBase == null)
        {
            Debug.LogError("TreeBaseHPDisplay: Could not find TreeBase in scene!");
            return;
        }

        GameObject popupObj = GameObject.FindGameObjectWithTag("BaseUpgradePopup");
        if (popupObj != null)
        {
            upgradePopup = popupObj.GetComponent<TreeBaseUpgradeProgressUI>();
        }
        else
        {
            Debug.LogWarning("TreeBaseHPDisplay: Upgrade popup not found in scene (tag 'BaseUpgradePopup' missing).");
        }

        baseTransform = treeBase.transform;
        mainCamera = Camera.main;
        SetupCanvas();
    }

    void Start()
    {
        SetBaseIcon();
        UpdateHPDisplay();
        UpdateProgress();
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
        if (baseIconImage != null && treeBaseIcon != null)
            baseIconImage.sprite = treeBaseIcon;
    }

    public void UpdateHPDisplay()
    {
        if (treeBase == null || hpText == null) return;
        hpText.text = $"{treeBase.health}";
    }

    public void OnHealthChanged()
    {
        UpdateHPDisplay();
    }

    public void OnLevelChanged()
    {
        Debug.Log("[TreeBaseHPDisplay] OnLevelChanged called");
        UpdateHPDisplay();
        UpdateProgress();
    }

    public void OnPopulationChanged()
    {
        Debug.Log($"[TreeBaseHPDisplay] OnPopulationChanged called for TreeBase ID: {treeBase?.TreeBaseId}");
        UpdateProgress();
    }

    private void UpdateProgress()
    {
        if (levelProgress != null)
        {
            levelProgress.UpdateProgress();
        }
        else
        {
            Debug.LogWarning("[TreeBaseHPDisplay] levelProgress is null!");
        }
    }

    public void ShowUpgradePopup(int upgradedLevel)
    {
        //if (upgradePopup != null)
        //{
        //    upgradePopup.ShowPopup(treeBase.level + 1);
        //}\
        if (upgradePopup != null)
        {
            upgradePopup.ShowPopup(upgradedLevel);
        }
    }
}

