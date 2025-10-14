using UnityEngine;

public class BuildingFactory : MonoBehaviour
{
    public static BuildingFactory Instance;

    [Header("Prefabs")]
    [SerializeField] private GameObject GrovePrefab;
    [SerializeField] private GameObject TreeBasePrefab;

    [Header("Building Data (ScriptableObjects or CSV)")]
    [SerializeField] private BuildingData GroveData;
    [SerializeField] private BuildingData TreeBaseData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
