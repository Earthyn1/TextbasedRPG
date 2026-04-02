using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    public Image progressBar;
    public Image damageOverlayBar;
    public TMP_Text Text;

    [Header("Settings")]
    [SerializeField] private float overlayLerpSpeed = 1.0f;

    private float targetFill;
    private float overlayFill;

    private void Awake()
    {
        if (progressBar != null)
            targetFill = progressBar.fillAmount;

        if (damageOverlayBar != null)
        {
            overlayFill = damageOverlayBar.fillAmount;
        }
        else
        {
            overlayFill = targetFill;
        }
    }

    private void Update()
    {
        if (damageOverlayBar == null)
            return;

        overlayFill = Mathf.MoveTowards(overlayFill, targetFill, overlayLerpSpeed * Time.deltaTime);
        damageOverlayBar.fillAmount = overlayFill;
    }

    public void SetupOverlayFill()
    {
        if (progressBar != null)
            targetFill = progressBar.fillAmount;

        overlayFill = targetFill;

        if (damageOverlayBar != null)
            damageOverlayBar.fillAmount = overlayFill;
    }

    public void SetProgress(float value, string text = "")
    {
        targetFill = Mathf.Clamp01(value);

        if (progressBar != null)
            progressBar.fillAmount = targetFill;

        // Heal: overlay should jump up instantly
        if (damageOverlayBar != null && overlayFill < targetFill)
        {
            overlayFill = targetFill;
            damageOverlayBar.fillAmount = overlayFill;
        }

        if (Text != null)
            Text.text = text;
    }

    public void ResetProgress()
    {
        targetFill = 0f;
        overlayFill = 0f;

        if (progressBar != null)
            progressBar.fillAmount = 0f;

        if (damageOverlayBar != null)
            damageOverlayBar.fillAmount = 0f;
    }
}