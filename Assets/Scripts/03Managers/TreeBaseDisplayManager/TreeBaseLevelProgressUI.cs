using UnityEngine;
using UnityEngine.UI;

public class TreeBaseLevelProgressUI : MonoBehaviour
{
    [Header("Segment Settings")]
    [SerializeField] private GameObject capsuleSegmentPrefab;  // BarFront
    [SerializeField] private GameObject rectangularSegmentPrefab;  // BarMiddle
    [SerializeField] private Color filledColor = new Color(0f, 1f, 1f); // Cyan
    [SerializeField] private Color emptyColor = Color.gray;

    [Header("Layout Settings")]
    [SerializeField] private float spacing = 2f;

    private TreeBase treeBase;
    private Transform container;

    void Awake()
    {
        treeBase = FindObjectOfType<TreeBase>();
        if (treeBase == null)
        {
            Debug.LogError("TreeBaseLevelProgress: Could not find TreeBase in scene!");
            return;
        }

        container = transform;
    }

    public void UpdateProgress()
    {
        if (container == null) return;

        // Clear old segments
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        int currentLevel = treeBase.level;
        int maxLevels = treeBase.maxUpgrades;

        // Start with 2 segments minimum
        int totalSegments = Mathf.Max(2, currentLevel + 1);

        for (int i = 0; i < totalSegments; i++)
        {
            GameObject segment;
            Vector3 position = new Vector3(i * (GetSegmentWidth() + spacing), 0, 0);

            if (i == 0 && totalSegments > 1)
            {
                // First segment (left cap)
                segment = Instantiate(capsuleSegmentPrefab, container);
                segment.GetComponent<Image>().color = (i < currentLevel) ? filledColor : emptyColor;
            }
            else if (i == totalSegments - 1 && totalSegments > 1)
            {
                // Last segment (right cap) — flipped
                segment = Instantiate(capsuleSegmentPrefab, container);
                segment.GetComponent<Image>().color = (i < currentLevel) ? filledColor : emptyColor;
                segment.transform.localScale = new Vector3(-1, 1, 1); // Flip horizontally
            }
            else
            {
                // Middle segment
                segment = Instantiate(rectangularSegmentPrefab, container);
                segment.GetComponent<Image>().color = (i < currentLevel) ? filledColor : emptyColor;
            }

            segment.transform.localPosition = position;
        }
    }

    private float GetSegmentWidth()
    {
        return 30f; // Adjust based on your sprite size
    }
}
