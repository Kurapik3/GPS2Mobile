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
    public void OnClick()
    {
        //Debug.Log("CLICKED");
        //TechTreeUI.instance.OpenConfirmPopup(this);

        if (state == TechState.Available)
        {
            TechTreeUI.instance.OpenConfirmPopup(this);
        }
    }

    public void Unlock()
    {
        state = TechState.Unlocked;
        UpdateVisual();
        UpdateConnectedNodes();
    }

    public void UpdateVisual()
    {
        switch (state)
        {
            case TechState.Locked:
                background.sprite = lockedSprite;
                button.interactable = false;
                break;

            case TechState.Available:
                background.sprite = availableSprite;
                button.interactable = true;
                break;

            case TechState.Unlocked:
                background.sprite = unloackedSprite;
                button.interactable = false;
                break;
        }
        UpdateLines();
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
