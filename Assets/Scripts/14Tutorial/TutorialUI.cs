using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class TutorialUI : MonoBehaviour
{
    public static TutorialUI instance;

    [Header("UI Elements")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI Text;
    [SerializeField] private Button end;
    [SerializeField] private Button tribe;

    [Header("Icons")]
    [SerializeField] private Sprite TechTree;
    [SerializeField] private Sprite UnlockFishing;
    [SerializeField] private Sprite TapTree;
    [SerializeField] private Sprite BuildUnit;
    [SerializeField] private Sprite Endturn;
    [SerializeField] private Sprite MoveUnit;
    [SerializeField] private Sprite WowGrove;

    [Header("Condition")]
    [SerializeField] private bool afterFish = false;
    [SerializeField] private bool afterTree = false;
    [SerializeField] private bool afterEnd = false;


    [SerializeField] private float size;
    private void Awake()
    {
        instance = this;
        UpdateNotification(TutorialStage.TechTree);
    }


    public void UpdateNotification(TutorialStage step)
    {
        switch (step)
        {
            case TutorialStage.TechTree:
                icon.sprite = TechTree;
                Text.text = "Research Fishing in tech Tree and Upgrade your tree base";
                Text.fontSize = 18.94f;
                end.interactable = false;
                tribe.interactable = false;
                break;
            case TutorialStage.UnlockFishing:
                icon.sprite = UnlockFishing;
                Text.text = "Extract the Fish to upgrade your tree base";
                Text.fontSize = 20f;
                end.interactable = false;
                tribe.interactable = false; 
                afterFish = true;
                SelectionOfStructureManager.instance.afterFishing = true;
                break;
            case TutorialStage.TapTree:
                if(afterFish == true)
                {
                    icon.sprite = TapTree;
                    Text.text = "Tap on the Tree Base";
                    Text.fontSize = 32.34f;
                    end.interactable = false;
                    tribe.interactable = false;
                    afterTree = true;
                }
                break;
            case TutorialStage.BuildUnit:
                if(afterTree == true)
                {
                    icon.sprite = BuildUnit;
                    Text.text = "Train a Builder Unit";
                    Text.fontSize = 32.4f;
                    end.interactable = false;
                    tribe.interactable = false;
                }
                break;
            case TutorialStage.Endturn:
                icon.sprite = Endturn;
                Text.text = "Now End Your Turn";
                Text.fontSize = 32.5f;
                end.interactable = true;
                tribe.interactable = false;
                afterEnd = true;
                break;
            case TutorialStage.MoveUnit:
                if(afterEnd == true)
                {
                    icon.sprite = MoveUnit;
                    Text.text = "Move your Unit to reveal some fog";
                    Text.fontSize = 26.57f;
                    end.interactable = true;
                    tribe.interactable = true;
                }
                break;
            case TutorialStage.WowGrove:
                icon.sprite = WowGrove;
                Text.text = "Wow, a grove! Send Builder to build base.";
                Text.fontSize = 20.68f;
                end.interactable = true;
                tribe.interactable = true;
                break;
        }
    }
}

