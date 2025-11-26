using System.Data;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitButtonStatus : MonoBehaviour
{
    [Header("Button States")]
    public Button unitButton;
    public Image bgCircle;
    public Image lockIcon;
    public TextMeshProUGUI apCostText;

    [Header("Visual States")]
    public Color lockedColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
    public Color notEnoughAPColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
    public Color availableColor = new Color(0.2f, 0.8f, 0.9f, 0.9f);

    [Header("Cost & Requirements")]
    public int apCost;
    public string techName;

    private TechTree techTree;
    private PlayerTracker playerTracker;

    private void Awake()
    {
        if (unitButton == null) unitButton = GetComponent<Button>();
        if (bgCircle == null) bgCircle = GetComponent<Image>();

        playerTracker = FindAnyObjectByType<PlayerTracker>();
        techTree = FindAnyObjectByType<TechTree>();

        UpdateStatus();
    }

    public void UpdateStatus()
    {
        if (playerTracker == null || techTree == null) return;

        bool isUnlocked = IsTechUnlocked();
        bool hasEnoughAP = playerTracker.getAp() >= apCost;

        unitButton.interactable = isUnlocked && hasEnoughAP;

        if (!isUnlocked)
        {
            bgCircle.color = lockedColor;
            if (lockIcon != null) lockIcon.gameObject.SetActive(true);
            apCostText.color = Color.white;
        }
        else if (!hasEnoughAP)
        {
            bgCircle.color = notEnoughAPColor;
            if (lockIcon != null) lockIcon.gameObject.SetActive(false);
            apCostText.color = Color.white;
        }
        else
        {
            bgCircle.color = availableColor;
            if (lockIcon != null) lockIcon.gameObject.SetActive(false);
            apCostText.color = Color.black;
        }
    }

    private bool IsTechUnlocked()
    {
        if (techTree == null) return false;

        switch (techName.ToLower())
        {
            case "scouting": return techTree.IsScouting;
            case "armor": return techTree.IsArmor;
            case "shooter": return techTree.IsShooter;
            case "navalwarfare": return techTree.IsNavalWarfare;
            case "builder": return true;
            default: return false;
        }
    }
}
