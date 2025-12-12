using UnityEngine;
using UnityEngine.UI;

public class TreeBaseLevelProgressUI : MonoBehaviour
{
    [Header("Level Bar Sets (Set Active/Inactive)")]
    [SerializeField] private GameObject level1BarSet; // Contains 2 bars
    [SerializeField] private GameObject level2BarSet; // Contains 3 bars
    [SerializeField] private GameObject level3BarSet; // Contains 4 bars
    [SerializeField] private GameObject level4BarSet; // Contains 5 bars

    [Header("Bar Fill Images (Within Each Set)")]
    [SerializeField] private Image[] level1Bars; // Array of 2 Images
    [SerializeField] private Image[] level2Bars; // Array of 3 Images
    [SerializeField] private Image[] level3Bars; // Array of 4 Images
    [SerializeField] private Image[] level4Bars;


    [Header("Colors")]
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.cyan;

    [Header("References")]
    [SerializeField] private TreeBase treeBase; // Reference to the TreeBase script

    private void Start()
    {
        if (treeBase == null)
        {
            Debug.LogError("TreeBaseLevelProgressUI: TreeBase reference not set!");
            return;
        }

        // Subscribe to level changes (you'll need to trigger this from TreeBase)
        // This assumes you add an event or call UpdateProgress() from TreeBase
        // For now, we'll rely on the parent TreeBaseHPDisplay calling UpdateProgress()
        UpdateProgress();
    }

    public void UpdateProgress()
    {
        if (treeBase == null) return;

        int currentLevel = treeBase.level;
        int currentPop = treeBase.currentPop;

        // Hide all bar sets first
        level1BarSet?.SetActive(false);
        level2BarSet?.SetActive(false);
        level3BarSet?.SetActive(false);


        int requiredPop = currentLevel switch
        {
            1 => treeBase.popForLvl2,
            2 => treeBase.popForLvl3,
            _ => treeBase.popForLvlMore // for level 3  need popForLvlMore to reach 4
        };

        if (currentLevel == 1)
        {
            level1BarSet?.SetActive(true);
            UpdateBarFill(level1Bars, currentPop, treeBase.popForLvl2);
        }
        else if (currentLevel == 2)
        {
            level2BarSet?.SetActive(true);
            UpdateBarFill(level2Bars, currentPop, treeBase.popForLvl3);
        }
        else if (currentLevel == 3)
        {
            level3BarSet?.SetActive(true);
            int cappedPop = Mathf.Min(currentPop, treeBase.popForLvlMore);
            UpdateBarFill(level3Bars, cappedPop, treeBase.popForLvlMore);
        }
        else // Level 4 and beyond
        {
            level4BarSet?.SetActive(true);
            UpdateBarFill(level3Bars, currentPop, treeBase.popForLvlMore);
        }


        Debug.Log($"[LevelProgress] Current Level: {treeBase.level}, Pop: {treeBase.currentPop}");
    }

    private void FillBarsFull(Image[] bars)
    {
        if (bars == null) return;
        foreach (var bar in bars)
            bar.color = activeColor;
    }

    private void UpdateBarFill(Image[] bars, int current, int max)
    {

        if (bars == null || bars.Length == 0 || max <= 0)
        {
            Debug.LogWarning("[LevelProgress] Invalid bars or max value.");
            return;
        }

        float ratio = Mathf.Clamp01((float)current / max);
        int fullBars = Mathf.FloorToInt(ratio * bars.Length);

        for (int i = 0; i < bars.Length; i++)
        {
            bars[i].color = (i < fullBars) ? activeColor : inactiveColor;
        }

    }
}
