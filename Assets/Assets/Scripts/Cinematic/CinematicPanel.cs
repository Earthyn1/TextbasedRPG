using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The UI component for cinematic sequences.
///
/// Prefab setup (the panel itself only needs the overlay + button):
///   - Root GameObject with CanvasGroup (dark overlay image)
///   - Child: Button "ContinueButton" with its own CanvasGroup
///
/// The narrative text is the existing scene description text — assign it in the Inspector.
/// Its CanvasGroup (descriptionGroup from ZoneUIManager) is also assigned so we can
/// control its visibility independently of the overlay.
///
/// Behaviour:
///   - Fades in dark overlay
///   - Types text into the existing scene description text
///   - First Continue click while typing: skips to full text
///   - Second Continue click (or first if not typing): advances to next page or closes
///   - On close: button fades out, then overlay fades out, then text is cleared
/// </summary>
public class CinematicPanel : MonoBehaviour
{
    [Header("UI References — Overlay & Button (on this prefab)")]
    [SerializeField] private CanvasGroup overlayCanvasGroup;    // CanvasGroup on the root panel (dark overlay)
    [SerializeField] private Button continueButton;
    [SerializeField] private CanvasGroup continueCanvasGroup;   // CanvasGroup on the Continue button itself

    [Header("Scene Text Reference (assign the existing description text)")]
    [SerializeField] private TMP_Text narrativeText;            // drag the existing zone description TMP_Text here
    [SerializeField] private CanvasGroup narrativeCanvasGroup;  // drag the existing descriptionGroup here (can be null)

    [Header("Text Fade Settings")]
    [SerializeField] private float textFadeInDuration = 0.8f;   // how long the paragraph takes to fade in
    [SerializeField] private float continueShowDelay = 0.3f;    // seconds after text finishes fading before Continue appears

    [Header("Fade Settings")]
    [SerializeField] private float overlayFadeInDuration = 0.6f;
    [SerializeField] private float overlayFadeOutDuration = 0.5f;
    [SerializeField] private float targetOverlayAlpha = 0.82f;  // how dark the overlay goes (0–1)
    [SerializeField] private float continueFadeDuration = 0.3f;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private string[] _pages;
    private int _currentPage;
    private Action _onComplete;
    private Coroutine _textFadeCoroutine;
    private bool _isFading;
    private string _savedDescription;
    private Color _textOriginalColor;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        continueButton.onClick.AddListener(OnContinueClicked);

        _textOriginalColor = narrativeText.color;

        overlayCanvasGroup.alpha = 0f;
        overlayCanvasGroup.blocksRaycasts = false;
        overlayCanvasGroup.interactable = false;
        continueCanvasGroup.alpha = 0f;
        continueButton.interactable = false;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Show(string[] pages, Action onComplete)
    {
        _pages = pages;
        _currentPage = 0;
        _onComplete = onComplete;

        // Remember whatever was showing so we can restore it afterwards
        _savedDescription = narrativeText.text;
        narrativeText.text = "";
        continueCanvasGroup.alpha = 0f;
        continueButton.interactable = false;

        // Ensure the scene description text is visible
        if (narrativeCanvasGroup != null)
        {
            narrativeCanvasGroup.alpha = 1f;
            narrativeCanvasGroup.blocksRaycasts = false;
            narrativeCanvasGroup.interactable = false;
        }

        StartCoroutine(FadeOverlay(0f, targetOverlayAlpha, overlayFadeInDuration, () =>
        {
            overlayCanvasGroup.blocksRaycasts = true;
            overlayCanvasGroup.interactable = true;
            ShowPage(_currentPage);
        }));
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void ShowPage(int index)
    {
        continueCanvasGroup.alpha = 0f;
        continueButton.interactable = false;

        if (_textFadeCoroutine != null)
            StopCoroutine(_textFadeCoroutine);

        _textFadeCoroutine = StartCoroutine(FadeInTextRoutine(_pages[index]));
    }

    private IEnumerator FadeInTextRoutine(string text)
    {
        _isFading = true;

        // Set full text immediately but start transparent
        narrativeText.text = text;
        SetTextAlpha(0f);

        // Fade the text in over textFadeInDuration
        float t = 0f;
        while (t < textFadeInDuration)
        {
            t += Time.deltaTime;
            SetTextAlpha(Mathf.Clamp01(t / textFadeInDuration));
            yield return null;
        }

        SetTextAlpha(1f);
        _isFading = false;

        yield return new WaitForSeconds(continueShowDelay);
        yield return StartCoroutine(FadeContinue(0f, 1f));
        continueButton.interactable = true;
    }

    private void OnContinueClicked()
    {
        if (_isFading)
        {
            // Skip fade — show full text at full alpha immediately
            if (_textFadeCoroutine != null)
                StopCoroutine(_textFadeCoroutine);

            _isFading = false;
            narrativeText.text = _pages[_currentPage];
            SetTextAlpha(1f);

            StartCoroutine(FadeContinue(0f, 1f));
            continueButton.interactable = true;
            return;
        }

        _currentPage++;
        if (_currentPage < _pages.Length)
        {
            ShowPage(_currentPage);
        }
        else
        {
            Close();
        }
    }

    private void Close()
    {
        continueButton.interactable = false;
        overlayCanvasGroup.interactable = false;
        overlayCanvasGroup.blocksRaycasts = false;

        // 1. Fade out the continue button
        StartCoroutine(FadeContinue(1f, 0f, () =>
        {
            // 2. Then fade out the overlay
            StartCoroutine(FadeOverlay(targetOverlayAlpha, 0f, overlayFadeOutDuration, () =>
            {
                // 3. Restore the original scene description at full alpha
                SetTextAlpha(1f);
                narrativeText.text = _savedDescription;
                _onComplete?.Invoke();
            }));
        }));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetTextAlpha(float a)
    {
        Color c = narrativeText.color;
        c.a = a;
        narrativeText.color = c;
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    private IEnumerator FadeOverlay(float from, float to, float duration, Action onDone = null)
    {
        float t = 0f;
        overlayCanvasGroup.alpha = from;

        while (t < duration)
        {
            t += Time.deltaTime;
            overlayCanvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        overlayCanvasGroup.alpha = to;
        onDone?.Invoke();
    }

    private IEnumerator FadeContinue(float from, float to, Action onDone = null)
    {
        float t = 0f;
        continueCanvasGroup.alpha = from;

        while (t < continueFadeDuration)
        {
            t += Time.deltaTime;
            continueCanvasGroup.alpha = Mathf.Lerp(from, to, t / continueFadeDuration);
            yield return null;
        }

        continueCanvasGroup.alpha = to;
        onDone?.Invoke();
    }
}
