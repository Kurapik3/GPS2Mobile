using UnityEngine;

public class EnemyTracker : MonoBehaviour
{
    public static EnemyTracker Instance { get; private set; }

    [SerializeField] public int currentScore = 0;

    public static event System.Action OnScoreChanged;

    private void Awake()
    {
        Instance = this;
    }

    public int GetScore()
    {
        return currentScore;
    }

    public void AddScore(int amount)
    {
        currentScore += amount;

        OnScoreChanged?.Invoke();
    }
}
