using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    private Dictionary<string, Item_Data> itemDatabase;

    // Stacks (your current approach)
    [SerializeField] private List<Item_Data> stacks = new();

    public IReadOnlyList<Item_Data> Stacks => stacks;

    public event Action OnInventoryChanged;
    public event Action<string, int> OnItemDelta; // (itemId, +qty / -qty)

    [Header("Capacity")]
    [SerializeField] private int maxSlots = 30;
    public int MaxSlots => maxSlots;
    public int UsedSlots => stacks.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Called by ItemLoader
    public void SetItemDatabase(Dictionary<string, Item_Data> db) => itemDatabase = db;

    public Item_Data GetItemDefinition(string itemId)
    {
        if (itemDatabase != null && itemDatabase.TryGetValue(itemId, out var def))
            return def;

        Debug.LogWarning($"ItemID '{itemId}' not found in item database.");
        return null;
    }

    public int CountOf(string itemId) => stacks.Where(s => s.itemID == itemId).Sum(s => s.quantity);

    public Item_Data FindFirst(string itemId) => stacks.FirstOrDefault(s => s.itemID == itemId);

    public bool HasSpaceForNewStack() => stacks.Count < maxSlots;

    public void AddItem(string itemId, int amount, bool silent = false)
    {
        if (amount <= 0) return;

        var template = GetItemDefinition(itemId);
        if (template == null) return;

        int remaining = amount;
        int totalAdded = 0;

        // Fill existing stacks first
        for (int i = 0; i < stacks.Count && remaining > 0; i++)
        {
            var stack = stacks[i];
            if (stack.itemID != itemId) continue;
            if (stack.quantity >= stack.maxQuantity) continue;

            int space = stack.maxQuantity - stack.quantity;
            int toAdd = Mathf.Min(space, remaining);

            stack.quantity += toAdd;
            remaining -= toAdd;
            totalAdded += toAdd;
        }

        // Create new stacks as needed
        while (remaining > 0 && stacks.Count < maxSlots)
        {
            int toAdd = Mathf.Min(template.maxQuantity, remaining);
            var newStack = ItemFactory.CloneWithQuantity(template, toAdd);
            stacks.Add(newStack);

            remaining -= toAdd;
            totalAdded += toAdd;
        }

        if (totalAdded > 0)
        {
            OnInventoryChanged?.Invoke();
            OnItemDelta?.Invoke(itemId, totalAdded);
        }

        if (remaining > 0)
            Debug.LogWarning($"Inventory full. Could not add {remaining} of {itemId}.");
    }

    public bool RemoveItem(string itemId, int amount, bool silent = false)
    {
        if (amount <= 0) return true;

        int remaining = amount;
        int totalRemoved = 0;

        // Remove from stacks in order
        for (int i = stacks.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var stack = stacks[i];
            if (stack.itemID != itemId) continue;

            int take = Mathf.Min(stack.quantity, remaining);
            stack.quantity -= take;

            remaining -= take;
            totalRemoved += take;

            if (stack.quantity <= 0)
                stacks.RemoveAt(i);
        }

        if (totalRemoved > 0)
        {
            OnInventoryChanged?.Invoke();
            OnItemDelta?.Invoke(itemId, -totalRemoved);
        }

        return remaining <= 0;
    }

    // Same helpers you had
    public void TakeClamped(string itemId, int amt)
    {
        int have = Mathf.Max(0, CountOf(itemId));
        int rm = Mathf.Clamp(amt, 0, have);
        if (rm > 0) RemoveItem(itemId, rm, silent: true);
    }

    public void RemoveItemQuantity(string itemId, int amount) => RemoveItem(itemId, amount, silent: true);
}