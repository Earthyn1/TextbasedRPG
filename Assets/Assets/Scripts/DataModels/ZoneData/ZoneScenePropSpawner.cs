using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ZoneScenePropSpawner : MonoBehaviour
{
    public static ZoneScenePropSpawner Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>Returns the RectTransform of a spawned prop by its prop id, or null if not found.</summary>
    public RectTransform GetPropRect(string propId)
    {
        Debug.Log($"[GetPropRect] Requested propId: {propId}");

        if (_spawned == null)
        {
            Debug.LogWarning("[GetPropRect] _spawned dictionary is NULL!");
            return null;
        }

        if (!_spawned.ContainsKey(propId))
        {
            var keys = string.Join(", ", _spawned.Keys);
            Debug.LogWarning($"[GetPropRect] propId '{propId}' NOT FOUND. Available keys: [{keys}]");
            return null;
        }

        var go = _spawned[propId];

        if (go == null)
        {
            Debug.LogWarning($"[GetPropRect] GameObject for '{propId}' is NULL!");
            return null;
        }

        var rect = go.GetComponent<RectTransform>();

        if (rect == null)
        {
            Debug.LogWarning($"[GetPropRect] '{propId}' has NO RectTransform component!");
            return null;
        }

        Debug.Log($"[GetPropRect] SUCCESS for '{propId}' → {go.name}");

        return rect;
    }

    [Header("Required refs")]
    public RectTransform backgroundRect;   // RectTransform of your BG Image
    public RectTransform propsContainer;   // empty overlay rect (same size as bg)
    public GameObject propPrefab;          // prefab with Image component
    public ZoneHitmaskInteractor hitmaskInteractor; // for prop-based hover + click

    [Header("Positioning")]
    public bool posIsNormalized01 = true;  // if true: pos is 0..1 across bg
    public bool yIsTopDown = true;         // if true: pos[1]=0 is top, 1 is bottom

    private readonly Dictionary<string, GameObject> _spawned       = new();
    /// <summary>Shadows are siblings of their prop, keyed by the same prop id.</summary>
    private readonly Dictionary<string, GameObject> _spawnedShadows = new();

    private readonly Dictionary<string, ScenePropData> _propsById = new();

    private ZoneData _zone;

    public float fadeDuration = 0.20f;

    private readonly Dictionary<string, Coroutine> _running = new();

    // ── Build ─────────────────────────────────────────────────────────────────

    public void Build(ZoneData zone)
    {
        _zone = zone;

        Clear();

        if (_zone?.sceneProps == null || _zone.sceneProps.Count == 0)
            return;

        if (backgroundRect == null) { Debug.LogError("[Props] backgroundRect is NULL"); return; }
        if (propsContainer == null) { Debug.LogError("[Props] propsContainer is NULL"); return; }
        if (propPrefab == null)     { Debug.LogError("[Props] propPrefab is NULL"); return; }

        foreach (var p in _zone.sceneProps)
        {
            if (p == null || string.IsNullOrEmpty(p.id)) continue;

            _propsById[p.id] = p;

            var spriteKey     = ResolveSpriteKey(p);
            bool shouldBeVisible = ShouldBeVisible(p);

            // ── Shadow (sibling, spawned first so it renders behind the prop) ──
            //
            // Looks for "<baseSpriteId>_Shadow" in the same Props/ folder.
            // Not every prop needs a shadow — if the asset doesn't exist we just skip.
            var shadowSprite = LoadSprite(p.sprite + "_Shadow");
            if (shadowSprite != null)
            {
                var shadowGo = new GameObject($"PropShadow_{p.id}");
                shadowGo.transform.SetParent(propsContainer, worldPositionStays: false);

                var shadowRt             = shadowGo.AddComponent<RectTransform>();
                shadowRt.pivot           = new Vector2(0.5f, 0.5f);
                shadowRt.sizeDelta       = new Vector2(p.SizeX, p.SizeY);
                shadowRt.anchoredPosition = new Vector2(0, 0);

                var shadowImg            = shadowGo.AddComponent<Image>();
                shadowImg.sprite         = shadowSprite;
                shadowImg.raycastTarget  = false; // shadows never receive pointer events
                shadowImg.preserveAspect = false; // match the prop's rect exactly

                shadowGo.AddComponent<CanvasGroup>(); // used for fade in/out

                _spawnedShadows[p.id] = shadowGo;

                if (!shouldBeVisible)
                    shadowGo.SetActive(false);
            }

            // ── Main prop (spawned after shadow → higher sibling index → draws on top) ──

            var go   = Instantiate(propPrefab, propsContainer);
            go.name  = $"Prop_{p.id}";

            var rt             = (RectTransform)go.transform;
            rt.pivot           = new Vector2(0.5f, 0.5f);
            rt.sizeDelta       = new Vector2(p.SizeX, p.SizeY);
            rt.anchoredPosition = new Vector2(0, 0);

            var pv = go.GetComponent<PropVisual>();
            if (pv == null) { Debug.LogError($"[Props] Prop prefab missing PropVisual (prop '{p.id}')"); continue; }

            if (pv.imageA == null || pv.imageB == null)
            {
                Debug.LogError($"[Props] PropVisual missing imageA/imageB (prop '{p.id}')");
                continue;
            }

            // Stretch both child images to fill the root rect so sizeDelta drives their size.
            // Also enable alpha-based hit testing so transparent pixels never receive clicks.
            // NOTE: each prop sprite must have Read/Write Enabled in its Texture Import Settings.
            foreach (var img in new[] { pv.imageA, pv.imageB })
            {
                var irt      = (RectTransform)img.transform;
                irt.anchorMin = Vector2.zero;
                irt.anchorMax = Vector2.one;
                irt.offsetMin = Vector2.zero;
                irt.offsetMax = Vector2.zero;

                img.alphaHitTestMinimumThreshold = 0.1f;
            }

            // Set initial sprite
            var sprite = LoadSprite(spriteKey);
            pv.ActiveImage.sprite = sprite;
            pv.InactiveImage.gameObject.SetActive(false);

            _spawned[p.id] = go;

            // ── Hit detection ──────────────────────────────────────────────────

            if (p.hitId > 0 && hitmaskInteractor != null)
            {
                byte hitIdByte = (byte)p.hitId;

                // ── DEBUG: confirm setup ──────────────────────────────────────
                var rootImage  = go.GetComponent<Image>();
                var childImages = go.GetComponentsInChildren<Image>(includeInactive: true);
              
               
                // ─────────────────────────────────────────────────────────────

                var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() =>
                {
                    
                    hitmaskInteractor.TriggerPropClick(hitIdByte);
                });

                var highlight  = go.AddComponent<PropHoverHighlight>();
                var trigger    = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();

                var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enterEntry.callback.AddListener(_ =>
                {
                   
                    hitmaskInteractor.SetPropHovered(hitIdByte);
                    highlight.SetHovered(true);
                });
                trigger.triggers.Add(enterEntry);

                var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exitEntry.callback.AddListener(_ => { hitmaskInteractor.ClearHover(); highlight.SetHovered(false); });
                trigger.triggers.Add(exitEntry);
            }
            else
            {
               
            }

            // prevent 1-frame flash: if it starts hidden, spawn inactive immediately
            if (!shouldBeVisible)
                go.SetActive(false);

            if (_running.TryGetValue(p.id, out var existing) && existing != null)
                StopCoroutine(existing);

            _running[p.id] = StartCoroutine(ApplyPropStateRoutine(p.id, go, spriteKey, shouldBeVisible));
        }
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    public void RefreshVisibility()
    {
        if (_zone?.sceneProps == null) return;

        foreach (var p in _zone.sceneProps)
        {
            if (p == null || string.IsNullOrEmpty(p.id)) continue;
            if (!_spawned.TryGetValue(p.id, out var go) || go == null) continue;

            string spriteKey     = ResolveSpriteKey(p);
            bool shouldBeVisible = ShouldBeVisible(p);

            if (string.IsNullOrEmpty(spriteKey))
                shouldBeVisible = false;

            if (_running.TryGetValue(p.id, out var existing) && existing != null)
                StopCoroutine(existing);

            _running[p.id] = StartCoroutine(ApplyPropStateRoutine(p.id, go, spriteKey, shouldBeVisible));
        }
    }

    // ── State routine ─────────────────────────────────────────────────────────

    private IEnumerator ApplyPropStateRoutine(string propId, GameObject go, string spriteKey, bool shouldBeVisible)
    {
        var pv = go.GetComponent<PropVisual>();
        if (pv == null)
        {
            go.SetActive(shouldBeVisible);
            SetShadowActive(propId, shouldBeVisible);
            yield break;
        }

        var activeImg   = pv.ActiveImage;
        var inactiveImg = pv.InactiveImage;

        if (activeImg == null || inactiveImg == null)
            yield break;

        var activeCG   = activeImg.GetComponent<CanvasGroup>()   ?? activeImg.gameObject.AddComponent<CanvasGroup>();
        var inactiveCG = inactiveImg.GetComponent<CanvasGroup>() ?? inactiveImg.gameObject.AddComponent<CanvasGroup>();

        // Grab shadow CanvasGroup once up front (null if no shadow for this prop)
        _spawnedShadows.TryGetValue(propId, out var shadowGo);
        var shadowCG = shadowGo != null ? shadowGo.GetComponent<CanvasGroup>() : null;

        // ── Hiding ────────────────────────────────────────────────────────────
        if (!shouldBeVisible)
        {
            if (!go.activeSelf)
                yield break;

            activeImg.gameObject.SetActive(true);
            inactiveImg.gameObject.SetActive(false);
            inactiveCG.alpha = 0f;
            activeCG.alpha   = Mathf.Clamp01(activeCG.alpha <= 0f ? 1f : activeCG.alpha);

            // Fade prop and shadow out in parallel (same duration, start together)
            if (shadowGo != null && shadowGo.activeSelf && shadowCG != null)
                StartCoroutine(FadeCanvasGroup(shadowCG, shadowCG.alpha, 0f, fadeDuration));

            yield return FadeCanvasGroup(activeCG, activeCG.alpha, 0f, fadeDuration);

            go.SetActive(false);
            if (shadowGo != null) shadowGo.SetActive(false);
            yield break;
        }

        // ── Showing ───────────────────────────────────────────────────────────
        if (!go.activeSelf)
        {
            go.SetActive(true);
            activeImg.gameObject.SetActive(true);
            inactiveImg.gameObject.SetActive(false);
            inactiveCG.alpha = 0f;
            activeCG.alpha   = 0f;

            // Bring shadow up in parallel
            if (shadowGo != null && shadowCG != null)
            {
                shadowGo.SetActive(true);
                shadowCG.alpha = 0f;
                StartCoroutine(FadeCanvasGroup(shadowCG, 0f, 1f, fadeDuration));
            }

            yield return FadeCanvasGroup(activeCG, 0f, 1f, fadeDuration);
        }

        // ── Sprite change ─────────────────────────────────────────────────────
        Sprite newSprite = LoadSprite(spriteKey);
        if (newSprite == null)
            yield break;

        if (pv.ActiveImage.sprite != newSprite)
            yield return CrossfadeSprite(pv, newSprite, fadeDuration);
        else
        {
            pv.ActiveImage.gameObject.SetActive(true);
            pv.InactiveImage.gameObject.SetActive(false);
            activeCG.alpha   = 1f;
            inactiveCG.alpha = 0f;
        }

        _running[propId] = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────


    public ScenePropData GetPropData(string propId)
    {
        if (_propsById.TryGetValue(propId, out var prop))
        {
            Debug.Log($"[Props] Found data for '{propId}' Size=({prop.SizeX}, {prop.SizeY})");
            return prop;
        }

        Debug.LogWarning($"[Props] No data found for '{propId}'");
        return null;
    }

    /// <summary>Instantly shows or hides a prop's shadow without fading.</summary>
    private void SetShadowActive(string propId, bool visible)
    {
        if (_spawnedShadows.TryGetValue(propId, out var shadowGo) && shadowGo != null)
            shadowGo.SetActive(visible);
    }

    private IEnumerator CrossfadeSprite(PropVisual pv, Sprite newSprite, float duration)
    {
        var fromImg = pv.ActiveImage;
        var toImg   = pv.InactiveImage;

        var fromCG = fromImg.GetComponent<CanvasGroup>() ?? fromImg.gameObject.AddComponent<CanvasGroup>();
        var toCG   = toImg.GetComponent<CanvasGroup>()   ?? toImg.gameObject.AddComponent<CanvasGroup>();

        toImg.sprite = newSprite;
        toImg.gameObject.SetActive(true);
        toCG.alpha = 0f;

        fromImg.gameObject.SetActive(true);
        fromCG.alpha = 1f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / duration);
            fromCG.alpha = Mathf.Lerp(1f, 0f, lerp);
            toCG.alpha   = Mathf.Lerp(0f, 1f, lerp);
            yield return null;
        }

        fromCG.alpha = 0f;
        toCG.alpha   = 1f;

        fromImg.gameObject.SetActive(false);
        pv.Swap();
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;

        float t  = 0f;
        cg.alpha = from;

        while (t < duration)
        {
            if (cg == null) yield break;
            t       += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
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
        foreach (var kv in _running)
            if (kv.Value != null) StopCoroutine(kv.Value);
        _running.Clear();

        foreach (var kv in _spawned)
            if (kv.Value) Destroy(kv.Value);
        _spawned.Clear();

        foreach (var kv in _spawnedShadows)
            if (kv.Value) Destroy(kv.Value);
        _spawnedShadows.Clear();
    }

    private bool ShouldBeVisible(ScenePropData p)
    {
        if (p == null) return false;

        var wsm = WorldStateManager.Instance;

        var hideKey = (p.hideWhenFlag ?? "").Trim();
        if (!string.IsNullOrEmpty(hideKey) && wsm.HasFlag(hideKey))
            return false;

        var showKey = (p.showWhenFlag ?? "").Trim();
        if (!string.IsNullOrEmpty(showKey))
            return wsm.HasFlag(showKey);

        return true;
    }

    private Sprite LoadSprite(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        if (key.StartsWith("Props/"))
            return Resources.Load<Sprite>(key);

        return Resources.Load<Sprite>($"Props/{key}");
    }

    private string ResolveSpriteKey(ScenePropData p)
    {
        if (p.states != null)
        {
            foreach (var s in p.states)
            {
                if (s == null) continue;
                var flag = (s.whenFlag ?? "").Trim();
                if (flag.Length == 0) continue;

                if (WorldStateManager.Instance != null && WorldStateManager.Instance.HasFlag(flag))
                    return s.sprite;
            }
        }

        return p.sprite;
    }
}
