using UnityEngine;

public class PlayerTracker : MonoBehaviour
{
    public static PlayerTracker Instance { get; private set; }

    [Header ("Player Stats")]
    [SerializeField] public int currentAP=0;
    [SerializeField] public int currentScore=0;

    public static event System.Action OnScoreChanged;

    public event System.Action OnAPChanged;

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

        OnScoreChanged?.Invoke();
        OnAPChanged?.Invoke();
    }

    public void addAP(int amount)
    {
        currentAP += amount;
        Debug.Log($"[PlayerTracker] Gained {amount} AP. Total: {currentAP}");

        OnScoreChanged?.Invoke();
        OnAPChanged?.Invoke();
    }

    public void useAP(int amount)
    {
        currentAP -= amount;

        OnScoreChanged?.Invoke();
        OnAPChanged?.Invoke();
    }


}
