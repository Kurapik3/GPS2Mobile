using UnityEngine;

public class FishCount : MonoBehaviour
{
    void Start()
    {
        FishSelection.instance.allFishList.Add(gameObject);
    }

    private void OnDestroy()
    {
        FishSelection.instance.allFishList.Remove(gameObject);
    }
}
