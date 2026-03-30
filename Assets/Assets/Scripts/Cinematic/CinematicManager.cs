using System;
using System.Collections.Generic;
using Ink.Runtime;
using UnityEngine;

/// <summary>
/// Singleton that manages cinematic sequences — locking player interaction
/// and showing narrative text with a dark overlay and Continue button.
///
/// Usage:
///   CinematicManager.Instance.Play(new string[] { "Page one.", "Page two." }, onComplete: () => { ... });
///   CinematicManager.Instance.Play("A single page of story.");
/// </summary>
public class CinematicManager : MonoBehaviour
{
    public static CinematicManager Instance { get; private set; }

    /// <summary>True while a cinematic is displaying — use this to gate hitmask/NPC clicks.</summary>
    public static bool IsPlaying => Instance != null && Instance._isPlaying;

    /// <summary>Fired when a cinematic begins (player input locked).</summary>
    public static event Action OnCinematicStart;

    /// <summary>Fired when a cinematic ends (player input unlocked).</summary>
    public static event Action OnCinematicEnd;

    [SerializeField] private CinematicPanel cinematicPanel;

    private bool _isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>Play a multi-page cinematic sequence.</summary>
    public void Play(string[] pages, Action onComplete = null)
    {
        if (_isPlaying)
        {
            Debug.LogWarning("[CinematicManager] Already playing a cinematic — ignoring new request.");
            return;
        }

        if (cinematicPanel == null)
        {
            Debug.LogError("[CinematicManager] No CinematicPanel assigned!");
            onComplete?.Invoke();
            return;
        }

        _isPlaying = true;
        LockPlayer(true);
        OnCinematicStart?.Invoke();

        cinematicPanel.Show(pages, () =>
        {
            _isPlaying = false;
            LockPlayer(false);
            OnCinematicEnd?.Invoke();
            onComplete?.Invoke();
        });
    }

    /// <summary>Play a single-page cinematic.</summary>
    public void Play(string page, Action onComplete = null)
    {
        Play(new[] { page }, onComplete);
    }

    /// <summary>
    /// Play a cinematic driven by an Ink TextAsset.
    /// Each non-empty paragraph becomes one Continue page.
    /// Optionally jump to a specific knot first (e.g. "opening").
    /// </summary>
    public void Play(TextAsset inkJson, string startKnot = "", Action onComplete = null)
    {
        if (inkJson == null)
        {
            Debug.LogError("[CinematicManager] inkJson is null.");
            onComplete?.Invoke();
            return;
        }

        var story = new Story(inkJson.text);

        if (!string.IsNullOrEmpty(startKnot))
        {
            try { story.ChoosePathString(startKnot); }
            catch { Debug.LogWarning($"[CinematicManager] Knot '{startKnot}' not found in story."); }
        }

        var pages = new List<string>();
        while (story.canContinue)
        {
            var line = story.Continue().Trim();
            if (!string.IsNullOrEmpty(line))
                pages.Add(line);
        }

        if (pages.Count == 0)
        {
            Debug.LogWarning("[CinematicManager] Ink story produced no text — skipping cinematic.");
            onComplete?.Invoke();
            return;
        }

        Play(pages.ToArray(), onComplete);
    }

    private void LockPlayer(bool locked)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetNavigationLocked(locked);
    }
}
