using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExtractionButtonStatus : MonoBehaviour
{
    [Header("References")]
    public Button button;
    public Image backgroundCircle;
    public Image lockIcon;
    public TextMeshProUGUI costText;

    [Header("Visual States")]
    public Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 0.7f); 
    public Color availableColor = new Color(0.5f, 0.5f, 0.5f, 0.7f); 
    public Color unlockedColor = new Color(0.2f, 0.8f, 0.9f, 0.9f); 

    [Header("Cost & Requirements")]
    public int apCost;
    public string techName;
    public ObjectType objectType; 

    private PlayerTracker player;
    private TechTree techTree;
    private PopUpManager popUpManager;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (backgroundCircle == null) backgroundCircle = GetComponent<Image>();

        player = FindAnyObjectByType<PlayerTracker>();
        techTree = FindAnyObjectByType<TechTree>();
        popUpManager = FindObjectOfType<PopUpManager>();

        UpdateStatus();
    }

    private void OnEnable()
    {
        player = PlayerTracker.Instance;
        techTree = TechTree.Instance;

        if (player != null)
            player.OnAPChanged += UpdateStatus;
        if (techTree != null)
            techTree.OnTechResearched += UpdateStatus;

        UpdateStatus(); // Initial refresh
    }

    private void OnDisable()
    {
        if (player != null)
            player.OnAPChanged -= UpdateStatus;
        if (techTree != null)
            techTree.OnTechResearched -= UpdateStatus;
    }

    public void UpdateStatus()
    {
        if (player == null || techTree == null || popUpManager == null)
            return;

        bool isUnlocked = IsTechUnlocked();
        bool hasEnoughAP = player.getAp() >= apCost;

        button.interactable = true;

        if (!isUnlocked)
        {
            backgroundCircle.color = lockedColor;
            if (lockIcon != null) lockIcon.gameObject.SetActive(true);
            costText.color = Color.white;
        }
        else if (!hasEnoughAP)
        {
            backgroundCircle.color = availableColor;
            if (lockIcon != null) lockIcon.gameObject.SetActive(false);
            costText.color = Color.white;

            button.interactable = false;
        }
        else
        {
            backgroundCircle.color = unlockedColor;
            if (lockIcon != null) lockIcon.gameObject.SetActive(false);
            costText.color = Color.black;
        }
    }

    private bool IsTechUnlocked()
    {
        if (techTree == null) return false;

        switch (techName.ToLower())
        {
            case "fishing": return techTree.IsFishing;
            case "metal scrap": return techTree.IsMetalScraps;
            default: return false;
        }
    }

    public void OnButtonClick()
    {
        if (popUpManager == null) return;

        bool isUnlocked = IsTechUnlocked();
        bool hasEnoughAP = player.getAp() >= apCost;

        if (!isUnlocked)
        {
            ObjectData data = new ObjectData
            {
                objectName = techName,
                description = $"Research {techName} to extract this resource.",
                objectType = objectType,
                icon = GetIconForType(objectType)
            };

            popUpManager.ShowPopup(data);

            if (objectType == ObjectType.Fish)
            {
                popUpManager.OpenFishStats();
            }
            else if (objectType == ObjectType.Debris)
            {
                popUpManager.OpenDebrisStats();
            }
        }
        else if (hasEnoughAP)
        {
            ObjectData data = new ObjectData
            {
                objectName = techName,
                description = $"Produces {(objectType == ObjectType.Fish ? "1" : "2")} population.",
                objectType = objectType,
                icon = GetIconForType(objectType)
            };

            popUpManager.ShowPopup(data);

            if (objectType == ObjectType.Fish)
            {
                popUpManager.OpenFishStats();
            }
            else if (objectType == ObjectType.Debris)
            {
                popUpManager.OpenDebrisStats();
            }
        }
        else
        {
            Debug.Log("Not enough AP to extract!");
        }
    }

    private Sprite GetIconForType(ObjectType type)
    {
        // You'll need to assign these in the Inspector
        // For now, return null or create a method to get icons from a database
        return null;
    }

    public void ForceRefresh()
    {
        UpdateStatus();
    }
}