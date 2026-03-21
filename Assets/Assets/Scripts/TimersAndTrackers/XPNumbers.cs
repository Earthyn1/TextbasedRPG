using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class XPNumbers : MonoBehaviour
{
    public TMP_Text _Text;
    public Image    _Image;

    [Header("Float Animation")]
    [SerializeField] private float fadeInDuration  = 0.3f;
    [SerializeField] private float holdDuration    = 0.6f;
    [SerializeField] private float fadeOutDuration = 0.4f;
    [SerializeField] private float floatDistance   = 60f;   // pixels to travel upward

    // Legacy animator kept in case it still exists on the prefab — disabled at runtime
    [SerializeField] private Animator Animator;

    private CanvasGroup _canvasGroup;
    private RectTransform _rect;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _rect = GetComponent<RectTransform>();

        // Disable any legacy animator so it doesn't fight the coroutine
        if (Animator != null)
            Animator.enabled = false;
    }

    private void Start()
    {
        StartCoroutine(FloatRoutine());
    }

    private IEnumerator FloatRoutine()
    {
        Vector2 startPos = _rect.anchoredPosition;
        Vector2 endPos   = startPos + Vector2.up * floatDistance;

        // ── Fade in + rise ────────────────────────────────────────────────────
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            float pct = Mathf.Clamp01(t / fadeInDuration);
            _canvasGroup.alpha          = pct;
            _rect.anchoredPosition      = Vector2.Lerp(startPos, endPos, pct * 0.4f);
            yield return null;
        }

        _canvasGroup.alpha = 1f;

        // ── Hold ──────────────────────────────────────────────────────────────
        float holdEnd = Time.time + holdDuration;
        float holdProgress = 0f;
        while (Time.time < holdEnd)
        {
            holdProgress += Time.deltaTime / holdDuration;
            _rect.anchoredPosition = Vector2.Lerp(
                startPos + Vector2.up * floatDistance * 0.4f,
                endPos,
                holdProgress * 0.6f);
            yield return null;
        }

        // ── Fade out + continue rising ────────────────────────────────────────
        t = 0f;
        Vector2 fadeStartPos = _rect.anchoredPosition;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float pct = Mathf.Clamp01(t / fadeOutDuration);
            _canvasGroup.alpha     = 1f - pct;
            _rect.anchoredPosition = Vector2.Lerp(fadeStartPos, endPos, pct);
            yield return null;
        }

        DestroySelf();
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
