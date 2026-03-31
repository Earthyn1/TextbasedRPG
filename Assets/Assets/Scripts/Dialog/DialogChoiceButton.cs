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
    [SerializeField] private Image       skillIcon;
    [SerializeField] private Image       difficultyOverlay;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text    skillNumberText;

    [SerializeField] private float lockedAlpha = 0.4f;
    [SerializeField] private Color metColor    = new Color(0.27f, 0.85f, 0.27f); // green
    [SerializeField] private Color notMetColor = new Color(0.90f, 0.25f, 0.25f); // red

    private void Awake() => HideMinigameInfo();

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

    /// <summary>Player does NOT meet the requirement — dim button, show playerLevel/reqLevel in red.</summary>
    public void ShowRequirementNotMet(int playerLevel, int reqLevel)
    {
        if (canvasGroup != null)    canvasGroup.alpha = lockedAlpha;
        if (skillNumberText != null)
        {
            skillNumberText.text  = $"{playerLevel}/{reqLevel}";
            skillNumberText.color = notMetColor;
            skillNumberText.gameObject.SetActive(true);
        }
    }

    /// <summary>Player meets the requirement — full opacity, show reqLevel in green.</summary>
    public void ShowRequirementMet(int reqLevel)
    {
        if (canvasGroup != null)    canvasGroup.alpha = 1f;
        if (skillNumberText != null)
        {
            skillNumberText.text  = $"{reqLevel}";
            skillNumberText.color = metColor;
            skillNumberText.gameObject.SetActive(true);
        }
    }

    /// <summary>No skill requirement — full opacity, hide number text.</summary>
    public void HideRequirement()
    {
        if (canvasGroup != null)     canvasGroup.alpha = 1f;
        if (skillNumberText != null) skillNumberText.gameObject.SetActive(false);
    }

    /// <summary>Hide all minigame UI — used for non-minigame choices.</summary>
    public void HideMinigameInfo()
    {
        if (skillIcon != null)         skillIcon.gameObject.SetActive(false);
        if (difficultyOverlay != null) difficultyOverlay.gameObject.SetActive(false);
        HideRequirement();
    }
}
