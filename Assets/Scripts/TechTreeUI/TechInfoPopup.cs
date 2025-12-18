using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TechInfoPopup : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Image iconImage; 
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
            Debug.LogError("[TechInfoPopup] Setup() called with null node!");
            return;
        }

        selectedNode = node;
        titleText.text = node.techName;

        // Set the icon
        if (iconImage != null && node.icon != null)
        {
            iconImage.gameObject.SetActive(true);
            iconImage.sprite = node.icon;
            iconImage.SetNativeSize(); // Optional: match sprite size
        }
        else if (iconImage != null)
        {
            iconImage.gameObject.SetActive(false);
        }

        // Set description
        descriptionText.text = GetDescriptionForTech(node.techName);

        gameObject.SetActive(true);
    }

    private string GetDescriptionForTech(string techName)
    {
        switch (techName.ToLower())
        {
            case "fishing":
                return "Produces 1 population. Can be used to help upgrade bases.";
            case "metal scrap":
                return "Produces 2 population. Can be used to help upgrade bases.";
            case "camoflage":
                return "The Scout Unit will be able to be invisible undetectable by sea creatures or Enemies.";
            case "clear sight":
                return "All Boat Units will get to see 1 tile further into fog.";
            case "home defense":
                return "All Tree Bases gain +5 Max HP, and units in turf take 1 less damage.";
            case "mob research":
                return "Gain the ability to track both Turtle and Kraken movements and view their data.";
            case "mutualism":
                return "Enable movement on Turtle Island until there take reduced damage when defending.";
            case "hunter's mark":
                return "Gain the ability to do more damage (+5 damage) to Sea Creatures.";
            case "taming":
                return "Allows taming of all sea creatures, which are instantly tamed upon unlocking this tech.";
            default:
                return "No description available.";
        }
    }

    public void OnBack()
    {
        gameObject.SetActive(false);
    }
}