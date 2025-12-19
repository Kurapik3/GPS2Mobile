using UnityEngine;
using UnityEngine.UI;

public class TutorialButtons : MonoBehaviour
{
    [SerializeField] private GameObject TutorialPanel;

    [Header("button")]
    [SerializeField] private Button ReturnButton;

    public HexTile currentTile;

    public void ClickReturn()
    {
        TutorialPanel.SetActive(false);

        if (MapManager.Instance == null)
        {
            Debug.LogError("[TutorialButtons] MapManager not found!");
            return;
        }

        currentTile = MapManager.Instance.GetTile(new Vector2Int(0, 0));

        if (currentTile == null)
        {
            Debug.LogError("[TutorialButtons] HexTile (0,0) not found!");
            return;
        }

        TurfManager.Instance.AddTurfArea(currentTile, 2);
        Debug.Log("[TutorialButtons] Turf added at (0,0)");
    }
}
