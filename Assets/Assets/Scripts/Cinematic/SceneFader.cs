using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the full-black-screen fade that bookends scene loads and cinematic moments.
///
/// Prefab setup:
///   - Canvas (Screen Space – Overlay, sort order high so it's always on top)
///     - Image "FadeImage" — full screen, colour #000000, alpha 1 on start
///
/// On Start it automatically fades from black to clear, then fires OnFadeInComplete.
/// GameStartCinematic subscribes to that event to chain the opening cinematic.
/// </summary>
public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    /// <summary>Fired once the initial scene fade-in completes.</summary>
    public static event Action OnFadeInComplete;

    [SerializeField] private Image fadeImage;
    [SerializeField] private float defaultFadeDuration = 1.2f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Ensure we start fully black
        SetAlpha(1f);
        fadeImage.gameObject.SetActive(true);
    }

    private void Start()
    {
        // Automatically fade in, then notify listeners (e.g. GameStartCinematic)
        FadeIn(defaultFadeDuration, () => OnFadeInComplete?.Invoke());
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Fade from black to clear. Optional callback when done.</summary>
    public void FadeIn(float duration = -1f, Action onComplete = null)
    {
        float dur = duration < 0f ? defaultFadeDuration : duration;
        StartCoroutine(FadeRoutine(1f, 0f, dur, () =>
        {
            fadeImage.gameObject.SetActive(false);
            onComplete?.Invoke();
        }));
    }

    /// <summary>Fade from clear to black. Optional callback when done.</summary>
    public void FadeOut(float duration = -1f, Action onComplete = null)
    {
        float dur = duration < 0f ? defaultFadeDuration : duration;
        fadeImage.gameObject.SetActive(true);
        SetAlpha(0f);
        StartCoroutine(FadeRoutine(0f, 1f, dur, onComplete));
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private IEnumerator FadeRoutine(float from, float to, float duration, Action onDone)
    {
        SetAlpha(from);
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }

        SetAlpha(to);
        onDone?.Invoke();
    }

    private void SetAlpha(float a)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }
}
