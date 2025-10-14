using TMPro;
using UnityEngine;

public class HUDDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI turnText;

    private void OnEnable()
    {
        EventBus.Subscribe<TurnUpdatedEvent>(OnTurnUpdated);
    }
    private void OnDisable()
    {
        EventBus.Unsubscribe<TurnUpdatedEvent>(OnTurnUpdated);
    }
    private void OnTurnUpdated(TurnUpdatedEvent evt)
    {
        turnText.text = $"{evt.currentTurn}/{evt.maxTurns}";
    }
}
