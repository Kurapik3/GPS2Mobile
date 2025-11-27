using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TribeStatsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI titleText; 
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject scoreRowPrefab;
    [SerializeField] private Button closeButton; 
    [SerializeField] private Button homeButton; 

    [Header("Icons")]
    [SerializeField] private Sprite playerIcon; 
    [SerializeField] private Sprite enemyIcon;

    [Header("End Game Settings")]
    [SerializeField] private Sprite victoryIcon;
    [SerializeField] private Sprite defeatIcon;

    private List<ScoreRow> scoreRows = new List<ScoreRow>();
    private bool isEndGameMode = false;

    private void Awake()
    {
        Hide();

        homeButton.gameObject.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        if (homeButton != null)
            homeButton.onClick.AddListener(OnHomeClicked);
    }

    private void OnEnable()
    {
        // Subscribe to score changes only when panel is active for performance
        PlayerTracker.OnScoreChanged += UpdateScoreboard;
        EnemyTracker.OnScoreChanged += UpdateScoreboard;
        UpdateScoreboard(); // Update immediately when panel is shown
    }

    private void OnDisable()
    {
        // Unsubscribe when panel is hidden
        PlayerTracker.OnScoreChanged -= UpdateScoreboard;
        EnemyTracker.OnScoreChanged -= UpdateScoreboard;
    }

    // Call this from GameManager to show the end-game screen
    public void ShowAsEndGameResult(bool isVictory)
    {
        Debug.Log($"[ScoreboardPanel] ShowAsEndGameResult called with isVictory: {isVictory}");

        isEndGameMode = true;
        gameObject.SetActive(true);

        // Set title based on result
        if (isVictory)
        {
            titleText.text = "Victory";
            Debug.Log("[ScoreboardPanel] Title set to 'Victory'");
            // Optional: Set an icon next to the title if you have a TitleIcon Image component
            // titleIcon.sprite = victoryIcon;
        }
        else
        {
            titleText.text = "Defeat";
            Debug.Log("[ScoreboardPanel] Title set to 'Defeat'");
            // Optional: Set an icon next to the title
            // titleIcon.sprite = defeatIcon;
        }

        // Hide the close button in end-game mode (optional)
        if (closeButton != null)
            closeButton.gameObject.SetActive(false);

        if (homeButton != null)
            homeButton.gameObject.SetActive(true);

        UpdateScoreboard(); // Ensure scores are current
    }

    // Call this to show the regular scoreboard during gameplay
    public void ShowAsRegularScoreboard()
    {
        isEndGameMode = false;
        gameObject.SetActive(true);
        titleText.text = "Tribe Stats";

        // Show the close button
        if (closeButton != null)
            closeButton.gameObject.SetActive(true);

        if (homeButton != null)
            homeButton.gameObject.SetActive(false);

        UpdateScoreboard();
    }

    // This function is called externally (e.g., from a button's OnClick) to show the panel
    public void Show()
    {
        gameObject.SetActive(true);
        UpdateScoreboard(); // Ensure scores are current
    }

    //This function is called externally(e.g., from a close button's OnClick) to hide the panel
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void UpdateScoreboard()
    {
        // Get current scores
        int playerScore = PlayerTracker.Instance?.getScore() ?? 0;
        int enemyScore = EnemyTracker.Instance?.GetScore() ?? 0;

        // Create a list of all scores with their labels and icons
        var scores = new List<ScoreEntry>
        {
            new ScoreEntry { label = "You", score = playerScore, icon = playerIcon },
            new ScoreEntry { label = "Enemy", score = enemyScore, icon = enemyIcon }
        };

        // Sort by score descending (highest first)
        scores = scores.OrderByDescending(s => s.score).ToList();

        // Clear existing rows
        foreach (var row in scoreRows)
        {
            Destroy(row.gameObject);
        }
        scoreRows.Clear();

        // Create new rows
        foreach (var entry in scores)
        {
            GameObject rowObj = Instantiate(scoreRowPrefab, contentParent);
            ScoreRow row = rowObj.GetComponent<ScoreRow>();
            row.SetData(entry.label, entry.score, entry.icon);
            scoreRows.Add(row);
        }
    }

    private void OnHomeClicked()
    {
        Debug.Log("Home button clicked. Implement logic to return to main menu or restart.");
        SceneManager.LoadScene("MainMenu");
    }

    // Helper struct to hold score data for sorting
    private struct ScoreEntry
    {
        public string label;
        public int score;
        public Sprite icon;
    }
}
