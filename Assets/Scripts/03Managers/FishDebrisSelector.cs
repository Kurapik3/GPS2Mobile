using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class FishDebrisSelector : MonoBehaviour
{
    public static FishDebrisSelector instance;

    [Header("Input Settings")]
    [SerializeField] private LayerMask fishAndDebrisLayer;
    [SerializeField] private float tapTimeThreshold = 0.3f;
    [SerializeField] private float tapDistanceThreshold = 50f;

    [Header("UI Elements")]
    [SerializeField] private PopUpManager popUpManager;

    private Camera cam;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        cam = Camera.main;
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

    private void OnFingerDown(Finger finger)
    {
        if (finger.index != 0) return;

        Vector2 touchPosition = finger.screenPosition;

        if (IsPointerOverUI(touchPosition))
        {
            return;
        }

        Ray ray = cam.ScreenPointToRay(touchPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, fishAndDebrisLayer))
        {
            SelectObject(hit.collider.gameObject);
        }
    }

    private void OnFingerUp(Finger finger)
    {
        // Handle release if needed
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

    private void SelectObject(GameObject selectedObject)
    {
        if (selectedObject == null) return;

        // Determine object type
        ObjectType objectType = ObjectType.Fish;
        string techName = "Fishing";
        int apCost = 2; // Default for fish

        if (selectedObject.CompareTag("Debris"))
        {
            objectType = ObjectType.Debris;
            techName = "MetalScraps";
            apCost = 5;
        }

        // Create object data
        ObjectData data = new ObjectData
        {
            objectName = techName,
            description = $"Research {techName} to extract this resource.",
            objectType = objectType,
            icon = GetIconForType(objectType)
        };

        // Show popup
        if (popUpManager != null)
        {
            popUpManager.ShowPopup(data);
        }
    }

    private Sprite GetIconForType(ObjectType type)
    {
        // Return appropriate icon based on type
        // You can implement this based on your game assets
        return null;
    }
}