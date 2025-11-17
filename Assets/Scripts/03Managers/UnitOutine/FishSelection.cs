using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class FishSelection : MonoBehaviour
{
    public static FishSelection instance;

    public List<GameObject> allFishList = new List<GameObject>();
    public List<GameObject> fishSelected = new List<GameObject>();

    [Header("Input Settings")]
    [SerializeField] private LayerMask fish;
    [SerializeField] private float tapTimeThreshold = 0.3f; // Max time for a tap
    [SerializeField] private float tapDistanceThreshold = 50f; // Max movement for a tap

    [Header("UI Animation")]
    [SerializeField] private RectTransform fishInfoPanelMove;
    [SerializeField] private RectTransform fishStatusWindowMove;
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

        fishInfoPanelMove.anchoredPosition = offScreenPos;
        fishStatusWindowMove.anchoredPosition = offScreenPos;

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
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, fish))
        {
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
            isSFXPlayed = true;
            DeselectAll();
            CloseStructureInfoPanel();
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
        foreach (var structure in fishSelected)
        {
            if (structure != null) // Null check for safety
            {
                TriggerSelectionIndicator(structure, false);
            }
        }
        fishSelected.Clear();
    }

    private void SelectByClicking(GameObject fish)
    {
        DeselectAll();
        fishSelected.Add(fish);
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
        fishInfoPanelMove.DOAnchorPosY(0.0f, moveDuration).SetEase(Ease.OutBack);
    }

    private void StructureStatusWindowMove()
    {
        fishStatusWindowMove.DOAnchorPos(centrePos, moveDuration).SetEase(Ease.OutBack);
    }

    private void CloseStructureStatusWindow()
    {
        fishStatusWindowMove.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.OutBack);
    }

    private void CloseStructureInfoPanel()
    {
        fishInfoPanelMove.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.InBack);
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
        if (FishSelection.instance != null)
            FishSelection.instance.DevelopSelectedTile();
    }
    public void DevelopSelectedTile()
    {
        if (fishSelected.Count == 0 || fishSelected[0] == null)
        {
            Debug.Log("No fish selected.");
            return;
        }

        FishTile tile = fishSelected[0].GetComponent<FishTile>();
        if (tile != null)
        {
            bool success = tile.OnTileTapped();
            if (success)
            {
                Debug.Log("Developed fish tile: " + fishSelected[0].name);
                // Close the UI panels
                CloseStats();          // status window
                CloseStructureInfoPanel(); // info panel

                // Remove the fish from selection list
                fishSelected.Clear();
            }
        }
        else
        {
            Debug.LogWarning("Selected object has no FishTile component: " + fishSelected[0].name);
        }
    }
}
