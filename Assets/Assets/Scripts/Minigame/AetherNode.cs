using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A single node in the Aether path minigame.
/// Corrupted nodes are always visible as red — player must trace around them.
/// Clicking a corrupted node when it's active triggers an instant fail.
/// </summary>
public class AetherNode : MonoBehaviour
{
    public enum NodeState { Inactive, Clickable, Selected, Locked }

    [Header("Visuals")]
    [SerializeField] private Image nodeImage;

    [Header("Colors")]
    [SerializeField] private Color inactiveColor  = new Color(0.40f, 0.40f, 0.70f, 0.50f);
    [SerializeField] private Color clickableColor = new Color(0.75f, 0.75f, 1.00f, 1.00f);
    [SerializeField] private Color selectedColor  = new Color(0.40f, 0.90f, 1.00f, 1.00f);
    [SerializeField] private Color corruptedColor = new Color(0.80f, 0.20f, 0.20f, 0.90f);
    [SerializeField] private Color lockedColor    = new Color(0.20f, 0.20f, 0.30f, 0.30f);

    // ── Public state ──────────────────────────────────────────────────────────

    public bool IsCorrupted { get; private set; }
    public int  Column      { get; private set; }
    public int  Row         { get; private set; }

    private Button             _button;
    private Action<AetherNode> _onClick;

    // ── Setup ─────────────────────────────────────────────────────────────────

    public void Setup(int col, int row, bool isCorrupted, Action<AetherNode> onClick)
    {
        Column      = col;
        Row         = row;
        IsCorrupted = isCorrupted;
        _onClick    = onClick;

        _button = GetComponent<Button>();
        if (_button != null)
            _button.onClick.AddListener(() => _onClick?.Invoke(this));

        // Show corruption immediately so the player can plan during the intro hold
        SetState(NodeState.Inactive);
    }

    // ── State ─────────────────────────────────────────────────────────────────

    public void SetState(NodeState state)
    {
        Color color       = inactiveColor;
        bool  interactable = false;

        switch (state)
        {
            case NodeState.Inactive:
                color       = IsCorrupted ? corruptedColor : inactiveColor;
                interactable = false;
                break;

            case NodeState.Clickable:
                // Corrupted nodes stay red even when clickable — player sees the danger
                color       = IsCorrupted ? corruptedColor : clickableColor;
                interactable = true;
                break;

            case NodeState.Selected:
                color       = selectedColor;
                interactable = false;
                break;

            case NodeState.Locked:
                color       = IsCorrupted ? corruptedColor : lockedColor;
                interactable = false;
                break;
        }

        if (nodeImage != null) nodeImage.color    = color;
        if (_button   != null) _button.interactable = interactable;
    }
}
