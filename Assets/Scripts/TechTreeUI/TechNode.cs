using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TechNode : MonoBehaviour
{
    public string techName;
    public int costAP;
    public Image background;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public Button button;

    public void Setup(string name, int cost, Color color)
    {
        techName = name;
        costAP = cost;
        nameText.text = name;
        costText.text = cost + " AP";
        background.color = color;
    }

    public void OnClick()
    {
        //TechTreeUI.instance.OpenConfirmPopup(this);
    }
}
