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

    public event System.Action OnTechResearched;

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
            case "metal scrap": return IsFishing && !IsMetalScraps;
            case "armor": return IsMetalScraps && !IsArmor;
            case "scouting": return !IsScouting;
            case "camouflage": return IsScouting && !IsCamouflage;
            case "clear sight": return IsCamouflage && !IsClearSight;
            case "home defense": return !IsHomeDef;
            case "shooter unit": return IsHomeDef && !IsShooter;
            case "naval warfare": return IsShooter && !IsNavalWarfare;
            case "mob research": return !IsCreaturesResearch;
            case "mutualism": return IsCreaturesResearch && IsScouting && !IsMutualism;
            case "hunter's mark": return IsCreaturesResearch && !IsHunterMask;
            case "taming": return IsMutualism && !IsTaming;
            default: return false;
        }
    }

    private void Start()
    {
        //if (IsTaming)
        //{
        //    EventBus.Publish(new SeaMonsterEvents.TamingUnlockedEvent());
        //}
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
            case "metal scrap":
                MetalScraps(cost);
                return true;
            case "armor":
                Armor(cost);
                return true;
            case "scouting":
                Scouting(cost);
                return true;
            case "camoflage":
                Camouflage(cost);
                return true;
            case "clear sight":
                ClearSight(cost);
                return true;
            case "home defense":
                HomeDefense(cost);
                return true;
            case "shooter unit":
                Shooter(cost);
                return true;
            case "naval warfare":
                NavalWarfare(cost);
                return true;
            case "mob research":
                MobResearch(cost);
                return true;
            case "mutualism":
                Mutualism(cost);
                return true;
            case "hunter's mark":
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
            OnTechResearched?.Invoke();
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
            OnTechResearched?.Invoke();
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
            foreach (var unit in UnitManager.Instance.GetAllUnits())
            {
                unit.fogRevealRadius = 2;
                //if (unit.currentTile != null)
                //    unit.RevealNearbyFog(unit.currentTile);
            }
        }
        else
        {
            Debug.Log("Not enough AP, prerequisites not met, or already unlocked!");
        }
    }

    private void HomeDefense(int cost)
    {
        if (!IsHomeDef && player.getAp() >= cost)
        {
            player.useAP(cost);
            IsHomeDef = true;

            foreach (var treeBase in FindObjectsOfType<TreeBase>())
            {
                treeBase.ApplyHomeDefenseBonus();
            }
            //foreach (var unit in UnitManager.Instance.GetAllUnits())
            //{
            //    if (unit.currentTile != null && unit.currentTile.HasTurf)
            //    {
            //        unit.IsInTurf = true; // Units take 1 less damage in turf
            //    }
            //}

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

    private void MobResearch(int cost)
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
        if (IsCreaturesResearch && !IsHunterMask && player.getAp() >= cost)
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
        if (IsMutualism && !IsTaming && player.getAp() >= cost)
        {
            player.useAP(cost);
            IsTaming = true;

            EventBus.Publish(new SeaMonsterEvents.TamingUnlockedEvent());
        }
        else
        {
            Debug.Log("Not enough AP, prerequisites not met, or already unlocked!");
        }
    }
    public void RestoreFromSave(GameSaveData.TechTreeSave saved)
    {
        IsFishing = saved.IsFishing;
        IsMetalScraps = saved.IsMetalScraps;
        IsArmor = saved.IsArmor;

        IsScouting = saved.IsScouting;
        IsCamouflage = saved.IsCamouflage;
        IsClearSight = saved.IsClearSight;

        IsHomeDef = saved.IsHomeDef;
        IsShooter = saved.IsShooter;
        IsNavalWarfare = saved.IsNavalWarfare;

        IsCreaturesResearch = saved.IsCreaturesResearch;
        IsMutualism = saved.IsMutualism;
        IsHunterMask = saved.IsHunterMask;
        IsTaming = saved.IsTaming;

        // Fire side-effects manually
        if (IsClearSight)
        {
            foreach (var unit in UnitManager.Instance.GetAllUnits())
                unit.fogRevealRadius = 2;
        }

        if (IsHomeDef)
        {
            foreach (var treeBase in FindObjectsOfType<TreeBase>())
                treeBase.ApplyHomeDefenseBonus();
        }

        if (IsTaming)
            EventBus.Publish(new SeaMonsterEvents.TamingUnlockedEvent());

        OnTechResearched?.Invoke();
    }

}

