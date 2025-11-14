using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class DebirisSelect : MonoBehaviour
{
    public static DebirisSelect instance;

    public List<GameObject> allDebrisList = new List<GameObject>();
    public List<GameObject> debrisSelected = new List<GameObject>();

    [Header("Input Settings")]
    [SerializeField] private LayerMask debris;
    [SerializeField] private float tapTimeThreshold = 0.3f; // Max time for a tap
    [SerializeField] private float tapDistanceThreshold = 50f; // Max movement for a tap

    [Header("UI Animation")]
    [SerializeField] private RectTransform debrisInfoPanelMove;
    [SerializeField] private RectTransform debrisStatusWindowMove;
    [SerializeField] private Ease easing;
    [SerializeField] private float moveDuration = 1f;

    [Header("UI Positions")]
    [SerializeField] private Vector2 centrePos = new Vector2(0, 0);
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
        debrisInfoPanelMove.anchoredPosition = offScreenPos;
        debrisStatusWindowMove.anchoredPosition = offScreenPos;

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
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, debris))
        {
            // Structure was hit
            SelectByClicking(hit.collider.gameObject);
            StructureInfoPanelMove();
            Debug.Log("Fish Bitch");

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
        foreach (var structure in debrisSelected)
        {
            if (structure != null) // Null check for safety
            {
                TriggerSelectionIndicator(structure, false);
            }
        }
        debrisSelected.Clear();
    }

    private void SelectByClicking(GameObject fish)
    {
        DeselectAll();
        debrisSelected.Add(fish);
        TriggerSelectionIndicator(fish, true);
    }

    private void TriggerSelectionIndicator(GameObject structure, bool isVisible)
    {
        if (structure != null && structure.transform.childCount > 0)
        {
            structure.transform.GetChild(0).gameObject.SetActive(isVisible);
        }
    }

    private void StructureInfoPanelMove()
    {
        debrisInfoPanelMove.DOAnchorPosY(0.0f, moveDuration).SetEase(Ease.OutBack);
    }

    private void StructureStatusWindowMove()
    {
        debrisStatusWindowMove.DOAnchorPos(centrePos, moveDuration).SetEase(Ease.OutBack);
    }

    private void CloseStructureStatusWindow()
    {
        debrisStatusWindowMove.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.OutBack);
    }

    private void CloseStructureInfoPanel()
    {
        debrisInfoPanelMove.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.InBack);
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
    public void OnExtractButtonPressed()
    {
        if (DebirisSelect.instance != null)
            DebirisSelect.instance.DevelopSelectedTile();
    }
    public void DevelopSelectedTile()
    {
        if (debrisSelected.Count == 0 || debrisSelected[0] == null)
        {
            Debug.Log("No debris selected.");
            return;
        }

        DebrisTile tile = debrisSelected[0].GetComponent<DebrisTile>();
        if (tile != null)
        {
            bool success = tile.OnTileTapped();
            if (success)
            {
                Debug.Log("Developed debris tile: " + debrisSelected[0].name);
                CloseStats();
                CloseStructureInfoPanel();

                debrisSelected.Clear();
            }
        }
        else
        {
            Debug.LogWarning("Selected object has no DebrisTile component: " + debrisSelected[0].name);
        }
    }
}
