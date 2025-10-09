using DG.Tweening;
using UnityEngine;
using EasyTransition;

public class InGameSceneManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup settingButton;
    [SerializeField] private CanvasGroup techTreeButton;
    [SerializeField] private CanvasGroup endTurnButton;
    [SerializeField] private CanvasGroup unitInteractable;

    [SerializeField] private RectTransform settingPanelMove;

    [SerializeField] private Ease easing = Ease.InOutBack;
    [SerializeField] private TransitionSettings transition;

    [SerializeField] private float moveDuration = 1f;

    private Vector2 centrePos;
    private Vector2 offScreenPos;

    private void Start()
    {
        centrePos = Vector2.zero;

        offScreenPos = new Vector2(0, -Screen.height);
        settingPanelMove.anchoredPosition = offScreenPos;
    }

    public void Setting()
    {
        SettingPopUp();
        unitInteractable.interactable = false;
        ManagerAudio.instance.PlaySFX("ButtonPressed");
    }

    public void CloseSetting()
    {
        settingPanelMove.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.OutBack);
        unitInteractable.interactable = true;
        ManagerAudio.instance.PlaySFX("ButtonPressed");
    }

    public void MainMenu()
    {
        LoadMainMenu("MainMenu");

        ManagerAudio.instance.PlaySFX("ButtonPressed");
    }

    private void SettingPopUp()
    {
        settingPanelMove.DOAnchorPos(centrePos, moveDuration).SetEase(Ease.OutBack);
    }

    public void LoadMainMenu(string sceneName)
    {
        TransitionManager.Instance().Transition(sceneName, transition, 0.1f);
    }
}
