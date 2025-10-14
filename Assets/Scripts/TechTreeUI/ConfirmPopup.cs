using TMPro;
using UnityEngine;

public class ConfirmPopup : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    private TechNode selectedNode;

    public void Setup(TechNode node)
    {
        selectedNode = node;
        titleText.text = $"{node.techName} ({node.costAP} AP)";
        descriptionText.text = "This Tech will enable the following:";
    }

    public void OnResearch()
    {
        // Deduct AP, unlock tech, etc.
        Debug.Log("Researched " + selectedNode.techName);
        gameObject.SetActive(false);
    }

    public void OnBack()
    {
        gameObject.SetActive(false);
    }
}
