using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Brightens the prop's own Image on hover by tweening its color.
/// No child GameObjects required.
/// </summary>
public class PropHoverHighlight : MonoBehaviour
{
    [SerializeField] private float fadeSpeed    = 10f;
    [SerializeField] private float dimmedBrightness = 0.8f; // normal state

    private static readonly Color HoverColor = Color.white;

    private Image     _image;
    private Color     _normalColor;
    private Coroutine _coroutine;

    private void Awake()
    {
        _image = GetComponentInChildren<Image>();
        if (_image == null) return;

        _normalColor    = new Color(dimmedBrightness, dimmedBrightness, dimmedBrightness, 1f);
        _image.color    = _normalColor;
    }

    public void SetHovered(bool hovered)
    {
        if (_image == null) return;
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(TweenColor(hovered ? HoverColor : _normalColor));
    }

    private IEnumerator TweenColor(Color target)
    {
        Color start = _image.color;
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.MoveTowards(t, 1f, fadeSpeed * Time.deltaTime);
            _image.color = Color.Lerp(start, target, t);
            yield return null;
        }
        _image.color = target;
    }
}
