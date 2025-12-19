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
    private bool handledByThisManager = false;
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
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            GameObject hitObject = hit.collider.gameObject;
            Transform parentWithTag = hitObject.transform;
            while (parentWithTag != null &&
          !parentWithTag.CompareTag("Cache") &&
          !parentWithTag.CompareTag("Ruin") &&
          !parentWithTag.CompareTag("TurtleWall") &&
          !parentWithTag.CompareTag("Kraken") &&
          !parentWithTag.CompareTag("Fish") &&
          !parentWithTag.CompareTag("Water") &&
          !parentWithTag.CompareTag("EnemyBase") &&
          !parentWithTag.CompareTag("Grove") &&
          !parentWithTag.CompareTag("Debris"))
            {
                parentWithTag = parentWithTag.parent;
            }
            if (parentWithTag == null)
            {
       
                goto HandleEmptyTap;
            }
            hitObject = parentWithTag.gameObject;
            handledByThisManager = true;
            
            HexTile tile = hitObject.GetComponentInParent<HexTile>();
            if (tile != null)
            {

                if (tile.currentUnit != null)
                {
                    if (!tile.currentUnit.hasMovedThisTurn)
                    {
                        Debug.Log("Unit on tile still has movement left — block development popup");
                        return;
                    }
                }
                tile.OnTileClicked();
                
            }

            InteractableObject interactable = hitObject.GetComponent<InteractableObject>();
            if (interactable != null)
            {
                SelectObject(interactable);
                popupPanel.ShowPopup(interactable.objectData);
            }
            SeaMonsterBase monster = hitObject.GetComponentInParent<SeaMonsterBase>();
            if (monster != null)
            {
                if (monster.State == SeaMonsterState.Tamed)
                {
                    SeaMonsterTouchController smController = FindAnyObjectByType<SeaMonsterTouchController>();
                    smController?.TrySelectMonster(monster);
                }
            }
            SeaMonsterTouchController controller = FindAnyObjectByType<SeaMonsterTouchController>();
            if (controller != null && controller.HasSelectedMonster())
            {
                HexTile clickedTile = hitObject.GetComponentInParent<HexTile>();
                if (clickedTile != null)
                {
                    controller.MoveSelectedMonster(clickedTile);
                    return; // stop further processing
                }
            }
            return;
        }
        HandleEmptyTap:
        {
            if (!handledByThisManager) return;
            handledByThisManager = false;
            DeselectObject();
            popupPanel.HidePopup();
            if (TileSelector.CurrentTile != null)
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

    private void SelectObject(InteractableObject obj)
    {
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

        // Add the object to the appropriate selection manager based on type
        if (obj.objectData.objectType == ObjectType.Fish)
        {
            FishSelection.instance?.AddFishToSelection(obj.gameObject);
        }
        else if (obj.objectData.objectType == ObjectType.Debris)
        {
            DebirisSelect.instance?.AddDebrisToSelection(obj.gameObject);
        }
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
