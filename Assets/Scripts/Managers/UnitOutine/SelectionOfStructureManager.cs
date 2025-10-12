using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class SelectionOfStructureManager : MonoBehaviour
{
    public static SelectionOfStructureManager instance;

    public List<GameObject> allStructureList = new List<GameObject>();
    public List<GameObject> structureSelected = new List<GameObject>();

    [SerializeField] private LayerMask structure;

    [SerializeField] private RectTransform sturctureInfoPanelMove;
    [SerializeField] private RectTransform sturctureStatusWindowMove;
    [SerializeField] private Ease easing;

    [SerializeField] private float moveDuration = 1f;

    [SerializeField] private Vector2 centrePos = new Vector2(0, 200);
    [SerializeField] private Vector2 bottomPos = new Vector2(0, 200);
    [SerializeField] private Vector2 offScreenPos = new Vector2(0, -300);

    [SerializeField] private CanvasGroup infoBar;
    [SerializeField] private CanvasGroup panel;

    private Camera cam;
    private bool isStatusClosed = false;
    public static bool IsUIBlockingInput { get; set; } = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }

        else
        {
            instance = this;
        }
    }

    private void Start()
    {
        cam = Camera.main;
        sturctureInfoPanelMove.anchoredPosition = offScreenPos;
        sturctureStatusWindowMove.anchoredPosition = offScreenPos;
    }

    private void Update()
    {
        if (Touchscreen.current == null) return;

        TouchControl primaryTouch = Touchscreen.current.primaryTouch;

        if (primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 touchPosition = primaryTouch.position.ReadValue();

            if (EventSystem.current.IsPointerOverGameObject(primaryTouch.touchId.ReadValue())) return;

            Ray ray = cam.ScreenPointToRay(touchPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, structure))
            {
                SelectByClicking(hit.collider.gameObject);
                StructureInfoPanelMove();
            }
            else
            {
                DeselectAll();
                CloseStructureInfoPanel();
            }
        }
    }

    private void DeselectAll()
    {
        foreach (var unit in structureSelected)
        {
            TriggerSelectionIndicator(unit, false);
        }
        structureSelected.Clear();
    }

    private void SelectByClicking(GameObject unit)
    {
        DeselectAll();
        structureSelected.Add(unit);
        TriggerSelectionIndicator(unit, true);
    }

    private void TriggerSelectionIndicator(GameObject unit, bool isVisible)
    {
        unit.transform.GetChild(0).gameObject.SetActive(isVisible);
    }

    private void StructureInfoPanelMove()
    {
        sturctureInfoPanelMove.DOAnchorPos(bottomPos, moveDuration).SetEase(Ease.OutBack);
    }

    private void StructureStatusWindowMove()
    {
        sturctureStatusWindowMove.DOAnchorPos(centrePos, moveDuration).SetEase(Ease.OutBack);
    }

    private void CloseStructureStatusWindow()
    {
        sturctureStatusWindowMove.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.OutBack);
    }

    private void CloseStructureInfoPanel()
    {
        sturctureInfoPanelMove.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.InBack);
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
}
