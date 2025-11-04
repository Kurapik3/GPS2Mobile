using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public enum TechState { Locked, Available, Unlocked }

public class TechNode : MonoBehaviour
{
    [Header("Tech Info")]
    public string techName;
    public int costAP;
    public TechNode[] prerequisites;

    [Header("UI Reference")]
    public Image background;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public Button button;

    [Header("Connection Line")]
    public Image[] connectedLines;

    [HideInInspector] public TechState state = TechState.Locked;

    private PlayerTracker player;
    private Tween glowTween;

    public Sprite lockedSprite;
    public Sprite unloackedSprite;
    public Sprite availableSprite;

    public void Initialize()
    {
        player = FindAnyObjectByType<PlayerTracker>();

        nameText.text = techName;
        costText.text = costAP + " AP";

        if (prerequisites == null || prerequisites.Length == 0 )
        {
            state =  TechState.Available;
        }

        UpdateVisual();
    }

    //public void Setup(string name, int cost, Color color)
    //{
    //    techName = name;
    //    costAP = cost;
    //    nameText.text = name;
    //    costText.text = cost + " AP";
    //    background.color = color;

    //    techTree = FindAnyObjectByType<TechTree>();
    //    player = FindAnyObjectByType<PlayerTracker>();

    //    UpdateVisual();
    //}

    private void Update()
    {
        //if (state == TechState.Available)
        //{
        //    TechTreeUI.instance.OpenConfirmPopup(this);
        //}
    }

    public void OnClick()
    {
        Debug.Log("CLICKED");
        TechTreeUI.instance.OpenConfirmPopup(this);

        if (state == TechState.Available)
        {
            TechTreeUI.instance.OpenConfirmPopup(this);
        }
    }

    public void Unlock()
    {
        state = TechState.Unlocked;
        UpdateVisual();
        StopGlow();
        UpdateConnectedNodes();
    }

    public void UpdateVisual()
    {
        switch (state)
        {
            case TechState.Locked:
                background.sprite = lockedSprite;
                button.interactable = false;
                StopGlow();
                break;

            case TechState.Available:
                background.sprite = availableSprite;
                button.interactable = true;
                StartGlow();
                break;

            case TechState.Unlocked:
                background.sprite = unloackedSprite;
                button.interactable = false;
                StopGlow();
                break;
        }
        UpdateLines();
    }

    private void StartGlow()
    {
        StopGlow();

        if (background == null) return;

        glowTween = background.DOColor(new Color(0.7f, 1f, 1f), 1f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    private void StopGlow()
    {
        if (glowTween != null && glowTween.IsActive()) glowTween.Kill();

        if(background != null) background.color = Color.white;
    }

    void UpdateConnectedNodes()
    {
        foreach (var node in FindObjectsOfType<TechNode>())
        {
            if (node == this) continue;

            bool allUnlocked = true;
            foreach (var pre in node.prerequisites)
            {
                if (pre.state != TechState.Unlocked)
                {
                    allUnlocked = false;
                    break;
                }
            }

            if (allUnlocked && node.state == TechState.Locked)
            {
                node.state = TechState.Available;
                node.UpdateVisual();
            }
        }
    }
    private void UpdateLines()
    {
        foreach (var line in connectedLines)
        {
            if (line == null) continue;

            switch (state)
            {
                case TechState.Locked:
                    line.color = Color.gray;
                    break;
                case TechState.Available:
                    line.color = new Color(0.8f, 0.8f, 0.8f);
                    break;
                case TechState.Unlocked:
                    line.color = Color.white;
                    break;
            }
        }
    }
}
