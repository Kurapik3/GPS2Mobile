using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TreeBaseUpgradeProgressUI : MonoBehaviour
{
    [Header("Popup UI Elements")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button scoreButton;
    [SerializeField] private Button apButton;
    [SerializeField] private Button turfButton;

    private TreeBase treeBase;

    void Start()
    {
        treeBase = FindObjectOfType<TreeBase>();
        if (treeBase == null)
        {
            Debug.LogError("TreeBaseUpgradePopup: Could not find TreeBase in scene!");
            return;
        }

        popupPanel.SetActive(false);

        scoreButton.onClick.AddListener(() => ChooseReward("Score"));
        apButton.onClick.AddListener(() => ChooseReward("AP"));
        turfButton.onClick.AddListener(() => ChooseReward("Turf"));
    }

    public void ShowPopup(int nextLevel)
    {
        if (popupPanel == null) return;

        popupPanel.SetActive(true);
        titleText.text = $"Base Leveled Up";
        descriptionText.text = $"Base has been upgraded to level {nextLevel}. Base health increased by +5. You also get to pick additional rewards.";

        scoreButton.interactable = true;
        apButton.interactable = true;
        turfButton.interactable = true;
    }

    private void ChooseReward(string rewardType)
    {
        if (treeBase == null) return;

        switch (rewardType)
        {
            case "Score":
                treeBase.ChooseScore();
                break;
            case "AP":
                treeBase.ChooseApPerTurn();
                break;
            case "Turf":
                treeBase.ChooseTurfUp();
                break;
        }

        popupPanel.SetActive(false);

        // Notify HP display to update
        TreeBaseHPDisplay hpDisplay = FindObjectOfType<TreeBaseHPDisplay>();
        if (hpDisplay != null)
        {
            hpDisplay.OnLevelChanged();
        }
    }
}
