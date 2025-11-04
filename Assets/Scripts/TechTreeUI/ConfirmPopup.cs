using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPopup : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Button researchButton;

    private TechNode selectedNode;
    private PlayerTracker player;

    private void Awake()
    {
        player = FindAnyObjectByType<PlayerTracker>();

        if (researchButton == null)
        {
            Debug.LogError("[ConfirmPopup] Research button not assigned!");
        }
        else
        {
            // Clear previous listeners to prevent duplicate calls
            researchButton.onClick.RemoveAllListeners();
            researchButton.onClick.AddListener(OnResearch);
        }

        gameObject.SetActive(false);
    }

    public void Setup(TechNode node)
    {
        if (node == null)
        {
            Debug.LogError("[ConfirmPopup] Setup() called with null node!");
            return;
        }

        selectedNode = node;
        titleText.text = $"{node.techName} ({node.costAP} AP)";
        descriptionText.text = "This Tech will enable the following:";

        bool canAfford = player.getAp() >= node.costAP;
        researchButton.interactable = canAfford;

        var colors = researchButton.colors;
        colors.normalColor = canAfford ? Color.white : Color.gray;
        researchButton.colors = colors;
    }

    public void OnResearch()
    {
        if (selectedNode == null)
        {
            Debug.LogError("[ConfirmPopup] Cannot research — no tech node selected! (Did Setup() run?)");
            return;
        }

        if (player == null)
        {
            Debug.LogError("[ConfirmPopup] No PlayerTracker found!");
            return;
        }

        if (player.getAp() >= selectedNode.costAP) 
        {
            player.useAP(selectedNode.costAP);
            selectedNode.Unlock();
            Debug.Log("Research " + selectedNode.techName);
        }
        else
        {
            Debug.Log("Not Enough AP");
        }

        gameObject.SetActive(false);
    }

    public void OnBack()
    {
        gameObject.SetActive(false);
    }
}
