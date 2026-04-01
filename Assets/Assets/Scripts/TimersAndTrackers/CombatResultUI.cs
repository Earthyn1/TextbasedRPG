using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays a "Victory" or "Defeat" overlay when combat ends.
///
/// Wire up in Inspector:
///   - labelText       : TMP_Text at the top showing "Victory" or "Defeat"
///   - screenOverlay   : CanvasGroup covering the full screen (used for black fade on defeat)
///   - labelCanvasGroup: CanvasGroup on the label itself for fade in/out
///
/// Listens for EventBus "CombatResult" (bool) — true = win, false = lose.
/// </summary>
public class CombatResultUI : MonoBehaviour
{
    public static CombatResultUI Instance { get; private set; }

    [Header("Label")]
    [SerializeField] private TMP_Text     labelText;
    [SerializeField] private CanvasGroup  labelCanvasGroup;

    [Header("Screen Overlay (defeat black fade)")]
    [SerializeField] private CanvasGroup  screenOverlay;

    [Header("Timing")]
    [SerializeField] private float labelFadeInDuration  = 0.6f;
    [SerializeField] private float labelHoldDuration    = 1.4f;
    [SerializeField] private float labelFadeOutDuration = 0.4f;
    [SerializeField] private float screenFadeDuration   = 0.8f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Start hidden
        if (labelCanvasGroup != null)  labelCanvasGroup.alpha  = 0f;
        if (screenOverlay    != null)  screenOverlay.alpha     = 0f;
    }

    private void OnEnable()  => EventBus.OnTrigger += OnBusEvent;
    private void OnDisable() => EventBus.OnTrigger -= OnBusEvent;

    private void OnBusEvent(string trigger, object payload)
    {
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

        yield return FadeCG(labelCanvasGroup, 0f, 1f, labelFadeInDuration);
        yield return new WaitForSeconds(labelHoldDuration);
        yield return FadeCG(labelCanvasGroup, 1f, 0f, labelFadeOutDuration);
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
