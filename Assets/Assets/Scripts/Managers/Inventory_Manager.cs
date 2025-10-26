using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Inventory_Manager : MonoBehaviour
{
    public static Inventory_Manager Instance { get; private set; }

    private Dictionary<string, Item_Data> itemDatabase;

    [Header("Inventory Data")]
    public List<Item_Data> inventory = new List<Item_Data>();
    public event Action OnInventoryChanged;

    [Header("UI References")]
    public RectTransform InvPanel1;
    public RectTransform InvPanel2;
    public RectTransform InvPanel3;

    public Inventory_Slot slotPrefab;

    [Header("Settings")]
    public int slotsPerPanel = 21;

    public List<Inventory_Slot> slots = new List<Inventory_Slot>();

    public event Action<string, int> OnItemDelta; // (itemId, +qty for adds, -qty for removes)

    private void EmitItemDelta(string itemId, int delta)
    {
        if (delta != 0) OnItemDelta?.Invoke(itemId, delta);
    }

    private void Awake()
    {
        // singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        CreateSlots(InvPanel3);
        // CreateSlots(InvPanel2);
        UpdateSlots();
    }

    // Called by ItemLoader on Start()
    public void SetItemDatabase(Dictionary<string, Item_Data> db)
    {
        itemDatabase = db;
    }

    // Get canonical item def (no quantity). Do NOT hand this straight to inventory for stacking.
    public Item_Data GetItemDefinition(string itemID)
    {
        if (itemDatabase != null && itemDatabase.TryGetValue(itemID, out var def))
            return def;

        Debug.LogWarning($"ItemID '{itemID}' not found in InventoryManager database.");
        return null;
    }

    // Backwards-compat alias (safe now, returns definition directly)
    public Item_Data GetItemTemplate(string itemID)
    {
        return GetItemDefinition(itemID);
    }

    private void CreateSlots(RectTransform panel)
    {
        for (int i = 0; i < slotsPerPanel; i++)
        {
            Inventory_Slot newSlot = Instantiate(slotPrefab, panel);
            newSlot.name = $"Slot_{panel.name}_{i + 1}";
            newSlot.Initialize(this);
            newSlot.SetEmpty();
            slots.Add(newSlot);
        }
    }

    public void UpdateSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < inventory.Count)
                slots[i].SetSlot(inventory[i]);
            else
                slots[i].SetEmpty();
        }
    }

    public void AddItem(string itemId, int amount, bool silent = false)
    {
        if (amount <= 0) return;

        // Grab the canonical template with all data (including consumable)
        if (itemDatabase == null || !itemDatabase.TryGetValue(itemId, out Item_Data template))
        {
            Debug.LogWarning($"ItemID '{itemId}' not found in inventory database.");
            return;
        }

        int remaining = amount;
        int totalAdded = 0;

        // 1. Fill existing stacks
        foreach (var stack in inventory)
        {
            if (stack.itemID == itemId && stack.quantity < stack.maxQuantity)
            {
                int space = stack.maxQuantity - stack.quantity;
                int toAdd = Mathf.Min(space, remaining);

                stack.quantity += toAdd;
                remaining -= toAdd;
                totalAdded += toAdd;

                if (!silent && toAdd > 0)
                    GameLog_Manager.Instance.AddEntry($"Player received {toAdd} {stack.itemName}(s).");

                if (remaining <= 0)
                {
                    UpdateSlots();
                    OnInventoryChanged?.Invoke();
                    EmitItemDelta(itemId, totalAdded);
                    return;
                }
            }
        }

        // 2. How many new stacks can we spawn?
        int emptySlots = slots.Count - inventory.Count;
        int neededSlots = Mathf.CeilToInt((float)remaining / template.maxQuantity);
        if (neededSlots > emptySlots)
        {
            int possible = emptySlots * template.maxQuantity;
            Debug.LogWarning($"Not enough space in inventory! Could only add {possible} {template.itemName}(s).");
            remaining = possible;
        }

        // 3. Spawn new stacks (CLONE WITH CONSUMABLE DATA!)
        while (remaining > 0 && inventory.Count < slots.Count)
        {
            int toAdd = Mathf.Min(remaining, template.maxQuantity);

            var newStack = ItemFactory.CloneWithQuantity(template, toAdd);
            inventory.Add(newStack);

            remaining -= toAdd;
            totalAdded += toAdd;

            if (!silent && toAdd > 0)
                GameLog_Manager.Instance.AddEntry($"Player received {toAdd} {template.itemName}(s).", "#4CAF50");
        }

        UpdateSlots();
        OnInventoryChanged?.Invoke();
        EmitItemDelta(itemId, totalAdded);
    }

    public bool RemoveItem(string name, int amount, bool silent = false)
    {
        name = name.Trim();
        int remaining = amount;
        int totalRemoved = 0;

        var stacks = inventory.Where(s => s.itemID == name).ToList();
        if (stacks.Count == 0)
        {
            Debug.LogWarning($"{name} not found in inventory.");
            if (!silent)
                GameLog_Manager.Instance.AddEntry($"Failed to remove {amount} {name}(s): none in inventory.");
            return false;
        }

        foreach (var stack in new List<Item_Data>(stacks))
        {
            if (stack.quantity >= remaining)
            {
                stack.quantity -= remaining;
                totalRemoved += remaining;

                if (!silent)
                    GameLog_Manager.Instance.AddEntry($"Player removed {remaining} {name}(s).", "#E53935");

                if (stack.quantity <= 0)
                    inventory.Remove(stack);

                UpdateSlots();
                OnInventoryChanged?.Invoke();
                EmitItemDelta(name, -totalRemoved);
                return true;
            }
            else
            {
                remaining -= stack.quantity;
                totalRemoved += stack.quantity;
                inventory.Remove(stack);
            }
        }

        if (remaining > 0)
        {
            Debug.LogWarning($"Not enough {name} to remove. Removed what was available.");
            if (!silent)
                GameLog_Manager.Instance.AddEntry($"Could not remove {remaining} {name}(s): not enough in inventory.");
            UpdateSlots();
            OnInventoryChanged?.Invoke();
            EmitItemDelta(name, -totalRemoved);
            return false;
        }

        UpdateSlots();
        OnInventoryChanged?.Invoke();
        EmitItemDelta(name, -totalRemoved);
        return true;
    }

    // ✅ Put TakeClamped back in
    // This is called by your ApplyOutcome() logic.
    public void TakeClamped(string id, int amt)
    {
        int have = Mathf.Max(0, CountOf(id));
        int rm = Mathf.Clamp(amt, 0, have);
        if (rm > 0)
        {
            // We want silent=true here because story scripts probably shouldn't spam "Player removed X..."
            RemoveItem(id, rm, silent: true);
        }
    }

    public void RemoveItemQuantity(string itemID, int amount)
    {
        // This is what FoodUseSystem calls to consume 1 bread, etc.
        // We call silent:true to avoid double-log ("You eat bread" AND "Removed bread").
        RemoveItem(itemID, amount, silent: true);
    }

    public int CountOf(string itemID)
    {
        int total = 0;
        foreach (var s in inventory)
            if (s.itemID == itemID)
                total += s.quantity;
        return total;
    }

    public Item_Data FindFirst(string itemID)
    {
        return inventory.Find(s => s.itemID == itemID);
    }
}
