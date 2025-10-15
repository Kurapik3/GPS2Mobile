using UnityEngine;

public class PlayerTracker : MonoBehaviour
{
    public static PlayerTracker Instance { get; private set; }

    [Header ("Player Stats")]
    [SerializeField] public int currentAP=0;
    [SerializeField] public int currentScore=0;

    private void Awake()
    {
        Instance = this;
    }

    public int getAp()
    {
        return currentAP;
    }

    public int getScore()
    {
        return currentScore;
    }
    public void addScore(int amount)
    {
        currentScore += amount;
    }

    public void addAP(int amount)
    {
        currentAP += amount;
        Debug.Log($"[PlayerTracker] Gained {amount} AP. Total: {currentAP}");
    }

    public void useAP(int amount)
    {
        currentAP -= amount;
    }


}
