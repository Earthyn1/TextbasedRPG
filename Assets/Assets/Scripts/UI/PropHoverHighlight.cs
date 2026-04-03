using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders a soft glow around the prop on hover using a custom UI shader.
/// Targets PropVisual.imageA and imageB directly so the glow always follows
/// whichever image is currently visible (live sprite, dead sprite, etc.).
/// Never modifies Image.color — sprites always render at full fidelity.
/// </summary>
public class PropHoverHighlight : MonoBehaviour
{
    [Header("Glow")]
    [SerializeField] private Color glowColor   = new Color(0.655f, 0.686f, 1f, 1f);
    [SerializeField] private float glowRadius  = 0.0026f;
    [SerializeField] private float glowFalloff = 2.5f;

    [Header("Animation")]
    [SerializeField] private float fadeSpeed = 10f;

    private static readonly int StrengthProp = Shader.PropertyToID("_GlowStrength");
    private static readonly int ColorProp    = Shader.PropertyToID("_GlowColor");
    private static readonly int RadiusProp   = Shader.PropertyToID("_GlowRadius");
    private static readonly int FalloffProp  = Shader.PropertyToID("_GlowFalloff");

    private readonly List<Material> _mats = new();
    private Coroutine _coroutine;

    private void Awake()
    {
        var shader = Shader.Find("UI/PropGlow");
        if (shader == null)
        {
            Debug.LogError("[PropHoverHighlight] Shader 'UI/PropGlow' not found.");
            return;
        }

        // Prefer PropVisual so we target the actual sprite images (imageA + imageB),
        // not the transparent root Image that acts as the button hit-area.
        var pv = GetComponent<PropVisual>();
        if (pv != null)
        {
            SetupImage(pv.imageA, shader);
            SetupImage(pv.imageB, shader);
        }
        else
        {
            // Fallback for simple props that only have one Image child.
            // Skip the root Image (hit area) by searching only true children.
            foreach (Transform child in transform)
            {
                var img = child.GetComponent<Image>();
                if (img != null) { SetupImage(img, shader); break; }
            }
        }
    }

    private void SetupImage(Image img, Shader shader)
    {
        if (img == null) return;
        var mat = new Material(shader);
        mat.SetColor(ColorProp,    glowColor);
        mat.SetFloat(RadiusProp,   glowRadius);
        mat.SetFloat(FalloffProp,  glowFalloff);
        mat.SetFloat(StrengthProp, 0f);
        img.material = mat;
        img.color    = Color.white;
        _mats.Add(mat);
    }

    private void OnDestroy()
    {
        foreach (var m in _mats)
            if (m != null) Destroy(m);
        _mats.Clear();
    }

    public void SetHovered(bool hovered)
    {
        if (_mats.Count == 0) return;
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(TweenGlow(hovered ? 1f : 0f));
    }

    private IEnumerator TweenGlow(float target)
    {
        float current = _mats[0].GetFloat(StrengthProp);
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.MoveTowards(t, 1f, fadeSpeed * Time.deltaTime);
            float v = Mathf.Lerp(current, target, t);
            foreach (var m in _mats) m.SetFloat(StrengthProp, v);
            yield return null;
        }
        foreach (var m in _mats) m.SetFloat(StrengthProp, target);
    }
}
