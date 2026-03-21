using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to the WorldInteractableButtonPrefab alongside its Button + TMP_Text.
///
/// Inspector refs:
///   skillIcon        — skill icon image (shown for minigame choices)
///   difficultyOverlay — overlay image whose color is set in the prefab; just toggled on/off
///   reqText          — TMP_Text showing e.g. "Req: Perception 3" when locked
///   buttonLabelRect  — RectTransform of the main choice label (shifted down when req is shown)
/// </summary>
public class DialogChoiceButton : MonoBehaviour
{
    [SerializeField] private Image          skillIcon;
    [SerializeField] private Image          difficultyOverlay;
    [SerializeField] private TMP_Text       reqText;
    [SerializeField] private RectTransform  buttonLabelRect;
    [SerializeField] private CanvasGroup    canvasGroup;

    private float _defaultLabelY;

    private void Awake()
    {
        // Cache the label's default Y so we can restore it later
        if (buttonLabelRect != null)
            _defaultLabelY = buttonLabelRect.anchoredPosition.y;

        HideMinigameInfo();
    }

    /// <summary>Show skill icon and overlay for a minigame choice.</summary>
    public void SetMinigameInfo(Sprite skillSprite)
    {
        if (skillIcon != null)
        {
            skillIcon.sprite = skillSprite;
            skillIcon.gameObject.SetActive(skillSprite != null);
        }

        if (difficultyOverlay != null)
            difficultyOverlay.gameObject.SetActive(true);
    }

    /// <summary>
    /// Show the requirement label and shift the button text down.
    /// Call when the player does NOT meet the skill requirement.
    /// </summary>
    public void ShowRequirement(string label)
    {
        if (reqText != null)
        {
            reqText.text = label;
            reqText.gameObject.SetActive(true);
        }

        if (buttonLabelRect != null)
        {
            var pos = buttonLabelRect.anchoredPosition;
            buttonLabelRect.anchoredPosition = new Vector2(pos.x, -21.2f);
        }

        if (difficultyOverlay != null) difficultyOverlay.gameObject.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 0.6f;
    }

    /// <summary>Hide the requirement label and restore the button text to its default position.</summary>
    public void HideRequirement()
    {
        if (reqText != null)
            reqText.gameObject.SetActive(false);

        if (buttonLabelRect != null)
        {
            var pos = buttonLabelRect.anchoredPosition;
            buttonLabelRect.anchoredPosition = new Vector2(pos.x, _defaultLabelY);
        }

        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    /// <summary>Hide all minigame UI — used for non-minigame choices.</summary>
    public void HideMinigameInfo()
    {
        if (skillIcon != null)         skillIcon.gameObject.SetActive(false);
        if (difficultyOverlay != null) difficultyOverlay.gameObject.SetActive(false);
        HideRequirement();
    }
}
