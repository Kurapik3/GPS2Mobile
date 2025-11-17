using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class Interactablemanager : MonoBehaviour
{
    [Header("Detection Settings")]
    public LayerMask interactableLayer;
    public float tapTimeThreshold = 0.3f;
    public float tapDistanceThreshold = 50f;

    [Header("References")]
    public PopUpManager popupPanel;

    private Camera cam;
    private Vector2 touchStartPos;
    private float touchStartTime;
    private GameObject currentSelectedObject;

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

        // Check if it's a tap (not a drag or long press)
        if (touchDuration > tapTimeThreshold || touchDistance > tapDistanceThreshold)
        {
            return;
        }

        Vector2 touchPosition = finger.screenPosition;

        // Ignore touches on UI elements
        if (IsPointerOverUI(touchPosition))
        {
            return;
        }

        Ray ray = cam.ScreenPointToRay(touchPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, interactableLayer))
        {
            // Try to get InteractableObject component
            InteractableObject interactable = hit.collider.GetComponent<InteractableObject>();

            if (interactable != null)
            {
                SelectObject(interactable);
                popupPanel.ShowPopup(interactable.objectData);
            }
        }
        else
        {
            // Tapped empty space - deselect and close popup
            DeselectObject();
            popupPanel.HidePopup();
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

    private void SelectObject(InteractableObject obj)
    {
        // Deselect previous object if any
        if (currentSelectedObject != null)
        {
            InteractableObject prevInteractable = currentSelectedObject.GetComponent<InteractableObject>();
            if (prevInteractable != null)
            {
                prevInteractable.OnDeselected();
            }
        }

        currentSelectedObject = obj.gameObject;
        obj.OnSelected();
    }

    private void DeselectObject()
    {
        if (currentSelectedObject != null)
        {
            InteractableObject interactable = currentSelectedObject.GetComponent<InteractableObject>();
            if (interactable != null)
            {
                interactable.OnDeselected();
            }
            currentSelectedObject = null;
        }
    }
}
