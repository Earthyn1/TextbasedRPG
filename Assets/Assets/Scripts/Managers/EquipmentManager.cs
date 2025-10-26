using System;
using System.Collections.Generic;
using UnityEngine;
using static EquippableData;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    // Current equipped items per slot (we store a 1-qty Item_Data for convenience)
    private readonly Dictionary<EquipSlot, Item_Data> equipped = new();

    // External DB (provided by EquippableLoader.Start -> SetEquipmentDatabase)
    private Dictionary<string, EquippableRow> equipDB;

    // UI/Event: slot changed (slot, new equipped Item_Data or null)
    public event Action<EquipSlot, Item_Data> OnEquippedChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        foreach (EquipSlot s in Enum.GetValues(typeof(EquipSlot)))
            if (s != EquipSlot.None) equipped[s] = null;
    }

    public void SetEquipmentDatabase(Dictionary<string, EquippableRow> db)
    {
        equipDB = db;
    }

    // === Public getters ===
    public Item_Data GetEquipped(EquipSlot slot) =>
        equipped.TryGetValue(slot, out var it) ? it : null;

    public IEnumerable<Item_Data> GetAllEquipped()
    {
        foreach (var kv in equipped)
            if (kv.Key != EquipSlot.None && kv.Value != null) yield return kv.Value;
    }

    // === Equip/Unequip API ===

    /// <summary>Equip a specific inventory item (reference from your inventory list). Consumes 1.</summary>
    public bool Equip(Item_Data invItem, bool preferOffhandIfPossible = false)
    {
        if (!CanEquip(invItem, out var reason))
        {
            Debug.LogWarning($"[EquipmentManager] Cannot equip: {reason}");
            return false;
        }

        var row = equipDB[invItem.itemID];

        // --- Hands & armor routing ---
        if (row.allowedSlot == EquipSlot.MainHand || row.allowedSlot == EquipSlot.OffHand)
        {
            // 🔒 Force OffHand-only items (e.g., shields) into OffHand
            if (row.allowedSlot == EquipSlot.OffHand)
            {
                // If main has a 2H, clear it (2H blocks offhand)
                if (HasTwoHander(EquipSlot.MainHand))
                    ForceUnequip(EquipSlot.MainHand);

                EquipInto(EquipSlot.OffHand, invItem);
                RecalculateStats();
                return true;
            }

            if (row.handedness == WeaponHanded.TwoHanded)
            {
                // 2H → main hand; must clear offhand
                ForceUnequip(EquipSlot.OffHand);
                EquipInto(EquipSlot.MainHand, invItem);
            }
            else
            {
                // One-handed logic
                if (preferOffhandIfPossible && row.canGoInOffhand)
                {
                    if (HasTwoHander(EquipSlot.MainHand))
                        ForceUnequip(EquipSlot.MainHand);
                    EquipInto(EquipSlot.OffHand, invItem);
                }
                else
                {
                    if (GetEquipped(EquipSlot.MainHand) == null)
                    {
                        EquipInto(EquipSlot.MainHand, invItem);
                    }
                    else if (row.canGoInOffhand)
                    {
                        if (HasTwoHander(EquipSlot.MainHand))
                            ForceUnequip(EquipSlot.MainHand);
                        EquipInto(EquipSlot.OffHand, invItem);
                    }
                    else
                    {
                        if (HasTwoHander(EquipSlot.MainHand))
                            ForceUnequip(EquipSlot.MainHand);
                        EquipInto(EquipSlot.MainHand, invItem);
                    }
                }
            }
        }
        else
        {
            // Armor slots: Head/Body/Legs
            EquipInto(row.allowedSlot, invItem);
        }

        RecalculateStats();
        return true;
    }


    /// <summary>Unequip to inventory.</summary>
    public void Unequip(EquipSlot slot)
    {
        ForceUnequip(slot);
        RecalculateStats();
    }

    /// <summary>Convenience: equip by itemID (right-click via ID). Takes 1 from inventory if present.</summary>
    public bool EquipByItemID(string itemID, bool preferOffhandIfPossible = false)
    {
        var inv = Inventory_Manager.Instance;
        if (inv == null) { Debug.LogWarning("[EquipmentManager] No Inventory_Manager found."); return false; }

        // Check availability
        if (inv.CountOf(itemID) <= 0)
        {
            Debug.LogWarning($"[EquipmentManager] '{itemID}' not found in inventory.");
            return false;
        }

        // Use the first stack entry as the reference; EquipInto will RemoveItem(itemID,1)
        var first = inv.FindFirst(itemID);
        if (first == null)
        {
            Debug.LogWarning($"[EquipmentManager] '{itemID}' not found (slots changed).");
            return false;
        }

        return Equip(first, preferOffhandIfPossible);
    }

    // === Internals ===

    private bool CanEquip(Item_Data invItem, out string reason)
    {
        reason = "";
        if (invItem == null || string.IsNullOrEmpty(invItem.itemID))
        {
            reason = "No item.";
            return false;
        }

        if (equipDB == null || !equipDB.TryGetValue(invItem.itemID, out var row))
        {
            reason = "Item is not equippable.";
            return false;
        }

        // Example requirement: player level
       // int playerLevel = PlayerStats.Instance != null ? PlayerStats.Instance.Level : int.MaxValue;
      //  if (playerLevel < row.levelRequirement)
      //  {
      //      reason = $"Requires level {row.levelRequirement}.";
      //      return false;
     //  }

        // Sanity for offhand-only rows
        if (row.allowedSlot == EquipSlot.OffHand && !row.canGoInOffhand)
        {
            reason = "This item cannot be used in OffHand.";
            return false;
        }

        return true;
    }

    private void EquipInto(EquipSlot slot, Item_Data invItem)
    {
        var row = equipDB[invItem.itemID];
        if (slot == EquipSlot.MainHand && row.handedness == WeaponHanded.TwoHanded)
            ForceUnequip(EquipSlot.OffHand);

        if (equipped[slot] != null)
            Inventory_Manager.Instance.AddItem(equipped[slot].itemID, 1, silent: true); // 🔇

        Inventory_Manager.Instance.RemoveItem(invItem.itemID, 1, silent: true); // 🔇

        var template = Inventory_Manager.Instance.GetItemTemplate(invItem.itemID);
        template.quantity = 1;
        equipped[slot] = template;

        GameLog_Manager.Instance.AddEntry($"Player equipped {template.itemName}", "#42A5F5");  // blue
        OnEquippedChanged?.Invoke(slot, equipped[slot]);
    }

    private void ForceUnequip(EquipSlot slot)
    {
        var cur = GetEquipped(slot);
        if (cur == null) return;

        Inventory_Manager.Instance.AddItem(cur.itemID, 1, silent: true); // 🔇
        equipped[slot] = null;

        GameLog_Manager.Instance.AddEntry($"Player unequipped {cur.itemName}", "#FFB300");    // amber
        OnEquippedChanged?.Invoke(slot, null);
    }


    private bool HasTwoHander(EquipSlot slot)
    {
        var cur = GetEquipped(slot);
        if (cur == null) return false;
        var row = equipDB.TryGetValue(cur.itemID, out var r) ? r : null;
        return row != null && row.handedness == WeaponHanded.TwoHanded;
    }

    private void RecalculateStats()
    {
        if (PlayerStats.Instance == null) return;
        PlayerStats.Instance.RecalculateFromEquipment(GetAllEquipped());
    }

    // Public accessors for equip rows
    public bool TryGetEquipRow(string itemID, out EquippableRow row)
    {
        row = null;
        return equipDB != null && equipDB.TryGetValue(itemID, out row);
    }

    public EquippableRow GetEquipRowOrNull(string itemID)
    {
        return (equipDB != null && equipDB.TryGetValue(itemID, out var r)) ? r : null;
    }
}
