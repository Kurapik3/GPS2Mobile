using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System.Runtime.CompilerServices;
using System;
//using EasyTransition;
using UnityEditor;

public class ManagerOfScene : MonoBehaviour
{
    public static ManagerOfScene instance;

    [SerializeField] private RectTransform title;
    [SerializeField] private RectTransform[] buttons;
    [SerializeField] private Ease easing = Ease.InOutBack;

    [SerializeField] private float moveDuration = 1f;

    [SerializeField] private RectTransform creditsAnimatedPanel;
    [SerializeField] private RectTransform settingsAnimatedPanel;

    //[SerializeField] private TransitionSettings transition;

    [SerializeField] private CanvasGroup settingButton;
    [SerializeField] private CanvasGroup creditsButton;

    [SerializeField] private GameObject newPlayerPanel;
    [SerializeField] private GameObject existingPlayerPanel;


    private Vector2 centrePos;
    private Vector2 offScreenPos;
     
    public static bool isGameStarted = false;

    private void Start()
    {
        centrePos = Vector2.zero;

        offScreenPos = new Vector2(0, -Screen.height);
        creditsAnimatedPanel.anchoredPosition = offScreenPos;
        settingsAnimatedPanel.anchoredPosition = offScreenPos;

        HandleMenuBasedOnSave();
    }

    //public void LoadNextScene(string sceneName)
    //{
    //    TransitionManager.Instance().Transition(sceneName, transition, 0.1f);

    //    settingButton.interactable = false;
    //    creditsButton.interactable = false;

    //    //settingButton.blocksRaycasts = false;
    //    //creditsButton.interactable = false;
    //}
    public void GameStart()
    {
        bool tutorialDone = PlayerPrefs.GetInt("TutorialComplete", 0) == 1;

        if (!tutorialDone)
        {

            SceneManager.LoadScene("TutorialScene");
            return;
        }

        SceneManager.LoadScene("PrototypeScene");
        ManagerAudio.instance.PlaySFX("ButtonPressed");
        ManagerAudio.instance.StopMusic();
    }

    public void ResumeGame()
    {
        EventBus.Publish(new LoadGameEvent());
        SceneManager.LoadScene("PrototypeScene");
        ManagerAudio.instance.PlaySFX("ButtonPressed");
        ManagerAudio.instance.StopMusic();
    }

    public void NewGame()
    {
        GameManager.Instance.ClearSave();
        SceneManager.LoadScene("PrototypeScene");
        ManagerAudio.instance.PlaySFX("ButtonPressed");
        ManagerAudio.instance.StopMusic();
    }


    public void Credits()
    {
        
        CreditsAppearPanel();
        ManagerAudio.instance.PlaySFX("ButtonPressed");
        
    }

    public void Settings()
    {
        
        SettingsAppearPanel();
        ManagerAudio.instance.PlaySFX("ButtonPressed");

    }

    public void CloseCreditsPopUp()
    {
        creditsAnimatedPanel.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.OutBack);
        ManagerAudio.instance.PlaySFX("ButtonPressed");
    }

    public void CloseSettingsPopUp()
    {
        settingsAnimatedPanel.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.OutBack);
        ManagerAudio.instance.PlaySFX("ButtonPressed");
    }

    private void CreditsAppearPanel()
    {
        creditsAnimatedPanel.DOAnchorPos(centrePos, moveDuration).SetEase(Ease.OutBack);
    }

    private void SettingsAppearPanel()
    {
        settingsAnimatedPanel.DOAnchorPos(centrePos, moveDuration).SetEase(Ease.OutBack);
    }
    private void HandleMenuBasedOnSave()
    {
        if (GameManager.Instance == null)
        {
            newPlayerPanel.SetActive(true);
            existingPlayerPanel.SetActive(false);
            return;
        }
        bool saveExists = GameManager.SaveExists();

        newPlayerPanel.SetActive(!saveExists);
        existingPlayerPanel.SetActive(saveExists);
    }

}
