
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
    public Sprite icon; 

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
    public Sprite unlockedSprite;
    public Sprite availableSprite;
    public Sprite notEnoughSprite;

    private void Awake()
    {
        player.OnAPChanged += UpdateVisual;
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }


    public void Initialize()
    {
        player = FindAnyObjectByType<PlayerTracker>();
        nameText.text = techName;
        costText.text = costAP + " AP";

        // Set initial state based on prerequisites
        UpdateState();
        UpdateVisual();

        player.OnAPChanged += UpdateVisual;
    }
    private void OnDestroy()
    {
        player.OnAPChanged -= UpdateVisual;
    }

    public void OnClick()
    {
        Debug.Log($"TechNode clicked: {techName}, State: {state}");

        if (state == TechState.Available)
        {
            TechTreeUI.instance.OpenConfirmPopup(this);
        }
    }

    public void Unlock()
    {
        // Only change to Unlocked if currently Available
        if (state == TechState.Available)
        {
            state = TechState.Unlocked;
            UpdateVisual();
            UpdateConnectedNodes();
        }
    }

    private void UpdateState()
    {
        if (prerequisites == null || prerequisites.Length == 0)
        {
            // No prerequisites - can be available if not already unlocked
            if (state != TechState.Unlocked)
                state = TechState.Available;
            return;
        }

        bool allPrerequisitesMet = true;
        foreach (var prerequisite in prerequisites)
        {
            if (prerequisite == null || prerequisite.state != TechState.Unlocked)
            {
                allPrerequisitesMet = false;
                break;
            }
        }

        // Only change state if necessary
        if (allPrerequisitesMet && state == TechState.Locked)
        {
            state = TechState.Available;
        }
        else if (!allPrerequisitesMet && state == TechState.Available)
        {
            state = TechState.Locked;
        }
        // If already Unlocked, keep it as Unlocked
    }

    public void UpdateVisual()
    {
        int currentAP = player?.currentAP ?? int.MaxValue;

        switch (state)
        {
            case TechState.Locked:
                background.sprite = lockedSprite;
                if (button != null) button.interactable = false;
                break;
            case TechState.Available:
                if (currentAP >= costAP)
                {
                    background.sprite = availableSprite;
                    if (button !=null) button.interactable = true;
                }
                else
                {
                    background.sprite = notEnoughSprite;
                    if (button != null) button.interactable = true;
                }
                break;

            case TechState.Unlocked:
                        background.sprite = unlockedSprite;
                        if (button != null) button.interactable = false;
                        break;
                    }
        UpdateLines();
    }

    void UpdateConnectedNodes()
    {
        foreach (var node in FindObjectsOfType<TechNode>())
        {
            if (node == this) continue;

            // Preserve current state if already unlocked
            if (node.state == TechState.Unlocked)
                continue;

            // Update state based on prerequisites
            node.UpdateState();
            node.UpdateVisual();
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