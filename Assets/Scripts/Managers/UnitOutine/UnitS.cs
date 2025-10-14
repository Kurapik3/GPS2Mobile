using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

public class UnitS : MonoBehaviour
{
    public static UnitS instance;

    public List<GameObject> allUnitList = new List<GameObject>();
    public List<GameObject> unitsSelected = new List<GameObject>();

    [SerializeField] private LayerMask clickable;

    [SerializeField] private RectTransform unitInfoPanelMove;
    [SerializeField] private RectTransform unitStatusWindowMove;
    [SerializeField] private Ease easing;

    [SerializeField] private float moveDuration = 1f;

    [SerializeField] private Vector2 centrePos = new Vector2(0, 200);
    [SerializeField] private Vector2 bottomPos = new Vector2(0, 200);
    [SerializeField] private Vector2 offScreenPos = new Vector2(0, -300);

    [SerializeField] private CanvasGroup infoBar;
    [SerializeField] private CanvasGroup panel;

    private Camera cam;
    private bool isStatusClosed = false;
    private bool isSFXPlayed = true;
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
        unitInfoPanelMove.anchoredPosition = offScreenPos;
        unitStatusWindowMove.anchoredPosition = offScreenPos;
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
    }

    private void DeselectAll()
    {
        foreach (var unit in unitsSelected)
        {
            TriggerSelectionIndicator(unit, false);
        }
        unitsSelected.Clear();
    }

    private void SelectByClicking(GameObject unit)
    {
        DeselectAll();
        unitsSelected.Add(unit);
        TriggerSelectionIndicator(unit, true);
    }

    private void TriggerSelectionIndicator(GameObject unit, bool isVisible)
    {
        unit.transform.GetChild(0).gameObject.SetActive(isVisible);
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
    }

    public void CloseStats()
    {
        infoBar.interactable = true;
        CloseUnitStatusWindow();
        isStatusClosed = true;
        IsUIBlockingInput = false;
        panel.blocksRaycasts = false;
    }
}

