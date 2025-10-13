using UnityEngine;

public class PlayerTracker : MonoBehaviour
{
    public static PlayerTracker Instance { get; private set; }

    [Header ("Player Stats")]
    [SerializeField] public int currentAP=0;
    [SerializeField] public int currentScore=0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional if you want it persistent
    }

    public void addScore(int amount)
    {
        currentScore += amount;
    }

    public void addAP(int amount)
    {
        currentAP += amount;
    }

    public void useAP(int amount)
    {
        currentAP -= amount;
    }


}
