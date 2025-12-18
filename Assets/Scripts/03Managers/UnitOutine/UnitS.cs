using System;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using QuickOutlinePlugin;

public class UnitS : MonoBehaviour
{
    public static UnitS instance;

    public List<GameObject> allUnitList = new List<GameObject>();
    public List<GameObject> unitsSelected = new List<GameObject>();

    [Header("Input Settings")]
    [SerializeField] private LayerMask clickable;
    [SerializeField] private float tapTimeThreshold = 0.3f;
    [SerializeField] private float tapDistanceThreshold = 50f;

    [Header("UI Animation")]
    [SerializeField] private RectTransform unitInfoPanelMove;
    [SerializeField] private RectTransform unitStatusWindowMove;
    [SerializeField] private Ease easing;
    [SerializeField] private float moveDuration = 1f;

    [Header("UI Positions")]
    [SerializeField] private Vector2 centrePos = new Vector2(0, 200);
    [SerializeField] private Vector2 bottomPos = new Vector2(0, 200);
    [SerializeField] private Vector2 offScreenPos = new Vector2(0, -300);

    [Header("UI Elements")]
    [SerializeField] private CanvasGroup infoBar;
    [SerializeField] private CanvasGroup panel;

    [Header("Selection Indicator")]
    [SerializeField] private string selectionIndicatorTag = "SelectionIndicator"; // Tag for the indicator child

    private Camera cam;
    private bool isStatusClosed = false;
    private bool isSFXPlayed = true;
    public static bool IsUIBlockingInput { get; set; } = false;
    private bool handledByThisManager = false;
    // Touch tracking
    private Vector2 touchStartPos;
    private float touchStartTime;

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
        EnhancedTouchSupport.Enable();
        Touch.onFingerDown += OnFingerDown;
        Touch.onFingerUp += OnFingerUp;
    }

    private void OnDisable()
    {
        Touch.onFingerDown -= OnFingerDown;
        Touch.onFingerUp -= OnFingerUp;
        EnhancedTouchSupport.Disable();
    }

    private void Start()
    {
        cam = Camera.main;

        // Set initial positions off-screen
        unitInfoPanelMove.anchoredPosition = offScreenPos;
        unitStatusWindowMove.anchoredPosition = offScreenPos;

        Application.targetFrameRate = 60;
    }

    private void OnFingerDown(Finger finger)
    {
        if (finger.index != 0) return;

        touchStartPos = finger.screenPosition;
        touchStartTime = Time.time;
    }

    private void OnFingerUp(Finger finger)
    {
        if (finger.index != 0) return;

        float touchDuration = Time.time - touchStartTime;
        float touchDistance = Vector2.Distance(touchStartPos, finger.screenPosition);

        if (touchDuration > tapTimeThreshold || touchDistance > tapDistanceThreshold)
        {
            return;
        }

        Vector2 touchPosition = finger.screenPosition;

        if (IsPointerOverUI(touchPosition))
        {
            return;
        }

        Ray ray = cam.ScreenPointToRay(touchPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickable))
        {
            handledByThisManager = true;
            SelectByClicking(hit.collider.gameObject);
            UnitInfoPanelMove();

            UnitBase unitBase = hit.collider.GetComponentInParent<UnitBase>();
            if (unitBase != null && unitBase.currentTile != null)
            {
                unitBase.currentTile.OnTileClicked();
            }

            if (isSFXPlayed)
            {
                ManagerAudio.instance.PlaySFX("UnitSelected");
                isSFXPlayed = false;
            }
        }
        else
        {
            if (!handledByThisManager) return;
            handledByThisManager = false;
            isSFXPlayed = true;
            DeselectAll();
            CloseUnitInfoPanel();
            if(TileSelector.CurrentTile != null)
            {
                TileSelector.Hide();
            }
        }
    }

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        return results.Count > 0;
    }

    private void DeselectAll()
    {
        foreach (var unit in unitsSelected)
        {
            if (unit != null)
            {
                TriggerSelectionIndicator(unit, false);
            }
        }
        unitsSelected.Clear();
    }

    private void SelectByClicking(GameObject unit)
    {
        foreach (var u in unitsSelected)
        {
            QuickOutlinePlugin.Outline oldOutline = u.GetComponentInParent<QuickOutlinePlugin.Outline>();
            if (oldOutline != null) oldOutline.enabled = false;
        }
        DeselectAll();
        unitsSelected.Add(unit);
        TriggerSelectionIndicator(unit, true);
    }

    private void TriggerSelectionIndicator(GameObject unit, bool isVisible)
    {
        if (unit == null) return;

        Transform indicator = unit.transform.Find("SelectionIndicator");
        if (indicator != null)
        {
            indicator.gameObject.SetActive(isVisible);
            return;
        }

        foreach (Transform child in unit.transform)
        {
            if (child.CompareTag(selectionIndicatorTag))
            {
                child.gameObject.SetActive(isVisible);
                return;
            }
        }

        string[] indicatorNames = { "SelectionIndicator", "Indicator", "Selection", "Ring" };
        foreach (string name in indicatorNames)
        {
            Transform found = unit.transform.Find(name);
            if (found != null)
            {
                found.gameObject.SetActive(isVisible);
                return;
            }
        }

        Debug.LogWarning($"No selection indicator found on {unit.name}. Add a child named 'SelectionIndicator' or tag it with '{selectionIndicatorTag}'");
    }

    private void UnitInfoPanelMove()
    {
        unitInfoPanelMove.DOAnchorPos(bottomPos, moveDuration).SetEase(Ease.OutBack);
    }

    private void UnitStatusWindowMove()
    {
        unitStatusWindowMove.DOAnchorPos(centrePos, moveDuration).SetEase(Ease.OutBack);
    }

    private void CloseUnitStatusWindow()
    {
        unitStatusWindowMove.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.OutBack);
    }

    private void CloseUnitInfoPanel()
    {
        unitInfoPanelMove.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.InBack);
    }

    public void OpenStatusWindow()
    {
        isStatusClosed = false;
        infoBar.interactable = false;
        panel.blocksRaycasts = true;
        UnitStatusWindowMove();

        if (unitsSelected.Count > 0 && unitsSelected[0] != null)
        {
            UnitBase unitBase = unitsSelected[0].GetComponent<UnitBase>();
            if (unitBase != null && UnitStatusPopup.Instance != null)
            {
                UnitStatusPopup.Instance.ShowPopup(unitBase);
            }
        }
    }

    public void CloseStats()
    {
        infoBar.interactable = true;
        CloseUnitStatusWindow();
        isStatusClosed = true;
        IsUIBlockingInput = false;
        panel.blocksRaycasts = false;

        if (UnitStatusPopup.Instance != null)
        {
            UnitStatusPopup.Instance.HidePopup();
        }
    }

    private void OnDestroy()
    {
        DeselectAll();
    }

    public void SelectMultiple(GameObject unit)
    {
        if (unitsSelected.Contains(unit))
        {
            unitsSelected.Remove(unit);
            TriggerSelectionIndicator(unit, false);
        }
        else
        {
            unitsSelected.Add(unit);
            TriggerSelectionIndicator(unit, true);
        }
    }

    public GameObject GetSelectedUnit()
    {
        if (unitsSelected.Count > 0)
            return unitsSelected[0];
        return null;
    }
}

