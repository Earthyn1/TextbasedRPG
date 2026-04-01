using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Passive timed-action overlay. Displays a progress bar that fills automatically
/// over a set duration, then always reports success.
///
/// Spawned by MinigameManager on top of the active Dialog or WorldInteractable panel.
/// No player input is required — the player simply watches the bar fill.
///
/// Ink trigger format:
///   ~ startMinigame("myAction, 3, TimedAction, Perception, Searching the area...")
///   Parts: id, level, TimedAction, SkillName, [optional display label]
///
/// The skill level is used to scale duration (higher skill = faster completion).
/// Always fires MinigameFound on completion.
/// </summary>
public class TimedActionUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Image with Fill Method = Horizontal (or Radial). Fill amount goes 0 → 1.")]
    [SerializeField] private Image       fillBar;

    [Tooltip("Label shown while the action runs (e.g. 'Searching the area...')")]
    [SerializeField] private TMP_Text    actionLabel;

    [Tooltip("Optional result flash text shown briefly after completion (e.g. 'Done!')")]
    [SerializeField] private TMP_Text    resultText;

    [Tooltip("CanvasGroup on the root used for fade in / out animations")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration  = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.4f;
    [SerializeField] private float resultHoldTime  = 0.55f;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private float        _duration;
    private float        _elapsed;
    private bool         _running;
    private Action<bool> _onComplete;
    private Action<bool> _onInstantResult;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Starts the timed action.
    /// </summary>
    /// <param name="label">Text displayed on the action label while filling.</param>
    /// <param name="duration">Total fill duration in seconds.</param>
    /// <param name="onComplete">Called after exit animation. Always passes <c>true</c>.</param>
    /// <param name="onInstantResult">Called the frame the bar completes (before exit anim). Used for XP.</param>
    public void Play(string label, float duration,
                     Action<bool> onComplete,
                     Action<bool> onInstantResult = null)
    {
        _duration        = Mathf.Max(duration, 0.1f);
        _elapsed         = 0f;
        _running         = false;
        _onComplete      = onComplete;
        _onInstantResult = onInstantResult;

        if (actionLabel != null) actionLabel.text    = label;
        if (fillBar     != null) fillBar.fillAmount   = 0f;
        if (resultText  != null) resultText.gameObject.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha    = 0f;

        StartCoroutine(RunSequence());
    }

    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_running) return;

        _elapsed += Time.deltaTime;

        if (fillBar != null)
            fillBar.fillAmount = Mathf.Clamp01(_elapsed / _duration);

        if (_elapsed >= _duration)
        {
            _running = false;
            if (fillBar != null) fillBar.fillAmount = 1f;
            StartCoroutine(FinishSequence());
        }
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    private IEnumerator RunSequence()
    {
        yield return StartCoroutine(Fade(0f, 1f, fadeInDuration));
        _running = true;
    }

    private IEnumerator FinishSequence()
    {
        // XP and quest reporting fires immediately when the bar completes
        _onInstantResult?.Invoke(true);

        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            resultText.text = "Done!";
        }

        yield return new WaitForSeconds(resultHoldTime);

        yield return StartCoroutine(Fade(1f, 0f, fadeOutDuration));

        _onComplete?.Invoke(true);
        Destroy(gameObject);
    }

    private IEnumerator Fade(float from, float to, float dur)
    {
        if (canvasGroup == null) yield break;

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
