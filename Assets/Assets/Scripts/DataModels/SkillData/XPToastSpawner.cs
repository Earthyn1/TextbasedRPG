using System.Collections;
using UnityEngine;

public class XPToastSpawner : MonoBehaviour
{
    public static XPToastSpawner Instance { get; private set; }

    [Header("XP Toast UI")]
    public GameObject xppanel;   // parent container where toasts spawn (like a vertical group)
    public GameObject XPPrefab;  // the floating XP prefab with XPNumbers on it

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ShowXPToast(string xpText, Sprite xpIcon)
    {
        // 1. spawn the prefab under the panel
        GameObject go = Object.Instantiate(XPPrefab, xppanel.transform, false);

        // 2. find XPNumbers (it might be on root or a child)
        var xpNumbers = go.GetComponentInChildren<XPNumbers>();
        if (xpNumbers == null)
        {
            Debug.LogError("XPToastSpawner: XPNumbers component not found on spawned prefab.");
            return;
        }

        // 3. assign data BEFORE animation plays
        if (xpNumbers._Text != null)
            xpNumbers._Text.text = xpText;

        if (xpNumbers._Image != null)
            xpNumbers._Image.sprite = xpIcon;

        // 4. animation will auto-fire in XPNumbers.Start() via Animator.SetTrigger("Play")
        // 5. when animation finishes, Animation Event calls XPNumbers.DestroySelf()
    }

    /// <summary>Shows the XP toast after a delay in seconds.</summary>
    public void ShowXPToastDelayed(string xpText, Sprite xpIcon, float delay)
    {
        if (delay <= 0f) { ShowXPToast(xpText, xpIcon); return; }
        StartCoroutine(DelayedToast(xpText, xpIcon, delay));
    }

    private IEnumerator DelayedToast(string xpText, Sprite xpIcon, float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowXPToast(xpText, xpIcon);
    }
}
