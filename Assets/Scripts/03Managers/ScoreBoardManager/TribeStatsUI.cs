using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class TribeStatsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI titleText; // "Tribe Stats"
    [SerializeField] private Transform contentParent; // Parent for score rows
    [SerializeField] private GameObject scoreRowPrefab; // Prefab for each row (You, Enemy 1, etc.)

    [Header("Icons")]
    [SerializeField] private Sprite playerIcon; // Your tribe icon (e.g., blue circle)
    [SerializeField] private Sprite enemyIcon; // Enemy tribe icon (e.g., pink skull)

    private List<ScoreRow> scoreRows = new List<ScoreRow>();

    private void Awake()
    {
        Hide(); 
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

    // This function is called externally (e.g., from a button's OnClick) to show the panel
    public void Show()
    {
        gameObject.SetActive(true);
        UpdateScoreboard(); // Ensure scores are current
    }

    // This function is called externally (e.g., from a close button's OnClick) to hide the panel
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

    // Helper struct to hold score data for sorting
    private struct ScoreEntry
    {
        public string label;
        public int score;
        public Sprite icon;
    }
}
