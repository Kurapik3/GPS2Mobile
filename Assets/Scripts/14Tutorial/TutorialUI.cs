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

        }
    }
}

