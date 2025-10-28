using DG.Tweening;
using UnityEngine;
//using EasyTransition;
using UnityEngine.SceneManagement;

public class InGameSceneManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup settingButton;
    [SerializeField] private CanvasGroup techTreeButton;
    [SerializeField] private CanvasGroup endTurnButton;
    [SerializeField] private CanvasGroup interactablePanel;
    [SerializeField] private CanvasGroup techTreePage;

    [SerializeField] private RectTransform settingPanelMove;
    [SerializeField] private RectTransform tribeStatsPanel;

    [SerializeField] private Ease easing = Ease.InOutBack;
    //[SerializeField] private TransitionSettings transition;

    [SerializeField] private float moveDuration = 1f;
    [SerializeField] private float fastMoveDuration = 0.5f;
    [SerializeField] private float fadeDuration = 0.5f;

    [SerializeField] private Vector2 centrePos = new Vector2(0, 0);
    [SerializeField] private Vector2 offScreenPos = new Vector2(0, -1000);

    private void Start()
    {
        ManagerAudio.instance.PlayMusic("BGM");
        centrePos = Vector2.zero;

        offScreenPos = new Vector2(0, -Screen.height);
        settingPanelMove.anchoredPosition = offScreenPos;
    }

    public void Setting()
    {
        SettingPopUp();
        interactablePanel.blocksRaycasts = true;
        ManagerAudio.instance.PlaySFX("ButtonPressed");
    }

    public void CloseSetting()
    {
        settingPanelMove.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.OutBack);
        interactablePanel.blocksRaycasts = false;
        ManagerAudio.instance.PlaySFX("ButtonPressed");
    }

    public void MainMenu()
    {
        //LoadMainMenu("MainMenu");

        SceneManager.LoadScene("MainMenu");

        ManagerAudio.instance.PlaySFX("ButtonPressed");
    }

    private void SettingPopUp()
    {
        settingPanelMove.DOAnchorPos(centrePos, moveDuration).SetEase(Ease.OutBack);
    }

    //public void LoadMainMenu(string sceneName)
    //{
    //    TransitionManager.Instance().Transition(sceneName, transition, 0.1f);
    //}

    private void TribeStatsPopUp()
    {
        tribeStatsPanel.DOAnchorPos(centrePos, fastMoveDuration).SetEase(Ease.OutBack);
    }

    private void CloseTribeStatsPopUp()
    {
        tribeStatsPanel.DOAnchorPos(offScreenPos, fastMoveDuration).SetEase(Ease.OutBack);
    }

    public void OpenTribeStats()
    {
        TribeStatsPopUp();
        interactablePanel.blocksRaycasts = true;
        ManagerAudio.instance.PlaySFX("ButtonPressed");
    }

    public void ClosetribeStats()
    {
        CloseTribeStatsPopUp();
        interactablePanel.blocksRaycasts = false;
        ManagerAudio.instance.PlaySFX("ButtonPressed");
    }

    private void TechTreeFadeIn()
    {
        techTreePage.DOFade(1, fadeDuration);
    }

    private void TechTreeFadeOut()
    {
        techTreePage.DOFade(0, fadeDuration);
    }

    public void OpenTechTree()
    {
        TechTreeFadeIn();
        techTreePage.interactable = true;
        techTreePage.blocksRaycasts = true;
        ManagerAudio.instance.PlaySFX("ButtonPressed");
    }

    public void CloseTechTree()
    {
        TechTreeFadeOut();
        techTreePage.interactable = false;
        techTreePage.blocksRaycasts = false;
        ManagerAudio.instance.PlaySFX("ButtonPressed");
    }
}
