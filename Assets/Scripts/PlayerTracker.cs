using UnityEngine;

public class PlayerTracker : MonoBehaviour
{
    [Header ("Player Stats")]
    [SerializeField] public int currentAP=0;
    [SerializeField] public int currentScore=0;

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
