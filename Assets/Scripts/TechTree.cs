using UnityEngine;

public class TechTree : MonoBehaviour
{
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

    // Development branch
    public void Fishing(int cost)
    {
        if(IsFishing == false)
        {
            if (player.getAp() >= cost)
            {
                player.useAP(cost);
                IsFishing = true;
            }
            else
            {
                Debug.Log("Not enough AP!");
            }
        }
        
    }

    public void MetalScraps(int cost)
    {
        if (IsFishing == true)
        {
            if (player.getAp() >= cost)
            {
                player.useAP(cost);
                IsMetalScraps = true;
            }
            else
            {
                Debug.Log("Not enough AP!");
            }
        }
    }

    public void Armor(int cost) // unlock Tanker
    {
        if (IsMetalScraps == true)
        {
            if (player.getAp() >= cost)
            {
                player.useAP(cost);
                IsArmor = true;
            }
            else
            {
                Debug.Log("Not enough AP!");
            }
        }
    }

    // Navigation

    public void Scouting(int cost) // unlocks scout
    {
        if (IsScouting == false)
        {
            if (player.getAp() >= cost)
            {
                player.useAP(cost);
                IsScouting = true;
            }
            else
            {
                Debug.Log("Not enough AP!");
            }
        }
    }

    public void Camouflage(int cost)
    {
        if (IsScouting == true)
        {
            if (player.getAp() >= cost)
            {
                player.useAP(cost);
                IsCamouflage = true;
            }
            else
            {
                Debug.Log("Not enough AP!");
            }
        }
    }

    public void ClearSight(int cost)
    {
        if (IsCamouflage == true)
        {
            if (player.getAp() >= cost)
            {
                player.useAP(cost);
                IsClearSight = true;
            }
            else
            {
                Debug.Log("Not enough AP!");
            }
        }
        // is fogRaduis by 1 
    }

    // combat 

    public void HomeDef(int cost)
    {
        if (IsHomeDef == false)
        {
            if (player.getAp() >= cost)
            {
                player.useAP(cost);
                IsHomeDef = true;
            }
            else
            {
                Debug.Log("Not enough AP!");
            }
        }
    }

    public void Shooter(int cost) // unlock shooter
    {
        if (IsHomeDef == true)
        {
            if (player.getAp() >= cost)
            {
                player.useAP(cost);
                IsShooter = true;
            }
            else
            {
                Debug.Log("Not enough AP!");
            }
        }
    }

    public void NavalWarfare(int cost) // unlock Bomber
    {
        if (IsShooter == true)
        {
            if (player.getAp() >= cost)
            {
                player.useAP(cost);
                IsNavalWarfare = true;
            }
            else
            {
                Debug.Log("Not enough AP!");
            }
        }
    }

    // Sea Creatures

    public void CreaturesResearch(int cost)
    {
        if (IsCreaturesResearch == false)
        {
            if (player.getAp() >= cost)
            {
                player.useAP(cost);
                IsCreaturesResearch = true;
            }
            else
            {
                Debug.Log("Not enough AP!");
            }
        }
    }

    public void Mutualism(int cost)
    {
        if (IsCreaturesResearch == true)
        {
            if (player.getAp() >= cost)
            {
                player.useAP(cost);
                IsMutualism = true;
            }
            else
            {
                Debug.Log("Not enough AP!");
            }
        }
    }
    public void HunterMask(int cost)
    {
        if (IsMutualism == true)
        {
            if (player.getAp() >= cost)
            {
                player.useAP(cost);
                IsHunterMask = true;
            }
            else
            {
                Debug.Log("Not enough AP!");
            }
        }
    }
    public void Taming(int cost)
    {
        if (IsHunterMask == true)
        {
            if (player.getAp() >= cost)
            {
                player.useAP(cost);
                IsTaming = true;
            }
            else
            {
                Debug.Log("Not enough AP!");
            }
        }
    }
}

