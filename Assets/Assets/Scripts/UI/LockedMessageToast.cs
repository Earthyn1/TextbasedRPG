using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Singleton toast that fades in at the bottom of the screen to show a locked message.
/// Attach to a Canvas GameObject with a CanvasGroup and TMP_Text child.
/// Automatically hides on zone change, dialog open, or cinematic start.
/// </summary>
public class LockedMessageToast : MonoBehaviour
{
    public static LockedMessageToast Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text messageText;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration  = 0.25f;
    [SerializeField] private float holdDuration    = 2.0f;
    [SerializeField] private float fadeOutDuration = 0.4f;

    private Coroutine _current;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        GameManager.OnZoneWillChange      += OnDismiss;
        CinematicManager.OnCinematicStart += OnDismiss;
    }

    private void OnDisable()
    {
        GameManager.OnZoneWillChange      -= OnDismiss;
        CinematicManager.OnCinematicStart -= OnDismiss;
    }

    // Called by zone change event (passes ZoneData, we ignore it)
    private void OnDismiss(ZoneData _) => Hide();

    // Called by cinematic start event (no args)
    private void OnDismiss() => Hide();

    public void Show(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(ShowRoutine(message));
    }

    public void Hide()
    {
        if (_current != null) { StopCoroutine(_current); _current = null; }
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    private IEnumerator ShowRoutine(string message)
    {
        messageText.text = message;
        yield return Fade(0f, 1f, fadeInDuration);
        yield return new WaitForSeconds(holdDuration);
        yield return Fade(1f, 0f, fadeOutDuration);
        _current = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
