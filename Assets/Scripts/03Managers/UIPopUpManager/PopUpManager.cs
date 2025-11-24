using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ObjectData
{
    public string objectName;
    public string description;
    public Sprite icon;
    public ObjectType objectType;
    public int apCost = 2;
}
public enum ObjectType
{
    Fish,
    Debris,
    Ruins,
    Clam, 
    Groove, 
    WaterTile,
    Kraken, 
    Enemy, 
    EnemyBase, 
    Turtle
}

public class PopUpManager : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;

    [Header("Object-Specific Buttons")]
    public GameObject fishButtons;
    public GameObject debrisButton;

    [Header("Button References")]
    [SerializeField] public Button extractFishButton;
    [SerializeField] public Button extractDebrisButton;
    [SerializeField] public Button techTreeButton; 
    [SerializeField] public Button debristechTreeButton; 

    [Header("Stats Panel")]
    [SerializeField] private GameObject fishStatsPanel;
    [SerializeField] private GameObject debrisStatsPanel;

    [SerializeField] private InGameSceneManager inGameSceneManager;

    private PlayerTracker player;

    private ObjectData currentData;
    private bool isPopupVisible = false;

    private void Start()
    {
        player = FindAnyObjectByType<PlayerTracker>();
        if (player != null)
        {
            // Use the instance event if available
            player.OnAPChanged += OnAPChanged;
        }

        HidePopup();

        debrisStatsPanel.SetActive(false);
        fishStatsPanel.SetActive(false);

        if (extractFishButton != null)
            extractFishButton.onClick.AddListener(OnExtractFishButtonPressed);

        if (extractDebrisButton != null)
            extractDebrisButton.onClick.AddListener(OnExtractDebrisButtonPressed);

        if (techTreeButton != null)
            techTreeButton.onClick.AddListener(OnTechTreeButtonPressed);

        if (debristechTreeButton != null)
            debristechTreeButton.onClick.AddListener(OnTechTreeButtonPressed);
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (player != null)
        {
            player.OnAPChanged -= OnAPChanged;
        }
    }

    private void OnAPChanged()
    {
        if (isPopupVisible)
        {
            UpdateButtonVisibility();
        }
    }

    public void ShowPopup(ObjectData data)
    {
        currentData = data;

        titleText.text = data.objectName;
        descriptionText.text = data.description;
        iconImage.sprite = data.icon;

        HideAllButtons();

        switch (data.objectType)
        {
            case ObjectType.Fish:
                if (fishButtons != null) fishButtons.SetActive(true);
                break;
            case ObjectType.Debris:
                if (debrisButton != null) debrisButton.SetActive(true);
                break;
        }
        UpdateButtonVisibility();
        gameObject.SetActive(true);
    }

    public void HidePopup()
    {
        currentData = null;

        gameObject.SetActive(false);
        HideAllButtons();
    }

    private void HideAllButtons()
    {
        if (fishButtons != null) fishButtons.SetActive(false);
        if (debrisButton != null) debrisButton.SetActive(false);
        if (techTreeButton != null) techTreeButton.gameObject.SetActive(false);
        if (debristechTreeButton != null) debristechTreeButton.gameObject.SetActive(false);
    }

    public void OpenFishStats()
    {
        fishStatsPanel.SetActive(true);
        UpdateButtonVisibility();
    }

    public void OpenDebrisStats()
    {
        debrisStatsPanel.SetActive(true);
        UpdateButtonVisibility();
    }

    public void CloseFishStats()
    {
        fishStatsPanel.SetActive(false);
    }
    public void CloseDebrisStats()
    {
        debrisStatsPanel.SetActive(false);
    }

    private void UpdateButtonVisibility()
    {
        if (currentData == null) return;

        TechTree techTree = FindAnyObjectByType<TechTree>();
        if (techTree == null) return;

        // Check if tech is unlocked
        bool isUnlocked = false;
        switch (currentData.objectType)
        {
            case ObjectType.Fish:
                isUnlocked = techTree.IsFishing;
                break;
            case ObjectType.Debris:
                isUnlocked = techTree.IsMetalScraps;
                break;
        }

        // Check if player has enough AP
        bool hasEnoughAP = player != null && player.getAp() >= currentData.apCost;

        // Determine which button to show
        if (!isUnlocked)
        {
            // Tech not unlocked - show Tech Tree button
            if (techTreeButton != null && currentData.objectType == ObjectType.Fish)
            {
                techTreeButton.gameObject.SetActive(true);
            }
            if (debristechTreeButton != null && currentData.objectType == ObjectType.Debris)
            {
                debristechTreeButton.gameObject.SetActive(true);
            }

            // Hide extract buttons
            if (extractFishButton != null) extractFishButton.gameObject.SetActive(false);
            if (extractDebrisButton != null) extractDebrisButton.gameObject.SetActive(false);
        }
        else
        {
            // Tech is unlocked - show extract button
            if (currentData.objectType == ObjectType.Fish && extractFishButton != null)
            {
                extractFishButton.gameObject.SetActive(true);
                extractFishButton.interactable = hasEnoughAP; // Enable/disable based on AP
            }
            if (currentData.objectType == ObjectType.Debris && extractDebrisButton != null)
            {
                extractDebrisButton.gameObject.SetActive(true);
                extractDebrisButton.interactable = hasEnoughAP; // Enable/disable based on AP
            }

            // Hide tech tree buttons
            if (techTreeButton != null) techTreeButton.gameObject.SetActive(false);
            if (debristechTreeButton != null) debristechTreeButton.gameObject.SetActive(false);
        }
    }

    private void OnExtractFishButtonPressed()
    {
        if (currentData != null && currentData.objectType == ObjectType.Fish)
        {
            if (FishSelection.instance != null)
                FishSelection.instance.DevelopSelectedTile();
            

            HidePopup();
            CloseFishStats();
        }
    }

    private void OnExtractDebrisButtonPressed()
    {
        if (currentData != null && currentData.objectType == ObjectType.Debris)
        {
            if (DebirisSelect.instance != null)
                DebirisSelect.instance.DevelopSelectedTile();

            HidePopup();
            CloseDebrisStats();
        }
    }

    public void OnTechTreeButtonPressed()
    {
        Debug.Log("Opening Tech Tree...");

        if (inGameSceneManager != null)
        {
            inGameSceneManager.OpenTechTree();
            CloseDebrisStats();
            CloseFishStats();
        }
        else
        {
            Debug.LogError("InGameSceneManager is not assigned in PopUpManager! Please assign it in the Inspector.");
        }

        HidePopup();
    }
}



