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

        // Determine which bar set to show and how many bars to fill
        switch (currentLevel)
        {
            case 1:
                level1BarSet?.SetActive(true);
                UpdateBarFill(level1Bars, currentPop, treeBase.popForLvl2);
                break;
            case 2:
                level2BarSet?.SetActive(true);
                UpdateBarFill(level2Bars, currentPop, treeBase.popForLvl3);
                break;
            case 3:
                level3BarSet?.SetActive(true);
                UpdateBarFill(level3Bars, currentPop, treeBase.popForLvlMore);
                break;
            //default:
            //    // Fallback to Level 1 if level is invalid
            //    level1BarSet?.SetActive(true);
            //    FillBars(level1Bars, treeBase.currentPop);
            //    break;
        }

        Debug.Log($"[LevelProgress] Current Level: {treeBase.level}, Pop: {treeBase.currentPop}");
    }

    private void UpdateBarFill(Image[] bars, int currentPop, int requiredPop)
    {
        Debug.Log($"[UpdateBarFill] currentPop={currentPop}, bars.Length={bars?.Length ?? 0}");

        if (bars == null || bars.Length == 0)
        {
            Debug.LogError("No bars assigned!");
            return;
        }

        // Calculate how many full bars should be filled
        int fullBars = Mathf.Min(bars.Length, currentPop); // Assuming 1 pop = 1 bar fill
        Debug.Log($"[UpdateBarFill] Filling {fullBars} bars out of {bars.Length}");

        // Reset all bars to inactive color
        foreach (Image bar in bars)
        {
            if (bar == null)
            {
                Debug.LogError("A bar is null!");
                continue;
            }
            bar.color = inactiveColor;
            Debug.Log($"[UpdateBarFill] Reset bar color to {inactiveColor}");
        }

        // Fill the appropriate number of bars
        for (int i = 0; i < fullBars && i < bars.Length; i++)
        {
            if (bars[i] != null)
            {
                bars[i].color = activeColor;
                Debug.Log($"[UpdateBarFill] Set bar {i} to {activeColor}");
            }
        }

    }

    private void FillBars(Image[] bars, int pop)
    {
        if (bars == null) return;
        for (int i = 0; i < bars.Length; i++)
        {
            bars[i].color = (i < pop) ? activeColor : inactiveColor;
        }
    }
}
