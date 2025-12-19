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
    void Start()
    {
        var mapGen = FindObjectOfType<MapGenerator>();
        var fogSys = FindObjectOfType<FogSystem>();

        // Force map ready
        mapGen.RebuildTileDictionary(); // ensures MapManager is populated
        fogSys.mapReady = true; // bypass event dependency

        // Manually init fog
        fogSys.InitializeFog();
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
