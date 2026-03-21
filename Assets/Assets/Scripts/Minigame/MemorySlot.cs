using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One item card in the memory minigame.
/// Prefab needs: Image (itemImage), Image (slotBackground), Button.
/// </summary>
public class MemorySlot : MonoBehaviour
{
    [SerializeField] private Image itemImage;
    [SerializeField] private Image slotBackground;
    [SerializeField] private Image resultOverlay;  // new image, starts hidden, shown on result
    [SerializeField] private Button button;

    private static readonly Color NeutralColor  = new Color(1f,    1f,    1f,    1f);
    private static readonly Color CorrectColor  = new Color(0.35f, 0.85f, 0.35f, 1f);
    private static readonly Color WrongColor    = new Color(0.85f, 0.3f,  0.3f,  1f);
    private static readonly Color DimmedColor   = new Color(0.5f,  0.5f,  0.5f,  1f);

    public void Setup(Sprite sprite, Action onClick)
    {
        if (itemImage)     itemImage.sprite = sprite;
        if (resultOverlay) resultOverlay.gameObject.SetActive(false); // hidden until result
        if (button)        button.onClick.AddListener(() => onClick?.Invoke());
    }

    /// <summary>Hides the item image (question phase).</summary>
    public void HideItem()
    {
        if (itemImage) itemImage.enabled = false;
        if (button)    button.interactable = true;
    }

    /// <summary>Reveals result — green for correct, red for wrong.</summary>
    public void ShowResult(ResultType result)
    {
        if (itemImage) itemImage.enabled = true;
        if (button)    button.interactable = false;

        if (resultOverlay == null) return;

        resultOverlay.gameObject.SetActive(true);
        switch (result)
        {
            case ResultType.Correct: resultOverlay.color = new Color(CorrectColor.r, CorrectColor.g, CorrectColor.b, 0.5f); break;
            case ResultType.Wrong:   resultOverlay.color = new Color(WrongColor.r,   WrongColor.g,   WrongColor.b,   0.5f); break;
            case ResultType.Dimmed:  resultOverlay.color = new Color(WrongColor.r,   WrongColor.g,   WrongColor.b,   0.5f); break;
        }
    }

    public enum ResultType { Correct, Wrong, Dimmed }
}
