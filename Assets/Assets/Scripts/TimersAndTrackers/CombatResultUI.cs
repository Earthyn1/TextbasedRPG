using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a "Victory" or "Defeat" overlay when combat ends, plus a combat XP
/// toast under the Victory label.
///
/// Wire up in Inspector:
///   - labelText          : TMP_Text showing "Victory" or "Defeat"
///   - labelCanvasGroup   : CanvasGroup on the label for fade in/out
///   - xpToastRoot        : root GameObject containing xpIcon + xpText
///   - xpCanvasGroup      : CanvasGroup on xpToastRoot for fade in/out
///   - xpIcon             : UI Image for the skill icon (Strength / Speed etc.)
///   - xpText             : TMP_Text showing e.g. "334xp"
///   - screenOverlay      : CanvasGroup full-screen black (defeat only)
///
/// Listens for:
///   EventBus "CombatResult"   (bool)            — true = win, false = lose
///   EventBus "CombatXPEarned" (CombatXPPayload) — icon + amount to show
/// </summary>
public class CombatResultUI : MonoBehaviour
{
    public static CombatResultUI Instance { get; private set; }

    [Header("Victory / Defeat Label")]
    [SerializeField] private TMP_Text     labelText;
    [SerializeField] private CanvasGroup  labelCanvasGroup;

    [Header("Combat XP Toast (shown under Victory label)")]
    [SerializeField] private CanvasGroup xpCanvasGroup; // on the toast root
    [SerializeField] private Image       xpIcon;        // skill icon image
    [SerializeField] private TMP_Text    xpText;        // "334xp"

    [Header("Screen Overlay (defeat black fade)")]
    [SerializeField] private CanvasGroup  screenOverlay;

    [Header("Timing")]
    [SerializeField] private float labelFadeInDuration  = 0.6f;
    [SerializeField] private float labelHoldDuration    = 1.4f;
    [SerializeField] private float labelFadeOutDuration = 0.4f;
    [SerializeField] private float screenFadeDuration   = 0.8f;
    [SerializeField] private float xpFadeInDuration     = 0.4f;

    private CombatXPPayload _pendingXP;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Start hidden
        if (labelCanvasGroup != null) labelCanvasGroup.alpha = 0f;
        if (xpCanvasGroup    != null) xpCanvasGroup.alpha    = 0f;
        if (screenOverlay    != null) screenOverlay.alpha    = 0f;
    }

    private void OnEnable()  => EventBus.OnTrigger += OnBusEvent;
    private void OnDisable() => EventBus.OnTrigger -= OnBusEvent;

    private void OnBusEvent(string trigger, object payload)
    {
        if (trigger == "CombatXPEarned")
        {
            // Cache payload; CombatResult fires just after this
            _pendingXP = payload as CombatXPPayload;
            return;
        }

        if (trigger != "CombatResult") return;
        bool win = payload is bool b && b;
        StopAllCoroutines();
        StartCoroutine(win ? ShowVictory() : ShowDefeat());
    }

    // ── Sequences ─────────────────────────────────────────────────────────────

    private IEnumerator ShowVictory()
    {
        if (labelText != null)
        {
            labelText.text  = "Victory";
            labelText.color = new Color(1f, 0.85f, 0.2f); // gold
        }

        // Populate and reset the XP toast
        if (xpCanvasGroup != null) xpCanvasGroup.alpha = 0f;
        if (_pendingXP != null)
        {
            if (xpText != null) xpText.text = $"{_pendingXP.xp}xp";
            if (xpIcon != null)
            {
                xpIcon.sprite  = _pendingXP.icon;
                xpIcon.enabled = _pendingXP.icon != null;
            }
        }

        // Victory label fades in
        yield return FadeCG(labelCanvasGroup, 0f, 1f, labelFadeInDuration);

        // XP toast fades in underneath — only if there's actually a payload
        if (_pendingXP != null)
            yield return FadeCG(xpCanvasGroup, 0f, 1f, xpFadeInDuration);

        // Hold visible
        yield return new WaitForSeconds(labelHoldDuration);

        // Fade out
        if (_pendingXP != null)
            StartCoroutine(FadeCG(xpCanvasGroup, 1f, 0f, labelFadeOutDuration));
        yield return FadeCG(labelCanvasGroup, 1f, 0f, labelFadeOutDuration);

        _pendingXP = null;
    }

    private IEnumerator ShowDefeat()
    {
        if (labelText != null)
        {
            labelText.text  = "Defeat";
            labelText.color = new Color(0.85f, 0.15f, 0.15f); // red
        }

        // Fade in label and black overlay simultaneously
        StartCoroutine(FadeCG(labelCanvasGroup, 0f, 1f, labelFadeInDuration));
        yield return FadeCG(screenOverlay, 0f, 1f, screenFadeDuration);

        // Screen is now black — respawn sequence takes over from here.
        // Leave overlay visible; RespawnUI will reset it when it's done.
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    /// <summary>Call this from RespawnUI once the respawn is complete to clear the overlay.</summary>
    public void ResetOverlay()
    {
        StopAllCoroutines();
        if (labelCanvasGroup != null) labelCanvasGroup.alpha = 0f;
        if (screenOverlay    != null) screenOverlay.alpha    = 0f;
    }

    private IEnumerator FadeCG(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;
        float t = 0f;
        cg.alpha = from;
        while (t < duration)
        {
            t       += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        cg.alpha = to;
    }
}
