using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using UnityEngine.UI;
using TMPro;
using static TileSelector;
public class SelectionOfStructureManager : MonoBehaviour
{
    public static SelectionOfStructureManager instance;

    public List<GameObject> allStructureList = new List<GameObject>();
    public List<GameObject> structureSelected = new List<GameObject>();


    [Header("Input Settings")]
    [SerializeField] private LayerMask structure;
    [SerializeField] private float tapTimeThreshold = 0.3f; 
    [SerializeField] private float tapDistanceThreshold = 50f; 

    [Header("UI Animation")]
    [SerializeField] private RectTransform structureInfoPanelMove;
    [SerializeField] private RectTransform structureStatusWindowMove;
    [SerializeField] private RectTransform builderSpawnConfirmationWindowMove;
    [SerializeField] private RectTransform scoutSpawnConfirmationWindowMove;
    [SerializeField] private RectTransform bomberSpawnConfirmationWindowMove;
    [SerializeField] private RectTransform tankerSpawnConfirmationWindowMove;
    [SerializeField] private RectTransform shooterSpawnConfirmationWindowMove;
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
    [SerializeField] private string selectionIndicatorTag = "SelectionIndicator"; 

    private Camera cam;
    private bool isStatusClosed = false;
    private bool isSFXPlayed = true;
    private bool handledByThisManager = false;
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
        structureInfoPanelMove.anchoredPosition = offScreenPos;
        structureStatusWindowMove.anchoredPosition = offScreenPos;
        builderSpawnConfirmationWindowMove.anchoredPosition = offScreenPos;
        scoutSpawnConfirmationWindowMove.anchoredPosition = offScreenPos;
        bomberSpawnConfirmationWindowMove.anchoredPosition = offScreenPos;
        tankerSpawnConfirmationWindowMove.anchoredPosition = offScreenPos;
        shooterSpawnConfirmationWindowMove.anchoredPosition = offScreenPos;

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
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, structure))
        {
            handledByThisManager = true;
            HexTile tile = hit.collider.GetComponentInParent<HexTile>();
            if (tile != null)
            {
                tile.OnTileClicked();
            }
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
            if (!handledByThisManager) return;
            handledByThisManager = false;
            isSFXPlayed = true;
            DeselectAll();
            CloseStructureInfoPanel();
            if (TileSelector.CurrentTile != null)
            {
                //EventBus.Publish(new TileDeselectedEvent(TileSelector.CurrentTile));
                TileSelector.Hide();
            }
        }
        if (structureSelected.Contains(hit.collider.gameObject))
        {
            return;
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
        foreach (var structure in structureSelected)
        {
            if (structure != null) 
            {
                //TriggerSelectionIndicator(structure, false);
            }
        }
        structureSelected.Clear();
    }

    private void SelectByClicking(GameObject structure)
    {
        DeselectAll();
        structureSelected.Add(structure);
        //TriggerSelectionIndicator(structure, true);
    }

    //private void TriggerSelectionIndicator(GameObject structure, bool isVisible)
    //{
    //    //if (structure != null && structure.transform.childCount > 0)
    //    //{
    //    //    structure.transform.GetChild(0).gameObject.SetActive(isVisible);
    //    //}

    //    if (structure == null) return;

    //    // Method 1: Find by tag (RECOMMENDED)
    //    Transform indicator = structure.transform.Find("SelectionIndicator");
    //    if (indicator != null)
    //    {
    //        indicator.gameObject.SetActive(isVisible);
    //        return;
    //    }

    //    // Method 2: Find by tag if named differently
    //    foreach (Transform child in structure.transform)
    //    {
    //        if (child.CompareTag(selectionIndicatorTag))
    //        {
    //            child.gameObject.SetActive(isVisible);
    //            return;
    //        }
    //    }

    //    // Method 3: Fallback - look for specific indicator names
    //    string[] indicatorNames = { "SelectionIndicator", "Indicator", "Selection", "Ring" };
    //    foreach (string name in indicatorNames)
    //    {
    //        Transform found = structure.transform.Find(name);
    //        if (found != null)
    //        {
    //            found.gameObject.SetActive(isVisible);
    //            return;
    //        }
    //    }

    //    Debug.LogWarning($"No selection indicator found on {structure.name}. Add a child named 'SelectionIndicator' or tag it with '{selectionIndicatorTag}'");
    //}

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
        panel.interactable = true;
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
            //TriggerSelectionIndicator(structure, false);
        }
        else
        {
            structureSelected.Add(structure);
            //TriggerSelectionIndicator(structure, true);
        }
    }

    public GameObject GetSelectedUnit()
    {
        if (structureSelected.Count > 0)
            return structureSelected[0];
        return null;
    }

    public TreeBase GetSelectedTreeBase()
    {
        if (structureSelected.Count == 0)
            return null;

        // Try to get TreeBase component from the first selected structure
        GameObject selectedObj = structureSelected[0];
        if (selectedObj == null)
            return null;

        TreeBase tb = selectedObj.GetComponent<TreeBase>();
        return tb; // returns null if not a TreeBase
    }
    private void OpenBuilderConfirmationPopupMovement()
    {
        builderSpawnConfirmationWindowMove.DOAnchorPosY(0.0f, moveDuration).SetEase(Ease.OutBack);
    }
    private void OpenScoutConfirmationPopupMovement()
    {
        scoutSpawnConfirmationWindowMove.DOAnchorPosY(0.0f, moveDuration).SetEase(Ease.OutBack);
    }
    private void OpenBomberConfirmationPopupMovement()
    {
        bomberSpawnConfirmationWindowMove.DOAnchorPosY(0.0f, moveDuration).SetEase(Ease.OutBack);
    }
    private void OpenTankerConfirmationPopupMovement()
    {
        tankerSpawnConfirmationWindowMove.DOAnchorPosY(0.0f, moveDuration).SetEase(Ease.OutBack);
    }
    private void OpenShooterConfirmationPopupMovement()
    {
        shooterSpawnConfirmationWindowMove.DOAnchorPosY(0.0f, moveDuration).SetEase(Ease.OutBack);
    }

    private void CloseBuilderConfirmationPopupMovement()
    {
        builderSpawnConfirmationWindowMove.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.OutBack);
    }
    private void CloseScoutConfirmationPopupMovement()
    {
        scoutSpawnConfirmationWindowMove.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.OutBack);
    }
    private void CloseBomberConfirmationPopupMovement()
    {
        bomberSpawnConfirmationWindowMove.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.OutBack);
    }
    private void CloseTankerConfirmationPopupMovement()
    {
        tankerSpawnConfirmationWindowMove.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.OutBack);
    }
    private void CloseShooterConfirmationPopupMovement()
    {
        shooterSpawnConfirmationWindowMove.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.OutBack);
    }

    public void OpenBuilderConfirmationPopup()
    {
        OpenBuilderConfirmationPopupMovement();
    }
    public void OpenScoutConfirmationPopup()
    {
        OpenScoutConfirmationPopupMovement();
    }
    public void OpenShooterConfirmationPopup()
    {
        OpenShooterConfirmationPopupMovement();
    }
    public void OpenBomberConfirmationPopup()
    {
        OpenBomberConfirmationPopupMovement();
    }
    public void OpenTankerConfirmationPopup()
    {
        OpenTankerConfirmationPopupMovement();
    }

    public void CloseBuilderConfirmationPopup()
    {
        CloseBuilderConfirmationPopupMovement();
    }
    public void CloseScoutConfirmationPopup()
    {
        CloseScoutConfirmationPopupMovement();
    }
    public void CloseBomberConfirmationPopup()
    {
        CloseBomberConfirmationPopupMovement();
    }
    public void CloseTankerConfirmationPopup()
    {
        CloseTankerConfirmationPopupMovement();
    }
    public void CloseShooterConfirmationPopup()
    {
        CloseShooterConfirmationPopupMovement();
    }

}
