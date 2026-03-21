using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Self-contained timing bar minigame UI.
/// Place this component on the TimingBar prefab alongside its own UI children.
///
/// Inspector refs (all on the prefab itself):
///   arrowRect       — the moving indicator
///   barRect         — the full bar (used to get width)
///   successZoneRect — the green hit zone
/// </summary>
public class TimingBarUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private RectTransform arrowRect;
    [SerializeField] private RectTransform barRect;
    [SerializeField] private RectTransform successZoneRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private CanvasGroup arrowCanvasGroup;
    [SerializeField] private CanvasGroup successFailCanvasGroup;
    [SerializeField] private TMP_Text successFailText;

    [Header("Fade")]
    [SerializeField] private float fadeDuration      = 0.5f;
    [SerializeField] private float resultHoldDuration = 0.5f;
    [SerializeField] private float introFadeDuration = 0.3f;
    [SerializeField] private float introHoldDuration = 1.5f;

    [Header("Settings (overridden at runtime by difficulty)")]
    [SerializeField] private float speed = 300f;
    [SerializeField] private float successZoneWidth = 80f;
    [SerializeField] [Range(0f, 1f)] private float successZoneCenter = 0.5f;

    private float _arrowX;
    private float _barWidth;
    private float _direction = 1f;
    private bool _running;
    private string _minigameId;
    private Action<bool> _onComplete;
    private Action<bool> _onInstantResult; // fires immediately when result is known, before animations

    /// <summary>Start the minigame. Calls onComplete(true/false) then destroys itself.</summary>
    public void Play(string minigameId, float overrideSpeed, float overrideZoneWidth, Action<bool> onComplete, Action<bool> onInstantResult = null)
    {
        speed            = overrideSpeed;
        successZoneWidth = overrideZoneWidth;
        _minigameId = minigameId;
        _onComplete = onComplete;
        _onInstantResult = onInstantResult;

        _barWidth = barRect.rect.width;

        // Randomize success zone position each play (keep it away from edges)
        successZoneCenter = UnityEngine.Random.Range(0.15f, 0.85f);
        float centerX = (_barWidth * successZoneCenter) - (_barWidth * 0.5f);
        successZoneRect.sizeDelta = new Vector2(successZoneWidth, successZoneRect.sizeDelta.y);
        successZoneRect.anchoredPosition = new Vector2(centerX, successZoneRect.anchoredPosition.y);

        _arrowX    = -_barWidth * 0.5f; // start at left edge
        _direction = 1f;
        _running   = false; // held until intro completes

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        // Fade in
        float elapsed = 0f;
        while (elapsed < introFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / introFadeDuration);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        // Hold before arrow starts
        yield return new WaitForSeconds(introHoldDuration);

        _running = true;
    }

    private void Update()
    {
        if (!_running) return;

        float halfBar = _barWidth * 0.5f;

        _arrowX += speed * _direction * Time.deltaTime;

        if (_arrowX >= halfBar)  { _arrowX = halfBar;  _direction = -1f; }
        if (_arrowX <= -halfBar) { _arrowX = -halfBar; _direction =  1f; }

        arrowRect.anchoredPosition = new Vector2(_arrowX, arrowRect.anchoredPosition.y);

        if (Input.GetMouseButtonDown(0))
            Resolve();
    }

    private void Resolve()
    {
        _running = false;

        float successLeft  = successZoneRect.anchoredPosition.x - successZoneWidth * 0.5f;
        float successRight = successZoneRect.anchoredPosition.x + successZoneWidth * 0.5f;
        bool success = _arrowX >= successLeft && _arrowX <= successRight;

        Debug.Log($"[TimingBarUI] id='{_minigameId}' arrowX={_arrowX:F1} zone=[{successLeft:F1},{successRight:F1}] success={success}");

        _onInstantResult?.Invoke(success); // fire immediately — XP toast etc.
        StartCoroutine(FadeOutThenComplete(success));
    }

    private IEnumerator FadeOutThenComplete(bool success)
    {
        // --- Phase 1: fade out arrow, fade in SUCCESS/FAIL text simultaneously ---
        if (successFailText != null)
        {
            successFailText.text = success ? "SUCCESS" : "FAIL";
            successFailText.color = Color.white;
        }

        if (successFailCanvasGroup != null) successFailCanvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            if (arrowCanvasGroup != null)    arrowCanvasGroup.alpha    = Mathf.Lerp(1f, 0f, t);
            if (successFailCanvasGroup != null) successFailCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        // --- Phase 2: hold the result text briefly ---
        yield return new WaitForSeconds(resultHoldDuration);

        // --- Phase 3: fade out entire prefab (text fades faster) ---
        elapsed = 0f;
        float textFadeSpeed = 2f; // multiplier — text fades this much faster than the rest
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            if (successFailCanvasGroup != null)
                successFailCanvasGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t * textFadeSpeed));

            yield return null;
        }

        _onComplete?.Invoke(success);
        Destroy(gameObject);
    }
}
