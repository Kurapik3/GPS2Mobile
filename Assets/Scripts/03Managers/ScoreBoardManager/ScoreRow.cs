using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreRow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI scoreText;

    public void SetData(string label, int score, Sprite icon)
    {
        nameLabel.text = label;
        scoreText.text = score.ToString();
        iconImage.sprite = icon;
    }
}
