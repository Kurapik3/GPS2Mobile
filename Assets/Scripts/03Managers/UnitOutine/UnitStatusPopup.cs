using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitStatusPopup : MonoBehaviour
{
    public static UnitStatusPopup Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private Image unitIcon;
    [SerializeField] private TextMeshProUGUI unitNameText;
    [SerializeField] private TextMeshProUGUI unitDescriptionText;

    [Header("Stats Display")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI movementText;
    [SerializeField] private TextMeshProUGUI rangeText;

    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button trainButton;
    [SerializeField] private TextMeshProUGUI trainButtonText;
    [SerializeField] private Image trainButtonImage;

    [Header("Training Settings")]
    [SerializeField] private int trainingAPCost = 50;
    [SerializeField] private Color enabledButtonColor = new Color(0.9f, 0.9f, 0.7f);
    [SerializeField] private Color disabledButtonColor = new Color(0.5f, 0.5f, 0.5f);

    private UnitBase currentUnit;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Setup button listeners
        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClicked);

        if (trainButton != null)
            trainButton.onClick.AddListener(OnTrainButtonClicked);
    }

    private void Start()
    {
        HidePopup();
    }

    /// <summary>
    /// Opens the popup and displays the unit's information
    /// Call this from UnitS when opening the status window
    /// </summary>
    public void ShowPopup(UnitBase unit, string description = "")
    {
        if (unit == null)
        {
            Debug.LogWarning("Cannot show status for null unit!");
            return;
        }

        currentUnit = unit;

        // Show the popup
        if (popupPanel != null)
            popupPanel.SetActive(true);
        else
            gameObject.SetActive(true);

        // Set unit name
        if (unitNameText != null)
            unitNameText.text = unit.unitName;

        // Set description based on unit name if not provided
        if (string.IsNullOrEmpty(description))
        {
            description = GetDefaultDescription(unit.unitName);
        }

        if (unitDescriptionText != null)
            unitDescriptionText.text = description;

        // Update stats
        UpdateStats();

        // Update train button state
        UpdateTrainButton();
    }

    /// <summary>
    /// Gets the default description based on unit type
    /// Detects the specific unit script type for accurate descriptions
    /// </summary>
    private string GetDefaultDescription(string unitName)
    {
        // Check the actual component type for more accurate detection
        if (currentUnit != null)
        {
            if (currentUnit is BuilderUnit)
                return "Can Develop Groves";

            // Add other unit types as you implement them
            // if (currentUnit is ScoutUnit)
            //     return "Can move twice in a turn";
            // if (currentUnit is TankerUnit)
            //     return "Counter-attack enemies";
        }

        // Fallback to name-based detection
        switch (unitName.ToLower())
        {
            case "builder":
                return "Can Develop Groves";
            case "scout":
                return "Can move twice in a turn";
            case "tanker":
                return "Counter-attack enemies";
            case "shooter":
                return "Range Unit";
            case "bomber":
                return "Can do Splash Damage";
            default:
                return $"A {unitName} unit";
        }
    }

    /// <summary>
    /// Updates all stat displays
    /// </summary>
    private void UpdateStats()
    {
        if (currentUnit == null) return;

        if (hpText != null)
            hpText.text = currentUnit.hp.ToString();

        if (attackText != null)
            attackText.text = currentUnit.attack.ToString();

        if (movementText != null)
            movementText.text = currentUnit.movement.ToString() + " TILE";

        if (rangeText != null)
            rangeText.text = currentUnit.range.ToString() + " RANGE";
    }

    /// <summary>
    /// Updates the train button based on available AP
    /// </summary>
    private void UpdateTrainButton()
    {
        if (trainButton == null) return;

        bool hasEnoughAP = HasEnoughAP();

        // Enable/disable button
        trainButton.interactable = hasEnoughAP;

        // Update button appearance
        if (trainButtonImage != null)
        {
            trainButtonImage.color = hasEnoughAP ? enabledButtonColor : disabledButtonColor;
        }

        // Update button text
        if (trainButtonText != null)
        {
            if (hasEnoughAP)
            {
                trainButtonText.text = "Train";
                trainButtonText.color = Color.black;
            }
            else
            {
                trainButtonText.text = $"Train ({trainingAPCost} AP)";
                trainButtonText.color = new Color(0.3f, 0.3f, 0.3f);
            }
        }
    }

    /// <summary>
    /// Checks if the player has enough AP to train
    /// </summary>
    private bool HasEnoughAP()
    {
        if (PlayerTracker.Instance == null)
        {
            Debug.LogWarning("PlayerTracker not found!");
            return false;
        }

        return PlayerTracker.Instance.getAp() >= trainingAPCost;
    }

    /// <summary>
    /// Called when the train button is clicked
    /// </summary>
    private void OnTrainButtonClicked()
    {
        if (currentUnit == null)
        {
            Debug.LogWarning("No unit selected for training!");
            return;
        }

        if (!HasEnoughAP())
        {
            Debug.Log("Not enough AP to train unit!");
            return;
        }

        // Deduct AP using PlayerTracker
        if (PlayerTracker.Instance != null)
        {
            PlayerTracker.Instance.useAP(trainingAPCost);
            Debug.Log($"Training cost {trainingAPCost} AP. Remaining: {PlayerTracker.Instance.getAp()}");
        }

        // Train the unit (increase stats)
        TrainUnit();

        // Update displays
        UpdateStats();
        UpdateTrainButton();

        Debug.Log($"Trained {currentUnit.unitName}!");
    }

    /// <summary>
    /// Increases the unit's stats when trained
    /// </summary>
    private void TrainUnit()
    {
        if (currentUnit == null) return;

        // Increase stats (adjust these values as needed)
        currentUnit.hp += 5;
        currentUnit.attack += 1;

        // Optional: increase movement or range occasionally
        // currentUnit.movement += 1;
        // currentUnit.range += 1;

        Debug.Log($"{currentUnit.unitName} trained! New HP: {currentUnit.hp}, New Attack: {currentUnit.attack}");
    }

    /// <summary>
    /// Called when back button is clicked - closes popup via UnitS
    /// </summary>
    private void OnBackButtonClicked()
    {
        if (UnitS.instance != null)
        {
            UnitS.instance.CloseStats();
        }
        else
        {
            HidePopup();
        }
    }

    /// <summary>
    /// Closes the popup window
    /// </summary>
    public void HidePopup()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);
        else
            gameObject.SetActive(false);

        currentUnit = null;
    }

    /// <summary>
    /// Public method to update the popup if it's already open
    /// </summary>
    public void RefreshDisplay()
    {
        if (currentUnit != null && gameObject.activeSelf)
        {
            UpdateStats();
            UpdateTrainButton();
        }
    }
}
