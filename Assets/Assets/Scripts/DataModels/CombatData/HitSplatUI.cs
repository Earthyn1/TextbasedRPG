using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HitSplatUI : MonoBehaviour
{
    [Header("References")]
    public TMP_Text text;
    public Image image; // optional background image

    [Header("Timing")]
    public float holdDuration = 1f;      // fully visible time before fade
    public float fadeDuration = 0.2f;    // fade-out time
    public Vector3 startOffset = new Vector3(0f, 30f, 0f);

    private float _timer;
    private CanvasGroup _cg;
    private RectTransform _rt;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null)
            _cg = gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// Initializes the hitsplat.
    /// </summary>
    /// <param name="msg">Displayed text</param>
    /// <param name="textColor">Color of the text</param>
    /// <param name="bgColor">Color of the background image</param>
    /// <param name="scale">Visual scale (0.6 normal / 0.8 crit)</param>
    public void Init(string msg, Color textColor, Color bgColor, float scale)
    {
        _timer = 0f;
        text.text = msg;
        text.color = textColor;

        if (image != null)
            image.color = bgColor;

        _cg.alpha = 1f;
        transform.localScale = Vector3.one * scale;

        // optional start offset
        _rt.anchoredPosition += (Vector2)startOffset;
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        if (_timer <= holdDuration)
        {
            _cg.alpha = 1f;
        }
        else
        {
            float fadeT = (_timer - holdDuration) / fadeDuration;
            _cg.alpha = 1f - fadeT;

            if (_timer >= holdDuration + fadeDuration)
                Destroy(gameObject);
        }
    }
}
