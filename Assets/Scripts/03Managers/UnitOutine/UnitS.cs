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

public class UnitS : MonoBehaviour
{

    //    public static UnitS instance;

    //    public List<GameObject> allUnitList = new List<GameObject>();
    //    public List<GameObject> unitsSelected = new List<GameObject>();

    //    [Header("Input Settings")]
    //    [SerializeField] private LayerMask clickable;
    //    [SerializeField] private float tapTimeThreshold = 0.3f; // Max time for a tap
    //    [SerializeField] private float tapDistanceThreshold = 50f; // Max movement for a tap

    //    [Header("UI Animation")]
    //    [SerializeField] private RectTransform unitInfoPanelMove;
    //    [SerializeField] private RectTransform unitStatusWindowMove;
    //    [SerializeField] private Ease easing;
    //    [SerializeField] private float moveDuration = 1f;

    //    [Header("UI Positions")]
    //    [SerializeField] private Vector2 centrePos = new Vector2(0, 200);
    //    [SerializeField] private Vector2 bottomPos = new Vector2(0, 200);
    //    [SerializeField] private Vector2 offScreenPos = new Vector2(0, -300);

    //    [Header("UI Elements")]
    //    [SerializeField] private CanvasGroup infoBar;
    //    [SerializeField] private CanvasGroup panel;

    //    private Camera cam;
    //    private bool isStatusClosed = false;
    //    private bool isSFXPlayed = true;
    //    public static bool IsUIBlockingInput { get; set; } = false;

    //    // Touch tracking
    //    private Vector2 touchStartPos;
    //    private float touchStartTime;

    //    private void Awake()
    //    {
    //        if (instance != null && instance != this)
    //        {
    //            Destroy(gameObject);
    //            return;
    //        }

    //        instance = this;
    //    }

    //    private void OnEnable()
    //    {
    //        // Enable Enhanced Touch Support for mobile
    //        EnhancedTouchSupport.Enable();

    //        // Subscribe to touch events
    //        Touch.onFingerDown += OnFingerDown;
    //        Touch.onFingerUp += OnFingerUp;
    //    }

    //    private void OnDisable()
    //    {
    //        // Unsubscribe from touch events
    //        Touch.onFingerDown -= OnFingerDown;
    //        Touch.onFingerUp -= OnFingerUp;

    //        // Disable Enhanced Touch Support
    //        EnhancedTouchSupport.Disable();
    //    }

    //    private void Start()
    //    {
    //        cam = Camera.main;

    //        // Set initial positions off-screen
    //        unitInfoPanelMove.anchoredPosition = offScreenPos;
    //        unitStatusWindowMove.anchoredPosition = offScreenPos;

    //        // Mobile optimization: set target frame rate
    //        Application.targetFrameRate = 60;
    //    }

    //    private void OnFingerDown(Finger finger)
    //    {
    //        // Ignore if not the first finger (for multi-touch scenarios)
    //        if (finger.index != 0) return;

    //        // Store touch start data for tap detection
    //        touchStartPos = finger.screenPosition;
    //        touchStartTime = Time.time;
    //    }

    //    private void OnFingerUp(Finger finger)
    //    {
    //        // Ignore if not the first finger
    //        if (finger.index != 0) return;

    //        // Check if it was a tap (quick press without much movement)
    //        float touchDuration = Time.time - touchStartTime;
    //        float touchDistance = Vector2.Distance(touchStartPos, finger.screenPosition);

    //        if (touchDuration > tapTimeThreshold || touchDistance > tapDistanceThreshold)
    //        {
    //            // This was a drag/swipe, not a tap
    //            return;
    //        }

    //        // Get the touch position
    //        Vector2 touchPosition = finger.screenPosition;

    //        // Check if touching UI
    //        if (IsPointerOverUI(touchPosition))
    //        {
    //            return;
    //        }

    //        // Perform raycast to check for units
    //        Ray ray = cam.ScreenPointToRay(touchPosition);
    //        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickable))
    //        {
    //            // Unit was hit
    //            SelectByClicking(hit.collider.gameObject);
    //            UnitInfoPanelMove();

    //            if (isSFXPlayed)
    //            {
    //                ManagerAudio.instance.PlaySFX("UnitSelected");
    //                isSFXPlayed = false;
    //            }
    //        }
    //        else
    //        {
    //            // Tapped empty space
    //            isSFXPlayed = true;
    //            DeselectAll();
    //            CloseUnitInfoPanel();
    //        }
    //    }

    //    /// <summary>
    //    /// Check if touch is over UI (mobile-optimized)
    //    /// </summary>
    //    private bool IsPointerOverUI(Vector2 screenPosition)
    //    {
    //        // Create pointer event data
    //        PointerEventData pointerData = new PointerEventData(EventSystem.current)
    //        {
    //            position = screenPosition
    //        };

    //        // Raycast to check for UI elements
    //        List<RaycastResult> results = new List<RaycastResult>();
    //        EventSystem.current.RaycastAll(pointerData, results);

    //        return results.Count > 0;
    //    }

    //    private void DeselectAll()
    //    {
    //        foreach (var unit in unitsSelected)
    //        {
    //            if (unit != null) // Null check for safety
    //            {
    //                TriggerSelectionIndicator(unit, false);
    //            }
    //        }
    //        unitsSelected.Clear();
    //    }

    //    private void SelectByClicking(GameObject unit)
    //    {
    //        DeselectAll();
    //        unitsSelected.Add(unit);
    //        TriggerSelectionIndicator(unit, true);
    //    }

    //    private void TriggerSelectionIndicator(GameObject unit, bool isVisible)
    //    {
    //        if (unit != null && unit.transform.childCount > 0)
    //        {
    //            unit.transform.GetChild(0).gameObject.SetActive(isVisible);
    //        }
    //    }

    //    private void UnitInfoPanelMove()
    //    {
    //        unitInfoPanelMove.DOAnchorPos(bottomPos, moveDuration).SetEase(Ease.OutBack);
    //    }

    //    private void UnitStatusWindowMove()
    //    {
    //        unitStatusWindowMove.DOAnchorPos(centrePos, moveDuration).SetEase(Ease.OutBack);
    //    }

    //    private void CloseUnitStatusWindow()
    //    {
    //        unitStatusWindowMove.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.OutBack);
    //    }

    //    private void CloseUnitInfoPanel()
    //    {
    //        unitInfoPanelMove.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.InBack);
    //    }

    //    public void OpenStatusWindow()
    //    {
    //        isStatusClosed = false;
    //        infoBar.interactable = false;
    //        panel.blocksRaycasts = true;
    //        UnitStatusWindowMove();
    //    }

    //    public void CloseStats()
    //    {
    //        infoBar.interactable = true;
    //        CloseUnitStatusWindow();
    //        isStatusClosed = true;
    //        IsUIBlockingInput = false;
    //        panel.blocksRaycasts = false;
    //    }

    //    private void OnDestroy()
    //    {
    //        // Cleanup
    //        DeselectAll();
    //    }

    //    // Optional: Multi-select support for future
    //    public void SelectMultiple(GameObject unit)
    //    {
    //        if (unitsSelected.Contains(unit))
    //        {
    //            unitsSelected.Remove(unit);
    //            TriggerSelectionIndicator(unit, false);
    //        }
    //        else
    //        {
    //            unitsSelected.Add(unit);
    //            TriggerSelectionIndicator(unit, true);
    //        }
    //    }

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
            SelectByClicking(hit.collider.gameObject);
            UnitInfoPanelMove();

            if (isSFXPlayed)
            {
                ManagerAudio.instance.PlaySFX("UnitSelected");
                isSFXPlayed = false;
            }
        }
        else
        {
            isSFXPlayed = true;
            DeselectAll();
            CloseUnitInfoPanel();
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
        DeselectAll();
        unitsSelected.Add(unit);
        TriggerSelectionIndicator(unit, true);
    }

    /// <summary>
    /// FIXED: Now properly finds and toggles only the selection indicator, not the model
    /// </summary>
    private void TriggerSelectionIndicator(GameObject unit, bool isVisible)
    {
        if (unit == null) return;

        // Method 1: Find by tag (RECOMMENDED)
        Transform indicator = unit.transform.Find("SelectionIndicator");
        if (indicator != null)
        {
            indicator.gameObject.SetActive(isVisible);
            return;
        }

        // Method 2: Find by tag if named differently
        foreach (Transform child in unit.transform)
        {
            if (child.CompareTag(selectionIndicatorTag))
            {
                child.gameObject.SetActive(isVisible);
                return;
            }
        }

        // Method 3: Fallback - look for specific indicator names
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

        // If no indicator found, log warning
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

    /// <summary>
    /// Call this to open the status window and show unit stats
    /// </summary>
    public void OpenStatusWindow()
    {
        isStatusClosed = false;
        infoBar.interactable = false;
        panel.blocksRaycasts = true;
        UnitStatusWindowMove();

        // Show the unit popup with stats
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

        // Hide the popup
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

    /// <summary>
    /// Helper method to get currently selected unit
    /// </summary>
    public GameObject GetSelectedUnit()
    {
        if (unitsSelected.Count > 0)
            return unitsSelected[0];
        return null;
    }
}

