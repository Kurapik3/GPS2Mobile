using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;


public class SelectionOfStructureManager : MonoBehaviour
{
    //public static SelectionOfStructureManager instance;

    //public List<GameObject> allStructureList = new List<GameObject>();
    //public List<GameObject> structureSelected = new List<GameObject>();

    //[SerializeField] private LayerMask structure;

    //[SerializeField] private RectTransform sturctureInfoPanelMove;
    //[SerializeField] private RectTransform sturctureStatusWindowMove;
    //[SerializeField] private Ease easing;

    //[SerializeField] private float moveDuration = 1f;

    //[SerializeField] private Vector2 centrePos = new Vector2(0, 200);
    //[SerializeField] private Vector2 bottomPos = new Vector2(0, 200);
    //[SerializeField] private Vector2 offScreenPos = new Vector2(0, -300);

    //[SerializeField] private CanvasGroup infoBar;
    //[SerializeField] private CanvasGroup panel;

    //private Camera cam;
    //private bool isStatusClosed = false;
    //private bool isSFXPlayed = true;
    //public static bool IsUIBlockingInput { get; set; } = false;

    //private void Awake()
    //{
    //    if (instance != null && instance != this)
    //    {
    //        Destroy(gameObject);
    //    }

    //    else
    //    {
    //        instance = this;
    //    }
    //}

    //private void Start()
    //{
    //    cam = Camera.main;
    //    sturctureInfoPanelMove.anchoredPosition = offScreenPos;
    //    sturctureStatusWindowMove.anchoredPosition = offScreenPos;
    //}

    //private void Update()
    //{
    //    if (Touchscreen.current == null) return;

    //    TouchControl primaryTouch = Touchscreen.current.primaryTouch;

    //    if (primaryTouch.press.wasPressedThisFrame)
    //    {
    //        Vector2 touchPosition = primaryTouch.position.ReadValue();

    //        if (EventSystem.current.IsPointerOverGameObject(primaryTouch.touchId.ReadValue())) return;

    //        Ray ray = cam.ScreenPointToRay(touchPosition);
    //        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, structure))
    //        {
    //            SelectByClicking(hit.collider.gameObject);
    //            StructureInfoPanelMove();
    //            if (isSFXPlayed)
    //            {
    //                ManagerAudio.instance.PlaySFX("StructureSelected");
    //            }
    //        }
    //        else
    //        {
    //            isSFXPlayed = false;
    //            DeselectAll();
    //            CloseStructureInfoPanel();
    //        }
    //    }
    //}

    //private void DeselectAll()
    //{
    //    foreach (var unit in structureSelected)
    //    {
    //        TriggerSelectionIndicator(unit, false);
    //    }
    //    structureSelected.Clear();
    //}

    //private void SelectByClicking(GameObject unit)
    //{
    //    DeselectAll();
    //    structureSelected.Add(unit);
    //    TriggerSelectionIndicator(unit, true);
    //}

    //private void TriggerSelectionIndicator(GameObject unit, bool isVisible)
    //{
    //    unit.transform.GetChild(0).gameObject.SetActive(isVisible);
    //}

    //private void StructureInfoPanelMove()
    //{
    //    sturctureInfoPanelMove.DOAnchorPos(bottomPos, moveDuration).SetEase(Ease.OutBack);
    //}

    //private void StructureStatusWindowMove()
    //{
    //    sturctureStatusWindowMove.DOAnchorPos(centrePos, moveDuration).SetEase(Ease.OutBack);
    //}

    //private void CloseStructureStatusWindow()
    //{
    //    sturctureStatusWindowMove.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.OutBack);
    //}

    //private void CloseStructureInfoPanel()
    //{
    //    sturctureInfoPanelMove.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.InBack);
    //}

    //public void OpenStatusWindow()
    //{
    //    isStatusClosed = false;
    //    infoBar.interactable = false;
    //    panel.blocksRaycasts = true;
    //    StructureStatusWindowMove();
    //}

    //public void CloseStats()
    //{
    //    infoBar.interactable = true;
    //    CloseStructureStatusWindow();
    //    isStatusClosed = true;
    //    IsUIBlockingInput = false;
    //    panel.blocksRaycasts = false;
    //}
    //--------------------------------------------------------------------------------------------------------------------------------

    public static SelectionOfStructureManager instance;

    public List<GameObject> allStructureList = new List<GameObject>();
    public List<GameObject> structureSelected = new List<GameObject>();

    [Header("Input Settings")]
    [SerializeField] private LayerMask structure;
    [SerializeField] private float tapTimeThreshold = 0.3f; // Max time for a tap
    [SerializeField] private float tapDistanceThreshold = 50f; // Max movement for a tap

    [Header("UI Animation")]
    [SerializeField] private RectTransform structureInfoPanelMove;
    [SerializeField] private RectTransform structureStatusWindowMove;
    [SerializeField] private Ease easing;
    [SerializeField] private float moveDuration = 1f;

    [Header("UI Positions")]
    [SerializeField] private Vector2 centrePos = new Vector2(0, 200);
    [SerializeField] private Vector2 bottomPos = new Vector2(0, 200);
    [SerializeField] private Vector2 offScreenPos = new Vector2(0, -300);

    [Header("UI Elements")]
    [SerializeField] private CanvasGroup infoBar;
    [SerializeField] private CanvasGroup panel;

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
        // Enable Enhanced Touch Support for mobile
        EnhancedTouchSupport.Enable();

        // Subscribe to touch events
        Touch.onFingerDown += OnFingerDown;
        Touch.onFingerUp += OnFingerUp;
    }

    private void OnDisable()
    {
        // Unsubscribe from touch events
        Touch.onFingerDown -= OnFingerDown;
        Touch.onFingerUp -= OnFingerUp;

        // Disable Enhanced Touch Support
        EnhancedTouchSupport.Disable();
    }

    private void Start()
    {
        cam = Camera.main;

        // Set initial positions off-screen
        structureInfoPanelMove.anchoredPosition = offScreenPos;
        structureStatusWindowMove.anchoredPosition = offScreenPos;

        // Mobile optimization: set target frame rate
        Application.targetFrameRate = 60;
    }

    private void OnFingerDown(Finger finger)
    {
        // Ignore if not the first finger (for multi-touch scenarios)
        if (finger.index != 0) return;

        // Store touch start data for tap detection
        touchStartPos = finger.screenPosition;
        touchStartTime = Time.time;
    }

    private void OnFingerUp(Finger finger)
    {
        // Ignore if not the first finger
        if (finger.index != 0) return;

        // Check if it was a tap (quick press without much movement)
        float touchDuration = Time.time - touchStartTime;
        float touchDistance = Vector2.Distance(touchStartPos, finger.screenPosition);

        if (touchDuration > tapTimeThreshold || touchDistance > tapDistanceThreshold)
        {
            // This was a drag/swipe, not a tap
            return;
        }

        // Get the touch position
        Vector2 touchPosition = finger.screenPosition;

        // Check if touching UI
        if (IsPointerOverUI(touchPosition))
        {
            return;
        }

        // Perform raycast to check for structures
        Ray ray = cam.ScreenPointToRay(touchPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, structure))
        {
            // Structure was hit
            SelectByClicking(hit.collider.gameObject);
            StructureInfoPanelMove();

            if (isSFXPlayed)
            {
                ManagerAudio.instance.PlaySFX("StructureSelected");
                isSFXPlayed = false;
            }
        }
        else
        {
            // Tapped empty space
            isSFXPlayed = true;
            DeselectAll();
            CloseStructureInfoPanel();
        }
    }

    /// <summary>
    /// Check if touch is over UI (mobile-optimized)
    /// </summary>
    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        // Create pointer event data
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        // Raycast to check for UI elements
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        return results.Count > 0;
    }

    private void DeselectAll()
    {
        foreach (var structure in structureSelected)
        {
            if (structure != null) // Null check for safety
            {
                TriggerSelectionIndicator(structure, false);
            }
        }
        structureSelected.Clear();
    }

    private void SelectByClicking(GameObject structure)
    {
        DeselectAll();
        structureSelected.Add(structure);
        TriggerSelectionIndicator(structure, true);
    }

    private void TriggerSelectionIndicator(GameObject structure, bool isVisible)
    {
        if (structure != null && structure.transform.childCount > 0)
        {
            structure.transform.GetChild(0).gameObject.SetActive(isVisible);
        }
    }

    private void CalculatePositions()
    {

    }

    private void StructureInfoPanelMove()
    {
        structureInfoPanelMove.DOAnchorPosY(0.0f, moveDuration).SetEase(Ease.OutBack);
    }

    private void StructureStatusWindowMove()
    {
        structureStatusWindowMove.DOAnchorPos(centrePos, moveDuration).SetEase(Ease.OutBack);
    }

    private void CloseStructureStatusWindow()
    {
        structureStatusWindowMove.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.OutBack);
    }

    private void CloseStructureInfoPanel()
    {
        structureInfoPanelMove.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.InBack);
    }

    public void OpenStatusWindow()
    {
        isStatusClosed = false;
        infoBar.interactable = false;
        panel.blocksRaycasts = true;
        StructureStatusWindowMove();
    }

    public void CloseStats()
    {
        infoBar.interactable = true;
        CloseStructureStatusWindow();
        isStatusClosed = true;
        IsUIBlockingInput = false;
        panel.blocksRaycasts = false;
    }

    private void OnDestroy()
    {
        // Cleanup
        DeselectAll();
    }

    // Optional: Multi-select support for future
    public void SelectMultiple(GameObject structure)
    {
        if (structureSelected.Contains(structure))
        {
            structureSelected.Remove(structure);
            TriggerSelectionIndicator(structure, false);
        }
        else
        {
            structureSelected.Add(structure);
            TriggerSelectionIndicator(structure, true);
        }
    }
}
//--------------------------------------------------------------------------------------------------------------------------------
