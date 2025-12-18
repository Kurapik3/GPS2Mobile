
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPopup : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Button researchButton;
    public Button infoButton;
    public Image iconImage;

    private TechNode selectedNode;
    //private PlayerTracker player;

    private bool _listenersAdded = false;

    private void Awake()
    {
        //player = FindAnyObjectByType<PlayerTracker>();

        if (!_listenersAdded)
        {
            if (researchButton != null)
            {
                researchButton.onClick.AddListener(OnResearch);
            }
            if (infoButton != null)
            {
                infoButton.onClick.AddListener(OnInfo);
            }
            _listenersAdded = true;
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

        // Get PlayerTracker FRESH every time
        PlayerTracker player = FindAnyObjectByType<PlayerTracker>();
        if (player == null)
        {
            Debug.LogError("[ConfirmPopup] PlayerTracker not found!");
            // Optionally: disable research, show error
        }

        selectedNode = node;
        titleText.text = $"{node.techName} ({node.costAP} AP)";

        int currentAP = player?.getAp() ?? 0;

        if (currentAP >= node.costAP)
        {
            descriptionText.text = "This Tech will enable the following:";
            researchButton.interactable = true;
            var buttonText = researchButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = "Research";
        }
        else
        {
            descriptionText.text = $"You do not have enough AP to unlock the following tech: {node.techName}";
            researchButton.interactable = false;
            var buttonText = researchButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = "Insufficient AP";
        }

        // Set icon...
        if (iconImage != null && node.icon != null)
        {
            iconImage.gameObject.SetActive(true);
            iconImage.sprite = node.icon;
            iconImage.SetNativeSize();
        }
        else if (iconImage != null)
        {
            iconImage.gameObject.SetActive(false);
        }

        gameObject.SetActive(true);
    }

    public void OnResearch()
    {
        if (selectedNode == null || TechTree.Instance == null) return;

        PlayerTracker player = FindAnyObjectByType<PlayerTracker>();
        if (player == null)
        {
            Debug.LogError("[ConfirmPopup] PlayerTracker missing on research!");
            return;
        }

        if (player.getAp() < selectedNode.costAP)
        {
            Debug.Log("Not enough AP!");
            return;
        }

        bool success = TechTree.Instance.UnlockTech(selectedNode.techName, selectedNode.costAP);
        if (success)
        {
            selectedNode.Unlock();
            var extractionButtons = FindObjectsOfType<ExtractionButtonStatus>();
            foreach (var btn in extractionButtons)
            {
                btn.ForceRefresh();
            }
        }

        gameObject.SetActive(false);
        ManagerAudio.instance.PlaySFX("TechTreeLearn");
    }

    public void OnBack()
    {
        gameObject.SetActive(false);
    }

    public void OnInfo()
    {
        if (!gameObject.activeSelf) return;

        Invoke(nameof(DoOpenInfo), 0.01f);
    }

    void DoOpenInfo()
    {
        if (selectedNode != null && TechTreeUI.instance != null)
        {
            TechTreeUI.instance.OpenInfoPopUp(selectedNode);
        }
    }
}