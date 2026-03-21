using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ItemPickupToast : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private CanvasGroup canvasGroup;

    private RectTransform _rect;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    /// <summary>
    /// Call immediately after spawning.
    /// spawnPos and targetPos should both be in Canvas (local) space.
    /// </summary>
    public void Init(Sprite sprite, Vector2 spawnPos, Vector2 targetPos)
    {
        icon.sprite = sprite;
        _rect.anchoredPosition = spawnPos;
        StartCoroutine(Animate(spawnPos, targetPos));
    }

    private IEnumerator Animate(Vector2 spawnPos, Vector2 targetPos)
    {
        // --- Phase 1: pop in and float up briefly ---
        float holdDuration = 0.35f;
        float elapsed = 0f;
        Vector2 floatOffset = new Vector2(0, 30f);

        transform.localScale = Vector2.zero;
        canvasGroup.alpha = 0f;

        while (elapsed < holdDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / holdDuration;
            float ease = 1f - Mathf.Pow(1f - t, 3f); // ease out cubic

            transform.localScale = Vector3.one * Mathf.Lerp(0f, 1f, ease);
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, ease);
            _rect.anchoredPosition = Vector2.Lerp(spawnPos, spawnPos + floatOffset, ease);

            yield return null;
        }

        transform.localScale = Vector3.one;
        canvasGroup.alpha = 1f;

        // --- Phase 2: fly toward bag icon and fade out ---
        float flyDuration = 0.55f;
        elapsed = 0f;
        Vector2 flyStart = _rect.anchoredPosition;

        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyDuration;
            float ease = t * t; // ease in — accelerates toward target

            _rect.anchoredPosition = Vector2.Lerp(flyStart, targetPos, ease);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.5f, ease);

            yield return null;
        }

        Destroy(gameObject);
    }
}
