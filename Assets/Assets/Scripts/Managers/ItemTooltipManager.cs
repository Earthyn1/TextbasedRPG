using UnityEngine;
using TMPro;

public class ItemTooltipManager : MonoBehaviour
{
    public static ItemTooltipManager Instance { get; private set; }

    [Header("Prefab refs")]
    [SerializeField] private RectTransform root;     // panel RectTransform
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI subtitle;
    [SerializeField] private TextMeshProUGUI stats;

    [Header("Positioning")]
    [SerializeField] private Vector2 anchorOffset = new Vector2(0f, 100f); // 100 units above
    [SerializeField] private Vector2 screenPadding = new Vector2(12, 12);

    private RectTransform parentRT;
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        parentRT = root.parent as RectTransform;
        Hide();
    }

    public void ShowAt(RectTransform anchor, Item_Data item)
    {
        if (item == null || anchor == null) { Hide(); return; }

        // Fill
        title.text = item.itemName;
        subtitle.text = "Common";


        string statText = "";

        if (EquipmentManager.Instance != null &&
            EquipmentManager.Instance.TryGetEquipRow(item.itemID, out var row))
        {
            // --- Core stats ---
            Append(ref statText, "Strength", row.strength);
            Append(ref statText, "Defence", row.defence);
            Append(ref statText, "Fortitude", row.fortitude);
            Append(ref statText, "Precision", row.precision);
            Append(ref statText, "Aether", row.aether);

            // --- Combat stats ---
            Append(ref statText, "Damage", row.damage);
            AppendFloat(ref statText, "Attack Speed", row.attackSpeed);
            Append(ref statText, "Block", row.block);
            Append(ref statText, "Elemental Aether", row.elementalAether);

            // --- Specials ---
            AppendPercent(ref statText, "Crit Chance", row.critChance);
            AppendPercent(ref statText, "Crit Multiplier", row.critMultiplier);
            AppendPercent(ref statText, "Dodge Chance", row.dodgeChance);
        }
        else statText = "<color=#888>Not equippable</color>";
        stats.text = statText;

        // Show
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        root.gameObject.SetActive(true);

        // Position relative to anchor
        Vector2 anchorWorld = anchor.TransformPoint(anchor.rect.center);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT, anchorWorld, null, out var local);
        local += anchorOffset;

        // Clamp to parent rect
        var size = root.sizeDelta;
        var half = size * 0.5f;
        var min = parentRT.rect.min + (Vector2)screenPadding + half;
        var max = parentRT.rect.max - (Vector2)screenPadding - half;
        local.x = Mathf.Clamp(local.x, min.x, max.x);
        local.y = Mathf.Clamp(local.y, min.y, max.y);

        root.anchoredPosition = local;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        root.gameObject.SetActive(false);
    }

    // helpers
    private static void Append(ref string s, string label, int v) { if (v != 0) s += $"{label}: <b>{v}</b>\n"; }
    private static void AppendFloat(ref string s, string label, float v) { if (!Mathf.Approximately(v, 0f)) s += $"{label}: <b>{v:0.##}</b>\n"; }
    private void AppendPercent(ref string text, string label, float value)
    {
        if (Mathf.Abs(value) > 0.0001f)
            text += $"\n{label}: {value * 100f:+0.#;-0.#}%";
    }

}
