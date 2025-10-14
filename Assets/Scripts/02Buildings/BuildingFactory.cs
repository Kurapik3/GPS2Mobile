using UnityEngine;

public class BuildingFactory : MonoBehaviour
{
    public static BuildingFactory Instance;

    [Header("Prefabs")]
    [SerializeField] public GameObject GrovePrefab;
    [SerializeField] public GameObject TreeBasePrefab;

    [Header("Building Data (ScriptableObjects or CSV)")]
    [SerializeField] public BuildingData GroveData;
    [SerializeField] public BuildingData TreeBaseData;

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
