using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class UnitInteractionBubble : MonoBehaviour
{
    [Header("Proximity Settings")]
    public float interactionRadius = 1.5f; // Distance to trigger bubble

    [Header("UI")]
    public GameObject bubblePrefab; // Prefab with Image + Button
    public Canvas uiCanvas;         // Reference to your UI canvas (set in inspector or find)

    private GameObject currentBubble;
    private InteractableObject currentTarget;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (uiCanvas == null)
            uiCanvas = FindObjectOfType<Canvas>();
    }

    void Update()
    {
        // Only check if unit is standing on an object (not just near it)
        InteractableObject target = FindInteractableUnderUnit();

        if (target != currentTarget)
        {
            CleanupBubble();
            currentTarget = target;
            if (currentTarget != null)
            {
                CreateBubble();
            }
        }

        // Keep bubble positioned above unit
        if (currentBubble != null)
        {
            Vector3 worldPos = transform.position + Vector3.up * 1.2f;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
            currentBubble.transform.position = screenPos;
        }
    }

    private InteractableObject FindInteractableUnderUnit()
    {
        // Raycast downward from unit's position to find object directly below
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f; // Slightly above unit
        RaycastHit hit;

        // Only check objects on "Interactable" layer
        int interactableLayerMask = LayerMask.GetMask("Interactable");

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 2f, interactableLayerMask))
        {
            InteractableObject io = hit.collider.GetComponent<InteractableObject>();
            if (io != null && io.objectData?.showProximityBubble == true)
            {
                return io;
            }
        }

        return null;
    }

    //bool ShouldShowBubble(InteractableObject obj)
    //{
    //    // Optional: add conditions like "only if unit has enough AP", etc.
    //    return obj != null && obj.objectData?.showProximityBubble == true;
    //}

    void CreateBubble()
    {
        if (bubblePrefab == null || uiCanvas == null || currentTarget?.objectData?.icon == null)
            return;

        currentBubble = Instantiate(bubblePrefab, uiCanvas.transform);
        currentBubble.transform.localScale = Vector3.one;

        // Set icon
        Image image = currentBubble.GetComponent<Image>();
        if (image != null)
            image.sprite = currentTarget.objectData.icon;

        // Make it clickable
        Button button = currentBubble.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnBubbleClicked);
        }
    }

    void OnBubbleClicked()
    {
        // Mimic the same logic as tapping the object!
        // This opens your existing PopUpManager
        if (currentTarget != null && FindObjectOfType<PopUpManager>() is PopUpManager popup)
        {
            popup.ShowPopup(currentTarget.objectData);
        }

        CleanupBubble(); // Optional: hide bubble after click
    }

    void CleanupBubble()
    {
        if (currentBubble != null)
        {
            Destroy(currentBubble);
            currentBubble = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}