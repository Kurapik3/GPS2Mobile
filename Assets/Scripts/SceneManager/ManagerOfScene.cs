using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System.Runtime.CompilerServices;
using System;

public class ManagerOfScene : MonoBehaviour
{
    [SerializeField] private RectTransform title;
    [SerializeField] private RectTransform[] buttons;
    [SerializeField] private Ease easing = Ease.InOutBack;

    [SerializeField] private float moveDuration = 1f;

    [SerializeField] private RectTransform creditsAnimatedPanel;
    [SerializeField] private RectTransform settingsAnimatedPanel;

    private Vector2 centrePos;
    private Vector2 offScreenPos;


    private void Start()
    {
        centrePos = Vector2.zero;

        offScreenPos = new Vector2(0, -Screen.height);
        creditsAnimatedPanel.anchoredPosition = offScreenPos;
        settingsAnimatedPanel.anchoredPosition = offScreenPos;
    }
    public void GameStart()
    {
        SceneManager.LoadScene("Gameplay Scene");
    }

    public void Credits()
    {
        CreditsAppearPanel();
    }

    public void Settings()
    {
        SettingsAppearPanel();
    }

    public void CloseCreditsPopUp()
    {
        creditsAnimatedPanel.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.OutBack);
    }

    public void CloseSettingsPopUp()
    {
        settingsAnimatedPanel.DOAnchorPos(offScreenPos, moveDuration).SetEase(Ease.OutBack);
    }

    private void CreditsAppearPanel()
    {
        creditsAnimatedPanel.DOAnchorPos(centrePos, moveDuration).SetEase(Ease.OutBack);
    }

    private void SettingsAppearPanel()
    {
        settingsAnimatedPanel.DOAnchorPos(centrePos, moveDuration).SetEase(Ease.OutBack);
    }
}
