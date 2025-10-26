using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    // Public progress bar
    public Image progressBar;
    public Image damageOverlayBar;   // secondary (behind main)

    public TMP_Text Text;

    [Header("Settings")]
    private readonly float overlayLerpSpeed = 1.0f; // how fast the red overlay catches up

    private float targetFill;   // where we *want* the main bar
    private float overlayFill;  // current value of overlay bar

    // Current progress value (0-1)
    [Range(0f, 1f)]
    public float progress = 0f;

    // Max time for timer display
    public float maxTime = 10f; // e.g., 10 seconds

    // Show timer instead of percentage
    public bool showTimer = false;

    private void Awake()
    {
        if (progressBar != null)
            targetFill = progressBar.fillAmount;
        if (damageOverlayBar != null)
            overlayFill = damageOverlayBar.fillAmount;
    }

    public void SetupOverlayFill()
    {
        overlayFill = 1;
    }
    private void Update()
    {
        if (damageOverlayBar != null)
        {
            // Interpolate the overlay down to match targetFill
            overlayFill = Mathf.MoveTowards(overlayFill, targetFill, overlayLerpSpeed * Time.deltaTime);
            damageOverlayBar.fillAmount = overlayFill;
        }
    }

    public void SetProgress(float value, string text = "")
    {
        targetFill = Mathf.Clamp01(value);

        // Snap the main bar instantly
        if (progressBar != null)
            progressBar.fillAmount = targetFill;

        // Initialize overlay if it was above new value
        if (damageOverlayBar != null && overlayFill < targetFill)
            overlayFill = targetFill; // heal → overlay jumps instantly up

        if (Text != null)
            Text.text = text;
    }

    // Optional: reset progress
    public void ResetProgress()
    {
        progress = 0f;
    }
}
