using UnityEngine;

public class TechTree : MonoBehaviour
{
    public static TechTree Instance { get; private set; }

    [SerializeField] private PlayerTracker player;

    [Header("Development Branch Conditions")]
    [SerializeField] public bool IsFishing = false;
    [SerializeField] public bool IsMetalScraps = false;
    [SerializeField] public bool IsArmor = false;

    [Header("Development Navigation")]
    [SerializeField] public bool IsScouting = false;
    [SerializeField] public bool IsCamouflage = false;
    [SerializeField] public bool IsClearSight = false;

    [Header("Development Combat")]
    [SerializeField] public bool IsHomeDef = false;
    [SerializeField] public bool IsShooter = false;
    [SerializeField] public bool IsNavalWarfare = false;

    [Header("Sea Creatures")]
    [SerializeField] public bool IsCreaturesResearch = false;
    [SerializeField] public bool IsMutualism = false;
    [SerializeField] public bool IsHunterMask = false;
    [SerializeField] public bool IsTaming = false;

    public static TechTree instance { get; private set; } // Keep the lowercase version too

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        instance = this; // Also assign to lowercase version
    }

    // Check if a tech can be unlocked (prerequisites only)
    public bool CanUnlockTech(string techName)
    {
        switch (techName.ToLower())
        {
            case "fishing": return !IsFishing;
            case "metalscraps": return IsFishing && !IsMetalScraps;
            case "armor": return IsMetalScraps && !IsArmor;
            case "scouting": return !IsScouting;
            case "camouflage": return IsScouting && !IsCamouflage;
            case "clearsight": return IsCamouflage && !IsClearSight;
            case "homedef": return !IsHomeDef;
            case "shooter": return IsHomeDef && !IsShooter;
            case "navalwarfare": return IsShooter && !IsNavalWarfare;
            case "creaturesresearch": return !IsCreaturesResearch;
            case "mutualism": return IsCreaturesResearch && !IsMutualism;
            case "huntermask": return IsMutualism && !IsHunterMask;
            case "taming": return IsHunterMask && !IsTaming;
            default: return false;
        }
    }

    // Main unlock method that checks both prerequisites and AP
    public bool UnlockTech(string techName, int cost)
    {
        if (player == null)
        {
            Debug.LogError("[TechTree] PlayerTracker not assigned!");
            return false;
        }

        if (player.getAp() < cost)
        {
            Debug.Log("Not enough AP!");
            return false;
        }

        if (!CanUnlockTech(techName))
        {
            Debug.Log($"Cannot unlock {techName} - prerequisites not met!");
            return false;
        }

        // Call the specific unlock method based on tech name
        switch (techName.ToLower())
        {
            case "fishing":
                Fishing(cost);
                return true;
            case "metalscraps":
                MetalScraps(cost);
                return true;
            case "armor":
                Armor(cost);
                return true;
            case "scouting":
                Scouting(cost);
                return true;
            case "camouflage":
                Camouflage(cost);
                return true;
            case "clearsight":
                ClearSight(cost);
                return true;
            case "homedef":
                HomeDef(cost);
                return true;
            case "shooter":
                Shooter(cost);
                return true;
            case "navalwarfare":
                NavalWarfare(cost);
                return true;
            case "creaturesresearch":
                CreaturesResearch(cost);
                return true;
            case "mutualism":
                Mutualism(cost);
                return true;
            case "huntermask":
                HunterMask(cost);
                return true;
            case "taming":
                Taming(cost);
                return true;
            default:
                Debug.LogError($"Unknown tech: {techName}");
                return false;
        }
    }

    // Keep your original methods but make them private/internal
    private void Fishing(int cost)
    {
        if (!IsFishing && player.getAp() >= cost)
        {
            Debug.Log("Fishing unlocked!");
            player.useAP(cost);
            IsFishing = true;
        }
        else
        {
            Debug.Log("Not enough AP or already unlocked!");
        }
    }

    private void MetalScraps(int cost)
    {
        if (IsFishing && !IsMetalScraps && player.getAp() >= cost)
        {
            player.useAP(cost);
            IsMetalScraps = true;
        }
        else
        {
            Debug.Log("Not enough AP, prerequisites not met, or already unlocked!");
        }
    }

    private void Armor(int cost)
    {
        if (IsMetalScraps && !IsArmor && player.getAp() >= cost)
        {
            player.useAP(cost);
            IsArmor = true;
        }
        else
        {
            Debug.Log("Not enough AP, prerequisites not met, or already unlocked!");
        }
    }

    private void Scouting(int cost)
    {
        if (!IsScouting && player.getAp() >= cost)
        {
            player.useAP(cost);
            IsScouting = true;
        }
        else
        {
            Debug.Log("Not enough AP or already unlocked!");
        }
    }

    private void Camouflage(int cost)
    {
        if (IsScouting && !IsCamouflage && player.getAp() >= cost)
        {
            player.useAP(cost);
            IsCamouflage = true;
        }
        else
        {
            Debug.Log("Not enough AP, prerequisites not met, or already unlocked!");
        }
    }

    private void ClearSight(int cost)
    {
        if (IsCamouflage && !IsClearSight && player.getAp() >= cost)
        {
            player.useAP(cost);
            IsClearSight = true;
        }
        else
        {
            Debug.Log("Not enough AP, prerequisites not met, or already unlocked!");
        }
    }

    private void HomeDef(int cost)
    {
        if (!IsHomeDef && player.getAp() >= cost)
        {
            player.useAP(cost);
            IsHomeDef = true;
        }
        else
        {
            Debug.Log("Not enough AP or already unlocked!");
        }
    }

    private void Shooter(int cost)
    {
        if (IsHomeDef && !IsShooter && player.getAp() >= cost)
        {
            player.useAP(cost);
            IsShooter = true;
        }
        else
        {
            Debug.Log("Not enough AP, prerequisites not met, or already unlocked!");
        }
    }

    private void NavalWarfare(int cost)
    {
        if (IsShooter && !IsNavalWarfare && player.getAp() >= cost)
        {
            player.useAP(cost);
            IsNavalWarfare = true;
        }
        else
        {
            Debug.Log("Not enough AP, prerequisites not met, or already unlocked!");
        }
    }

    private void CreaturesResearch(int cost)
    {
        if (!IsCreaturesResearch && player.getAp() >= cost)
        {
            player.useAP(cost);
            IsCreaturesResearch = true;
        }
        else
        {
            Debug.Log("Not enough AP or already unlocked!");
        }
    }

    private void Mutualism(int cost)
    {
        if (IsCreaturesResearch && !IsMutualism && player.getAp() >= cost)
        {
            player.useAP(cost);
            IsMutualism = true;
        }
        else
        {
            Debug.Log("Not enough AP, prerequisites not met, or already unlocked!");
        }
    }

    private void HunterMask(int cost)
    {
        if (IsMutualism && !IsHunterMask && player.getAp() >= cost)
        {
            player.useAP(cost);
            IsHunterMask = true;
        }
        else
        {
            Debug.Log("Not enough AP, prerequisites not met, or already unlocked!");
        }
    }

    private void Taming(int cost)
    {
        if (IsHunterMask && !IsTaming && player.getAp() >= cost)
        {
            player.useAP(cost);
            IsTaming = true;
        }
        else
        {
            Debug.Log("Not enough AP, prerequisites not met, or already unlocked!");
        }
    }
}

