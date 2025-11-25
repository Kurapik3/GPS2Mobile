using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    public TutorialStage currentStep;

    private TutorialStage lastStep; 

    private void Awake()
    {
        Instance = this;
        lastStep = currentStep; 
    }

    private void Update()
    {
        if (currentStep != lastStep)
        {
            UpdateUI();
            lastStep = currentStep;
        }
    }

    private void UpdateUI()
    {
        if (TutorialUI.instance != null)
        {
            TutorialUI.instance.UpdateNotification(currentStep);
        }
    }
}
