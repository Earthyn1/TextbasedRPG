using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Refs")]
    public InventoryManager inv;



    [Header("Scroll List")]
    public RectTransform content;          // ScrollRect.Content
    public InventoryRowUI rowPrefab;

    [Header("Header")]
    public TMP_Text bagSpaceText;          // "Bag Space: 12/30"

    [Header("Details Panel")]
    public Image detailIcon;
    public TMP_Text detailName;
    public TMP_Text detailDesc;            // supports TMP rich text
    public GameObject detailsPanel;


    [Header("Buttons")]
    public Button primaryButton;           // Equip / Use
    public TMP_Text primaryButtonLabel;
    public Button dropButton;

    private readonly List<InventoryRowUI> rows = new();
    private Item_Data selected;

    private void OnEnable()
    {
        if (!inv) inv = InventoryManager.Instance;
        if (inv)
        {
            inv.OnInventoryChanged += Refresh;
            Refresh();
        }
    }

    private void OnDisable()
    {
        if (inv) inv.OnInventoryChanged -= Refresh;
    }

    public void Refresh()
    {
        // Bag space (do this even when empty)
        if (bagSpaceText)
            bagSpaceText.text = $"Bag Space: {inv.UsedSlots}/{inv.MaxSlots}";

        // Rebuild list
        for (int i = 0; i < rows.Count; i++)
            Destroy(rows[i].gameObject);
        rows.Clear();

        var stacks = inv.Stacks;

        for (int i = 0; i < stacks.Count; i++)
        {
            var stack = stacks[i];
            var row = Instantiate(rowPrefab, content);
            row.Bind(stack, OnRowClicked);
            rows.Add(row);
        }

        // If empty: clear selection + UI state
        if (stacks.Count == 0)
        {
            selected = null;

            // clear any leftover highlight state (paranoia / safety)
            for (int i = 0; i < rows.Count; i++)
                rows[i].SetSelected(false);

            if (detailsPanel)
                detailsPanel.SetActive(false);

            return;
        }

        // Try to keep selection by itemID (if the stack still exists)
        if (selected != null)
            selected = inv.FindFirst(selected.itemID);

        // If selection is gone (e.g., you dropped the last of that stack), select first
        if (selected == null)
            Select(stacks[0]);
        else
            Select(selected);
    }

    private void OnRowClicked(Item_Data stack)
    {
        Select(stack);
    }

    private void Select(Item_Data stack)
    {
        selected = stack;

        // highlight
        foreach (var r in rows)
            r.SetSelected(selected != null && r.Item == selected);

        // details
        if (selected == null)
        {
            detailsPanel.SetActive(false);
            return;
        }

        detailsPanel.SetActive(true);
        detailName.text = selected.itemName;
        detailIcon.sprite = selected.texture;
        detailDesc.text = selected.description;

        if (detailName) detailName.text = selected.itemName;

        if (detailIcon) detailIcon.sprite = selected.texture;

        // Example: your potion line with green 12hp
        // You can build this from fields on Item_Data (heal amount, etc.)
        if (detailDesc)
        {
            // Replace this with your actual description logic
            string desc = selected.description;

            // If you have "healsAmount" etc, you can do:
            // desc = $"Heals <color=#4CAF50>{selected.healAmount}hp</color>\nCan be used during combat";

            detailDesc.text = desc;
        }

        // Buttons: Equip vs Use, etc (placeholder)
        if (primaryButtonLabel)
            primaryButtonLabel.text = selected.itemType == ItemType.Consumable ? "Use" : "Equip";
    }

    // Hook these to your buttons
    public void OnPrimaryPressed()
    {
        if (selected == null) return;

        if (selected.itemType == ItemType.Consumable)
        {
            // call your consume system, then remove 1
            inv.RemoveItemQuantity(selected.itemID, 1);
        }
        else
        {
            // equip flow
        }
    }

    public void OnDropPressed()
    {
        if (selected == null) return;

        string id = selected.itemID; // capture before remove
        inv.RemoveItem(id, 1, silent: false);

        // Optional: if that was the last stack, selection is cleared by Refresh()
        // If you don't rely on Refresh, you can also do:
        // if (inv.Stacks.Count == 0) ClearSelection();
    }

    private void ClearSelection()
    {
        selected = null;

        // turn off highlights
        foreach (var r in rows)
            r.SetSelected(false);

        // hide details
        if (detailsPanel) detailsPanel.SetActive(false);
    }
}