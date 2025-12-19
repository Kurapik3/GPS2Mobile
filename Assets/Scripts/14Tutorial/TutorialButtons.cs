using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialButtons : MonoBehaviour
{
    public static TutorialButtons instance;

    [SerializeField] private GameObject TutorialPanel;
    [SerializeField] private GameObject TutorialPanel2;

    [Header("button")]
    [SerializeField] private Button ReturnButton;

    public HexTile currentTile;

    public void ClickReturn()
    {
        ManagerAudio.instance.PlaySFX("ButtonPressed");
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

    public void lastPart()
    {
        TutorialPanel2.SetActive(true);
    }

    public void clickNext()
    {
        TutorialPanel2.SetActive(false);
        ManagerAudio.instance.PlaySFX("ButtonPressed");

        PlayerPrefs.SetInt("TutorialComplete", 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene("PrototypeScene");
    }


}
