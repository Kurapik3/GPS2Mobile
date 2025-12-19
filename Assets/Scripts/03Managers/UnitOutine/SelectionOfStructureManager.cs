using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

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
    [SerializeField] private GameObject structureInfoPanelMove;
    [SerializeField] private GameObject structureStatusWindowMove;
    [SerializeField] private GameObject builderSpawnConfirmationWindowMove;
    [SerializeField] private GameObject scoutSpawnConfirmationWindowMove;
    [SerializeField] private GameObject bomberSpawnConfirmationWindowMove;
    [SerializeField] private GameObject tankerSpawnConfirmationWindowMove;
    [SerializeField] private GameObject shooterSpawnConfirmationWindowMove;
    [SerializeField] private Ease easing;
    [SerializeField] private float moveDuration = 1f;

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

    private bool tutorial = true;
    public bool afterFishing = false;
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

        structureInfoPanelMove.SetActive(false);
        structureStatusWindowMove.SetActive(false);
        builderSpawnConfirmationWindowMove.SetActive(false);
        scoutSpawnConfirmationWindowMove.SetActive(false);
        bomberSpawnConfirmationWindowMove.SetActive(false);
        tankerSpawnConfirmationWindowMove.SetActive(false);
        shooterSpawnConfirmationWindowMove.SetActive(false);

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

            TryTriggerTreeBaseTutorial(hit.collider.gameObject);
            if (tile != null)
            {
                if (tile.currentUnit != null)
                {
                    if (!tile.currentUnit.hasMovedThisTurn)
                    {
                        Debug.Log("Unit on tile still has movement left — block structure popup");
                        return;
                    }
                }
                if (tile.HasStructure)
                {
                    tile.OnTileClicked();
                }
            }

            SelectByClicking(hit.collider.gameObject);
            structureInfoPanelMove.SetActive(true);

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
            structureInfoPanelMove.SetActive(false);
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
    }

    public void OpenStatusWindow()
    {
        isStatusClosed = false;
        infoBar.interactable = false;
        panel.blocksRaycasts = true;
        panel.interactable = true;
        structureStatusWindowMove.SetActive(true);

    }

    public void CloseStats()
    {
        infoBar.interactable = true;
        structureStatusWindowMove.SetActive(false);
        isStatusClosed = true;
        IsUIBlockingInput = false;
        panel.blocksRaycasts = false;
    }



    private void OnDestroy()
    {
        // Cleanup
        DeselectAll();
    }

    public void SelectMultiple(GameObject structure)
    {
        if (structureSelected.Contains(structure))
        {
            structureSelected.Remove(structure);
        }
        else
        {
            structureSelected.Add(structure);
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

    public void OpenBuilderConfirmationPopup()
    {
        builderSpawnConfirmationWindowMove.SetActive(true);
    }
    public void OpenScoutConfirmationPopup()
    {
        scoutSpawnConfirmationWindowMove.SetActive(true);
    }
    public void OpenShooterConfirmationPopup()
    {
        shooterSpawnConfirmationWindowMove.SetActive(true);
    }
    public void OpenBomberConfirmationPopup()
    {
        bomberSpawnConfirmationWindowMove.SetActive(true);
    }
    public void OpenTankerConfirmationPopup()
    {
        tankerSpawnConfirmationWindowMove.SetActive(true);
    }

    public void CloseBuilderConfirmationPopup()
    {
        builderSpawnConfirmationWindowMove.SetActive(false);
    }
    public void CloseScoutConfirmationPopup()
    {
        scoutSpawnConfirmationWindowMove.SetActive(false);
    }
    public void CloseBomberConfirmationPopup()
    {
        bomberSpawnConfirmationWindowMove.SetActive(false);
    }
    public void CloseTankerConfirmationPopup()
    {
        tankerSpawnConfirmationWindowMove.SetActive(false);
    }
    public void CloseShooterConfirmationPopup()
    {
        shooterSpawnConfirmationWindowMove.SetActive(false);
    }

    private void TryTriggerTreeBaseTutorial(GameObject hitObject)
    {
        if (!tutorial) return;
        if (TutorialUI.instance == null) return;

        TreeBase treeBase = hitObject.GetComponentInParent<TreeBase>();
        if (treeBase == null) return;
        if (afterFishing == true)
        {
            TutorialUI.instance.UpdateNotification(TutorialStage.BuildUnit);
            tutorial = false;
        }
        else return;
    }

}
