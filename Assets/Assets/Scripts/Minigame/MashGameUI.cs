using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Strength minigame — rapidly click the button to fill the bar.
/// The bar constantly drains; reach 100% to win, hit 0% to fail.
///
/// Inspector setup:
///   canvasGroup — root CanvasGroup (for fade in/out)
///   fillBar     — Image with Image Type = Filled, Fill Method = Horizontal
///   mashButton  — the big clickable Button
///   successText — TMP_Text showing "Click Here" initially, then "SUCCESS!" / "FAIL"
/// </summary>
public class MashGameUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image       fillBar;
    [SerializeField] private Button      mashButton;
    [SerializeField] private TMP_Text    successText;

    [Header("Intro")]
    [SerializeField] private float introFadeDuration  = 0.3f;
    [SerializeField] private float introHoldDuration  = 1.5f;
    [SerializeField] private float textFadeDuration   = 0.25f;

    [Header("Defaults (overridden by MinigameManager)")]
    [SerializeField] private float drainRate    = 15f;  // % per second
    [SerializeField] private float fillPerClick = 8f;   // % per click
    [SerializeField] private float startFillPct = 30f;  // starting fill %

    [Header("Result")]
    [SerializeField] private float resultHoldTime   = 1.0f;
    [SerializeField] private float exitFadeDuration = 0.4f;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private float        _fillPct;
    private bool         _active;
    private bool         _started;   // true after the first click
    private Action<bool> _onComplete;
    private Action<bool> _onInstantResult;

    // ── Entry point ───────────────────────────────────────────────────────────

    public void Play(float drainRateOverride, float fillPerClickOverride, float startFill,
                     Action<bool> onComplete, Action<bool> onInstantResult = null)
    {
        drainRate        = drainRateOverride;
        fillPerClick     = fillPerClickOverride;
        startFillPct     = startFill;
        _onComplete      = onComplete;
        _onInstantResult = onInstantResult;

        _fillPct  = startFillPct;
        _started  = false;
        UpdateBar();

        // Text starts visible with prompt
        if (successText != null)
        {
            successText.text  = "Click Here";
            successText.color = Color.white;
            successText.alpha = 1f;
        }

        mashButton.onClick.AddListener(OnMashClicked);
        mashButton.interactable = false;

        if (canvasGroup != null) canvasGroup.alpha = 0f;

        StartCoroutine(IntroRoutine());
    }

    // ── Intro ─────────────────────────────────────────────────────────────────

    private IEnumerator IntroRoutine()
    {
        // Fade in whole UI
        float t = 0f;
        while (t < introFadeDuration)
        {
            t += Time.deltaTime;
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(t / introFadeDuration);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        // Hold — "Click Here" is visible, button not yet interactable
        yield return new WaitForSeconds(introHoldDuration);

        // Ready — player must click to start
        mashButton.interactable = true;
    }

    // ── Game loop ─────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_active) return;

        _fillPct -= drainRate * Time.deltaTime;

        if (_fillPct <= 0f)
        {
            _fillPct = 0f;
            UpdateBar();
            Resolve(false);
        }
        else
        {
            UpdateBar();
        }
    }

    private void OnMashClicked()
    {
        if (_active)
        {
            // Normal mash click
            _fillPct = Mathf.Min(_fillPct + fillPerClick, 100f);
            UpdateBar();

            if (_fillPct >= 100f)
                Resolve(true);
        }
        else if (!_started)
        {
            // First click — fade out "Click Here" and start the game
            _started = true;
            StartCoroutine(FadeText(1f, 0f, textFadeDuration, onDone: () => _active = true));
        }
    }

    private void UpdateBar()
    {
        if (fillBar != null)
            fillBar.fillAmount = _fillPct / 100f;
    }

    // ── Resolve ───────────────────────────────────────────────────────────────

    private void Resolve(bool success)
    {
        if (!_active) return; // guard against double-resolve
        _active = false;
        mashButton.interactable = false;

        _onInstantResult?.Invoke(success);
        StartCoroutine(ResolveRoutine(success));
    }

    private IEnumerator ResolveRoutine(bool success)
    {
        // Fade result text back in
        if (successText != null)
        {
            successText.text  = success ? "SUCCESS!" : "FAIL";
            successText.color = success
                ? new Color(0.35f, 0.85f, 0.35f)
                : new Color(0.85f, 0.3f,  0.3f);
        }
        yield return StartCoroutine(FadeText(0f, 1f, textFadeDuration));

        yield return new WaitForSeconds(resultHoldTime);

        // Fade out entire prefab
        float t = 0f;
        while (t < exitFadeDuration)
        {
            t += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / exitFadeDuration);
            yield return null;
        }

        _onComplete?.Invoke(success);
        Destroy(gameObject);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private IEnumerator FadeText(float from, float to, float duration, Action onDone = null)
    {
        if (successText == null) { onDone?.Invoke(); yield break; }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            successText.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        successText.alpha = to;
        onDone?.Invoke();
    }
}
