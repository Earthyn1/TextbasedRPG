using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen overlay fader for zone transitions.
/// Place on a Canvas that sits above everything else (sort order highest).
/// Call FadeToBlack() before swapping content, then FadeFromBlack() after.
/// </summary>
public class SceneTransitionFader : MonoBehaviour
{
    public static SceneTransitionFader Instance { get; private set; }

    [SerializeField] private CanvasGroup overlayGroup;
    [SerializeField] private float fadeOutDuration = 0.25f; // scene → black
    [SerializeField] private float fadeInDuration  = 0.35f; // black → new scene

    private Coroutine _current;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (overlayGroup) overlayGroup.alpha = 0f;
    }

    /// <summary>Fade to black. Await this before swapping zone content.</summary>
    public IEnumerator FadeToBlack()
    {
        if (_current != null) StopCoroutine(_current);
        yield return _current = StartCoroutine(Fade(0f, 1f, fadeOutDuration));
    }

    /// <summary>Fade from black back to the scene.</summary>
    public IEnumerator FadeFromBlack()
    {
        if (_current != null) StopCoroutine(_current);
        yield return _current = StartCoroutine(Fade(1f, 0f, fadeInDuration));
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (overlayGroup == null) yield break;

        float t = 0f;
        overlayGroup.alpha            = from;
        overlayGroup.blocksRaycasts   = true; // block input during transition

        while (t < duration)
        {
            t                  += Time.deltaTime;
            overlayGroup.alpha  = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }

        overlayGroup.alpha          = to;
        overlayGroup.blocksRaycasts = to > 0f; // unblock once fully transparent
    }
}
