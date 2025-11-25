using UnityEngine;
using UnityEngine.UI;
public class TutorialUI : MonoBehaviour
{
    public static TutorialUI instance;

    [Header("UI Elements")]
    [SerializeField] private Image icon;
    [SerializeField] private Text Text;

    [Header("Icons")]
    [SerializeField] private Sprite TechTree;
    [SerializeField] private Sprite UnlockFishing;
    [SerializeField] private Sprite TapTree;
    [SerializeField] private Sprite BuildUnit;
    [SerializeField] private Sprite Endturn;
    [SerializeField] private Sprite MoveUnit;
    [SerializeField] private Sprite WowGrove;

    private void Awake()
    {
        instance = this;
    }


    public void UpdateNotification(TutorialStage step)
    {
        switch (step)
        {
            case TutorialStage.TechTree:
                icon.sprite = TechTree;
                Text.text = "Research Fishing in tech Tree and Upgrade your tree base";
                break;
            case TutorialStage.UnlockFishing:
                icon.sprite = UnlockFishing;
                Text.text = "Extract the Fish to upgrade your tree base";
                break;
            case TutorialStage.TapTree:
                icon.sprite = TapTree;
                Text.text = "Tap on the Tree Base";
                break;
            case TutorialStage.BuildUnit:
                icon.sprite = BuildUnit;
                Text.text = "Train a Builder Unit";
                break;
            case TutorialStage.Endturn:
                icon.sprite = Endturn;
                Text.text = "Now End Your Turn";
                break;
            case TutorialStage.MoveUnit:
                icon.sprite = MoveUnit;
                Text.text = "Move your Unit to reveal some fog";
                break;
            case TutorialStage.WowGrove:
                icon.sprite = WowGrove;
                Text.text = " Wow, a grove! Send Builder to build base. ";
                break;
        }
    }
}

