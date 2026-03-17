using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryRowUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text nameText;
    public TMP_Text qtyText;
    public Image highlight;

    public Button button;

    public Item_Data Item { get; private set; }
    private Action<Item_Data> onClick;

    public void Bind(Item_Data stack, Action<Item_Data> click)
    {
        Item = stack;
        onClick = click;

        if (icon) icon.sprite = stack.texture;
        if (nameText) nameText.text = stack.itemName;
        if (qtyText) qtyText.text = $"x{stack.quantity}";

        if (!button) button = GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke(Item));
    }

    public void SetSelected(bool selected)
    {
        if (highlight) highlight.enabled = selected;
    }
}