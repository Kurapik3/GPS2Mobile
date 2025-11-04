using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI apText;
    [SerializeField] private TextMeshProUGUI scoreText;

    private void Update()
    {
        if (PlayerTracker.Instance == null)
            return;

        apText.text = $"{PlayerTracker.Instance.getAp()}";
        scoreText.text = $"{PlayerTracker.Instance.getScore()}";
    }
}
