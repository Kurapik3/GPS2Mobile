using TMPro;
using UnityEngine;

public class InfoPopUp : MonoBehaviour
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

    public void OnBack()
    {
        gameObject.SetActive(false);
    }
}
