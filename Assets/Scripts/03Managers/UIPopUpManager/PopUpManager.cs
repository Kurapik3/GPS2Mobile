using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static TMPro.Examples.TMP_ExampleScript_01;

[System.Serializable]
public class ObjectData
{
    public string objectName;
    public string description;
    public Sprite icon;
    public ObjectType objectType;
}
public enum ObjectType
{
    Fish,
    Debris
}

public class PopUpManager : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;

    [Header("Object-Specific Buttons")]
    public GameObject fishButtons; 
    public GameObject debrisButton; 

    private void Start()
    {
        HidePopup(); 
    }

    public void ShowPopup(ObjectData data)
    {
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
        }

        gameObject.SetActive(true);
    }

    public void HidePopup()
    {
        gameObject.SetActive(false);
        HideAllButtons();
    }

    private void HideAllButtons()
    {
        if (fishButtons != null) fishButtons.SetActive(false);
        if (debrisButton != null) debrisButton.SetActive(false);
    }
}



