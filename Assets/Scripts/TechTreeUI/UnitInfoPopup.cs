using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitInfoPopup : MonoBehaviour
{
    public Image iconImage; 
    public TextMeshProUGUI unitNameText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI movementText;
    public TextMeshProUGUI rangeText;
    public TextMeshProUGUI unitDescriptionText;
    public Button backButton;

    private TechNode selectedNode;
    private PlayerTracker player;

    private void Awake()
    {
        player = FindAnyObjectByType<PlayerTracker>();

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBack);
        }

        gameObject.SetActive(false);
    }

    public void Setup(TechNode node)
    {
        if (node == null)
        {
            Debug.LogError("[UnitInfoPopup] Setup() called with null node!");
            return;
        }

        selectedNode = node;

        // Set the icon
        if (iconImage != null && node.icon != null)
        {
            iconImage.gameObject.SetActive(true);
            iconImage.sprite = node.icon;
            iconImage.SetNativeSize(); // Optional
        }
        else if (iconImage != null)
        {
            iconImage.gameObject.SetActive(false);
        }

        // Get unit data
        var unitData = GetUnitDataForTech(node.techName);
        if (unitData != null)
        {
            unitNameText.text = unitData.unitName;

            string desc = !string.IsNullOrEmpty(unitData.description)
               ? unitData.description
               : unitData.ability;
            unitDescriptionText.text = desc;

            hpText.text = $"{unitData.hp} HP";
            attackText.text = $"{unitData.attack} ATTACK";
            movementText.text = $"{unitData.movement} TILE";
            rangeText.text = $"{unitData.range} RANGE";
        }
        else
        {
            unitNameText.text = node.techName;
            unitDescriptionText.text = "No description available.";
            hpText.text = "N/A";
            attackText.text = "N/A";
            movementText.text = "N/A";
            rangeText.text = "N/A";
        }

        gameObject.SetActive(true);
    }

    private UnitData GetUnitDataForTech(string techName)
    {
        // Map tech name to unit data
        switch (techName.ToLower())
        {
            case "armor":
                return new UnitData("Tank", 0, 1, 1, 20, 5, true, "Counter-attack Enemies");
            case "scouting":
                return new UnitData("Scout", 0, 2, 3, 5, 3, true, "Can move twice in a turn");
            case "shooter":
                return new UnitData("Shooter", 0, 5, 2, 5, 5, true, "Range Unit");
            case "naval warfare":
                return new UnitData("Bomber", 0, 3, 2, 15, 10, true, "Can do Splash Damage");
            default:
                return null;
        }
    }

    public void OnBack()
    {
        gameObject.SetActive(false);
    }
}