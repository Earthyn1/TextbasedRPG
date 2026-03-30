using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Fade-in / fade-out toast for quest events.
/// Supports three states: Accepted, Updated (with progress), Completed.
/// Toasts are queued so rapid updates never stomp each other.
/// </summary>
public class QuestToast : MonoBehaviour
{
    public static QuestToast Instance { get; private set; }

    [Header("UI Refs")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI questNameText;
    [SerializeField] private TextMeshProUGUI progressText;  // hidden when empty

    [Header("Timing")]
    [SerializeField] private float fadeInDuration  = 0.25f;
    [SerializeField] private float holdDuration    = 2.0f;
    [SerializeField] private float fadeOutDuration = 0.4f;

    private readonly Queue<(string header, string name, string progress)> _queue = new();
    private Coroutine _processCoroutine;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (canvasGroup) canvasGroup.alpha = 0f;
        if (progressText) progressText.gameObject.SetActive(false);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void ShowAccepted(string questName)
        => Enqueue("Quest Accepted", questName, "");

    public void ShowUpdate(string questName, int current, int required)
        => Enqueue("Quest Updated", questName, $"{current}/{required}");

    public void ShowCompleted(string questName)
        => Enqueue("Quest Completed", questName, "");

    // ── Queue processing ─────────────────────────────────────────────────────

    private void Enqueue(string header, string name, string progress)
    {
        _queue.Enqueue((header, name, progress));
        if (_processCoroutine == null)
            _processCoroutine = StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        while (_queue.Count > 0)
        {
            var (header, name, progress) = _queue.Dequeue();
            yield return StartCoroutine(ShowOne(header, name, progress));
        }
        _processCoroutine = null;
    }

    private IEnumerator ShowOne(string header, string name, string progress)
    {
        // Populate
        if (headerText)    headerText.text    = header;
        if (questNameText) questNameText.text = name;

        bool hasProgress = !string.IsNullOrEmpty(progress);
        if (progressText)
        {
            progressText.text = progress;
            progressText.gameObject.SetActive(hasProgress);
        }

        // Fade in → hold → fade out
        yield return StartCoroutine(Fade(0f, 1f, fadeInDuration));
        yield return new WaitForSeconds(holdDuration);
        yield return StartCoroutine(Fade(1f, 0f, fadeOutDuration));
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (!canvasGroup) yield break;

        float elapsed  = 0f;
        canvasGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed           += Time.deltaTime;
            canvasGroup.alpha  = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
