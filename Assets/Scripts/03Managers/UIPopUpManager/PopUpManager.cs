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

    [Header ("Creature UI (Tamed)")]
    public TextMeshProUGUI statsText;
    public Image creatureIconImage;
    public TextMeshProUGUI creatureTitleText;
    public TextMeshProUGUI creatureDescriptionText;
    public Button tamedCreatureBackButton;
    [SerializeField] private GameObject creatureStatsPopup;

    public TextMeshProUGUI hpText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI movementText;
    public TextMeshProUGUI rangeText;

    [Header("Creature UI (NOT Tamed)")]
    public TextMeshProUGUI notTameCreatureTitleText;   
    public TextMeshProUGUI notTameCreatureDescriptionText;   
    public Button notTameCreaturebackButton;
    public Button notTameCreatureTechTreeButton;
    [SerializeField] private GameObject creatureInfoPopup;

    [Header("Object-Specific Buttons")]
    public GameObject fishButtons;
    public GameObject debrisButton;

    [Header("Button References")]
    [SerializeField] public Button extractFishButton;
    [SerializeField] public Button extractDebrisButton;
    [SerializeField] public Button techTreeButton; 
    [SerializeField] public Button debristechTreeButton;
    [SerializeField] public Button infoButton;
    [SerializeField] public Button ruinsBackButton;

    [Header("Stats Panel")]
    [SerializeField] private GameObject fishStatsPanel;
    [SerializeField] private GameObject debrisStatsPanel;
    [SerializeField] private GameObject ruinsInfoPopup;

    [SerializeField] private InGameSceneManager inGameSceneManager;

    private PlayerTracker player;

    private ObjectData currentData;
    private bool isPopupVisible = false;
    private ObjectType currentObjectType = ObjectType.WaterTile;

    private void Start()
    {
        player = FindAnyObjectByType<PlayerTracker>();
        if (player != null)
        {
            // Use the instance event if available
            player.OnAPChanged += OnAPChanged;
        }


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

        if (infoButton != null)
            infoButton.onClick.AddListener(OnInfoButtonPressed);

        if (notTameCreaturebackButton != null)
            notTameCreaturebackButton.onClick.AddListener(HideAllInfoPopup);

        if (notTameCreatureTechTreeButton != null)
            notTameCreatureTechTreeButton.onClick.AddListener(OnTechTreeButtonPressed);

        if (tamedCreatureBackButton != null)
            tamedCreatureBackButton.onClick.AddListener(HideAllInfoPopup);

        HidePopup();
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
            case ObjectType.Ruins:
            case ObjectType.Kraken:
            case ObjectType.Turtle:
                if (infoButton != null)
                {
                    infoButton.gameObject.SetActive(true);
                }
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
        if (infoButton != null) infoButton.gameObject.SetActive(false);

        HideAllInfoPopup();

        if (techTreeButton != null) techTreeButton.gameObject.SetActive(false);
        if (debristechTreeButton != null) debristechTreeButton.gameObject.SetActive(false);
        if (extractFishButton != null) extractFishButton.gameObject.SetActive(false);
        if (extractDebrisButton != null) extractDebrisButton.gameObject.SetActive(false);
    }

    public void HideAllInfoPopup()
    {
        if (fishStatsPanel != null) fishStatsPanel.SetActive(false);
        if (debrisStatsPanel != null) debrisStatsPanel.SetActive(false);
        if (ruinsInfoPopup != null) ruinsInfoPopup.SetActive(false);
        if (creatureStatsPopup != null) creatureStatsPopup.SetActive(false);
        if (creatureInfoPopup != null) creatureInfoPopup.SetActive(false);
    }

    public void OnInfoButtonPressed()
    {
        if (currentData == null) return;

        HideAllInfoPopup();

        switch (currentData.objectType)
        {
            case ObjectType.Ruins:
                ShowRuinsInfoPanel();
                break;
            case ObjectType.Kraken:
            case ObjectType.Turtle:
                if (TechTree.Instance?.IsTaming == true)
                {
                    creatureIconImage.sprite = currentData.icon;
                    creatureTitleText.text = currentData.objectName;

                    int hp = 20;
                    int attack = 100;
                    int movement = 1;
                    int range = 1;

                    hpText.text = $"{hp} HP;";
                    attackText.text = $"{attack} ATTACK";
                    movementText.text =  $"{movement} TILE";
                    rangeText.text =  $"{range} RANGE";

                    creatureStatsPopup?.SetActive(true);
                }
                else
                {
                    notTameCreatureTitleText.text = currentData.objectName;
                    notTameCreatureDescriptionText.text = currentData.description;
                    creatureIconImage.sprite = currentData.icon;

                    creatureInfoPopup?.SetActive(true);
                }
                    ShowCreatureInfoOrLockedPanel();
                break;
        }
    }

    private void ShowRuinsInfoPanel()
    {
        // Optional: you could show description again, but it's already in main popup
        // For now, just show an acknowledgment panel
        if (ruinsInfoPopup != null)
            ruinsInfoPopup.SetActive(true);
        // If you want dynamic text:
        // ruinsInfoText.text = currentData.description;
    }

    private void ShowCreatureInfoOrLockedPanel()
    {
        TechTree techTree = TechTree.Instance;
        bool isTamingUnlocked = techTree != null && techTree.IsTaming;

        if (!isTamingUnlocked)
        {
            // Show locked panel
            if (notTameCreatureTitleText != null)
                notTameCreatureTitleText.text = currentData.objectName;
            if (notTameCreatureDescriptionText != null)
                notTameCreatureDescriptionText.text =
                    $"This majestic creature remains wild. Unlock the \"Taming\" technology to interact with it.";
            if (creatureIconImage != null)
                creatureIconImage.sprite = currentData.icon;

            if (creatureInfoPopup != null)
                creatureInfoPopup.SetActive(true);
        }
        else
        {
            ShowCreatureStatsPanel();
        }
    }

    private void ShowCreatureStatsPanel()
    {
        // Populate generic info
        if (creatureTitleText != null)
            creatureTitleText.text = currentData.objectName;
        if (creatureDescriptionText != null)
            creatureDescriptionText.text = currentData.description;
        if (creatureIconImage != null)
            creatureIconImage.sprite = currentData.icon;

        string stats = "HP: ???\n ATK: ???\n Move: ???";

        // If you later link to actual monster:
        // SeaMonsterBase monster = GetMonsterFromCurrentSelection();
        // if (monster != null) { ... format stats ... }

        if (statsText != null)
            statsText.text = stats;

        if (creatureStatsPopup != null)
            creatureStatsPopup.SetActive(true);
    }

    public void OpenFishStats()
    {
        HideAllInfoPopup();
        fishStatsPanel.SetActive(true);
        UpdateButtonVisibility();
    }

    public void OpenDebrisStats()
    {
        HideAllInfoPopup();
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



