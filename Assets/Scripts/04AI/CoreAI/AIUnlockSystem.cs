//using System.Collections.Generic;
//using UnityEngine;

///// <summary>
///// - Handles enemy AI unlock progression based on turn number.
///// </summary>
//public class AIUnlockSystem : MonoBehaviour
//{
//    [Header("Unlock Settings")]
//    [Tooltip("Unlock Scout at this turn")]
//    [SerializeField] private int unlockScoutTurn = 3;

//    [Tooltip("Unlock Tanker at this turn")]
//    [SerializeField] private int unlockTankerTurn = 5;

//    [Tooltip("Unlock Shooter at this turn")]
//    [SerializeField] private int unlockShooterTurn = 8;

//    [Tooltip("Unlock Bomber at this turn")]
//    [SerializeField] private int unlockBomberTurn = 12;

//    private List<string> unlockedUnits = new();
//    private int lastCheckedTurn = -1; //Prevent re-checking the same turn

//    void Start()
//    {
//        //Unlock builder unit at start
//        unlockedUnits.Add("Builder");
//        Debug.Log("[AI Unlock] Builder unlocked at start");
//    }

//    //Called once per enemy turn to check for new unlocks
//    public void UpdateUnlocks(int currentTurn)
//    {
//        if (currentTurn == lastCheckedTurn)
//            return; //Skip if already processed this turn
//        lastCheckedTurn = currentTurn;

//        if (currentTurn >= unlockScoutTurn && !unlockedUnits.Contains("Scout"))
//        {
//            unlockedUnits.Add("Scout");
//            Debug.Log($"[AI Unlock] Turn {currentTurn}: Scout unlocked");
//        }

//        if (currentTurn >= unlockTankerTurn && !unlockedUnits.Contains("Tanker"))
//        {
//            unlockedUnits.Add("Tanker");
//            Debug.Log($"[AI Unlock] Turn {currentTurn}: Tanker unlocked");
//        }

//        if (currentTurn >= unlockShooterTurn && !unlockedUnits.Contains("Shooter"))
//        {
//            unlockedUnits.Add("Shooter");
//            Debug.Log($"[AI Unlock] Turn {currentTurn}: Shooter unlocked");
//        }

//        if (currentTurn >= unlockBomberTurn && !unlockedUnits.Contains("Bomber"))
//        {
//            unlockedUnits.Add("Bomber");
//            Debug.Log($"[AI Unlock] Turn {currentTurn}: Bomber unlocked");
//        }
//    }

//    //Returns all currently unlocked unit types
//    public List<string> GetUnlockedUnits()
//    {
//        return new List<string>(unlockedUnits);
//    }

//    //Checks if a specific unit type has been unlocked
//    public bool IsUnlocked(string unitType)
//    {
//        return unlockedUnits.Contains(unitType);
//    }
//}
