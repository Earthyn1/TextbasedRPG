using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ZoneScenePropSpawner : MonoBehaviour
{
    [Header("Required refs")]
    public RectTransform backgroundRect;   // RectTransform of your BG Image
    public RectTransform propsContainer;   // empty overlay rect (same size as bg)
    public GameObject propPrefab;          // prefab with Image component

    [Header("Positioning")]
    public bool posIsNormalized01 = true;  // if true: pos is 0..1 across bg
    public bool yIsTopDown = true;         // if true: pos[1]=0 is top, 1 is bottom

    private readonly Dictionary<string, GameObject> _spawned = new();
    private ZoneData _zone;

    public float fadeDuration = 0.20f;

    private readonly System.Collections.Generic.Dictionary<string, Coroutine> _running = new();



    public void Build(ZoneData zone)
    {
        _zone = zone;

        Clear();

        if (_zone?.sceneProps == null || _zone.sceneProps.Count == 0)
            return;

        if (backgroundRect == null) { Debug.LogError("[Props] backgroundRect is NULL"); return; }
        if (propsContainer == null) { Debug.LogError("[Props] propsContainer is NULL"); return; }
        if (propPrefab == null) { Debug.LogError("[Props] propPrefab is NULL"); return; }

        // Initialize spawnHidden flags once per prop per zone (AFTER null checks, AFTER Clear)
        foreach (var p in _zone.sceneProps)
        {
            if (p == null) continue;
            if (!p.spawnHidden) continue;

            var key = (p.hideWhenFlag ?? "").Trim();
            if (string.IsNullOrEmpty(key)) continue;

            // Only set if it isn't already set (so once revealed, it stays revealed)
            if (WorldStateManager.Instance != null && !WorldStateManager.Instance.HasFlag(key))
                WorldStateManager.Instance.SetFlag(key);
        }

        foreach (var p in _zone.sceneProps)
        {
            if (p == null || string.IsNullOrEmpty(p.id)) continue;

            var go = Instantiate(propPrefab, propsContainer);
            go.name = $"Prop_{p.id}";

            var rt = (RectTransform)go.transform;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(p.SizeX, p.SizeY);
            rt.anchoredPosition = new Vector2(p.PosX, p.PosY);

            var pv = go.GetComponent<PropVisual>();
            if (pv == null) { Debug.LogError($"[Props] Prop prefab missing PropVisual (prop '{p.id}')"); continue; }

            if (pv.imageA == null || pv.imageB == null)
            {
                Debug.LogError($"[Props] PropVisual missing imageA/imageB (prop '{p.id}')");
                continue;
            }

            // Set initial sprite
            var spriteKey = ResolveSpriteKey(p);
            var sprite = LoadSprite(spriteKey);
            pv.ActiveImage.sprite = sprite;
            pv.InactiveImage.gameObject.SetActive(false);

            _spawned[p.id] = go;

            bool shouldBeVisible = ShouldBeVisible(p);

            // prevent 1-frame flash: if it starts hidden, spawn inactive immediately
            if (!shouldBeVisible)
                go.SetActive(false);

            // Run the *tracked* routine (so Clear() can stop it)
            if (_running.TryGetValue(p.id, out var existing) && existing != null)
                StopCoroutine(existing);

            _running[p.id] = StartCoroutine(ApplyPropStateRoutine(p.id, go, spriteKey, shouldBeVisible));
        }
    }

    public void RefreshVisibility()
    {
        if (_zone?.sceneProps == null) return;

        foreach (var p in _zone.sceneProps)
        {
            if (p == null || string.IsNullOrEmpty(p.id)) continue;
            if (!_spawned.TryGetValue(p.id, out var go) || go == null) continue;

            string spriteKey = ResolveSpriteKey(p);
            bool shouldBeVisible = ShouldBeVisible(p);

            // If state resolves to empty sprite, force hidden
            if (string.IsNullOrEmpty(spriteKey))
                shouldBeVisible = false;

            // Run one combined routine per prop so we don't fight ourselves
            if (_running.TryGetValue(p.id, out var existing) && existing != null)
                StopCoroutine(existing);

            _running[p.id] = StartCoroutine(ApplyPropStateRoutine(p.id, go, spriteKey, shouldBeVisible));
        }
    }

    private IEnumerator ApplyPropStateRoutine(string propId, GameObject go, string spriteKey, bool shouldBeVisible)
    {
        var pv = go.GetComponent<PropVisual>();
        if (pv == null)
        {
            // Fallback: if someone forgot PropVisual on prefab, do simple enable/disable
            go.SetActive(shouldBeVisible);
            yield break;
        }

        // Ensure we have canvas groups
        var activeImg = pv.ActiveImage;
        var inactiveImg = pv.InactiveImage;

        if (activeImg == null || inactiveImg == null)
            yield break;

        var activeCG = activeImg.GetComponent<CanvasGroup>() ?? activeImg.gameObject.AddComponent<CanvasGroup>();
        var inactiveCG = inactiveImg.GetComponent<CanvasGroup>() ?? inactiveImg.gameObject.AddComponent<CanvasGroup>();

        // If we should be hidden, fade the *active* image out then disable root
        if (!shouldBeVisible)
        {
            if (!go.activeSelf)
                yield break;

            // Make sure active is actually visible before fading
            activeImg.gameObject.SetActive(true);
            inactiveImg.gameObject.SetActive(false);
            inactiveCG.alpha = 0f;
            activeCG.alpha = Mathf.Clamp01(activeCG.alpha <= 0f ? 1f : activeCG.alpha);

            yield return FadeCanvasGroup(activeCG, activeCG.alpha, 0f, fadeDuration);
            go.SetActive(false);
            yield break;
        }

        // Should be visible
        if (!go.activeSelf)
        {
            go.SetActive(true);

            // Bring active back up cleanly
            activeImg.gameObject.SetActive(true);
            inactiveImg.gameObject.SetActive(false);
            inactiveCG.alpha = 0f;
            activeCG.alpha = 0f;

            yield return FadeCanvasGroup(activeCG, 0f, 1f, fadeDuration);
        }

        // Resolve sprite
        Sprite newSprite = LoadSprite(spriteKey);
        if (newSprite == null)
        {
            // If sprite can't load, fail safe: keep it visible but don't change sprite
            yield break;
        }

        // If sprite changed, crossfade
        if (pv.ActiveImage.sprite != newSprite)
        {
            yield return CrossfadeSprite(pv, newSprite, fadeDuration);
        }
        else
        {
            // Ensure alphas are sane
            pv.ActiveImage.gameObject.SetActive(true);
            pv.InactiveImage.gameObject.SetActive(false);
            activeCG.alpha = 1f;
            inactiveCG.alpha = 0f;
        }

        // cleanup
        _running[propId] = null;
    }

    private IEnumerator CrossfadeSprite(PropVisual pv, Sprite newSprite, float duration)
    {
        var fromImg = pv.ActiveImage;
        var toImg = pv.InactiveImage;

        var fromCG = fromImg.GetComponent<CanvasGroup>() ?? fromImg.gameObject.AddComponent<CanvasGroup>();
        var toCG = toImg.GetComponent<CanvasGroup>() ?? toImg.gameObject.AddComponent<CanvasGroup>();

        // Prep "to"
        toImg.sprite = newSprite;
        toImg.gameObject.SetActive(true);
        toCG.alpha = 0f;

        // Ensure "from" is visible
        fromImg.gameObject.SetActive(true);
        fromCG.alpha = 1f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / duration);

            fromCG.alpha = Mathf.Lerp(1f, 0f, lerp);
            toCG.alpha = Mathf.Lerp(0f, 1f, lerp);

            yield return null;
        }

        fromCG.alpha = 0f;
        toCG.alpha = 1f;

        fromImg.gameObject.SetActive(false);
        pv.Swap(); // now the "to" becomes active
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;

        float t = 0f;
        cg.alpha = from;

        while (t < duration)
        {
            if (cg == null) yield break;

            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / duration);
            cg.alpha = Mathf.Lerp(from, to, lerp);
            yield return null;
        }

        if (cg != null) cg.alpha = to;
    }

    public void SetPropVisible(string propId, bool visible)
    {
        if (_spawned.TryGetValue(propId, out var go))
            go.SetActive(visible);
    }

    public void Clear()
    {
        // Stop any running prop coroutines first
        foreach (var kv in _running)
            if (kv.Value != null) StopCoroutine(kv.Value);
        _running.Clear();

        // Destroy spawned GOs
        foreach (var kv in _spawned)
            if (kv.Value) Destroy(kv.Value);
        _spawned.Clear();
    }

    private bool ShouldBeVisible(ScenePropData p)
    {
        if (p == null) return false;

        var hideKey = (p.hideWhenFlag ?? "").Trim();
        if (string.IsNullOrEmpty(hideKey))
            return true;

        // hide when the flag exists
        return !WorldStateManager.Instance.HasFlag(hideKey);
    }

    private Sprite LoadSprite(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        // If JSON already includes "Props/..."
        if (key.StartsWith("Props/"))
            return Resources.Load<Sprite>(key);

        // Otherwise assume it's just the filename
        return Resources.Load<Sprite>($"Props/{key}");
    }

    private string ResolveSpriteKey(ScenePropData p)
    {
        // states are optional
        if (p.states != null)
        {
            foreach (var s in p.states)
            {
                if (s == null) continue;
                var flag = (s.whenFlag ?? "").Trim();
                if (flag.Length == 0) continue;

                if (WorldStateManager.Instance != null && WorldStateManager.Instance.HasFlag(flag))
                    return s.sprite; // may be null/empty to indicate hide if you want
            }
        }

        return p.sprite;
    }
}