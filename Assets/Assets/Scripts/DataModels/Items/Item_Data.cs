using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using UnityEngine;

[Serializable]
public class Item_Data
{
    // ---- Core identity / presentation ----
    public string itemID;
    public string itemName;
    public Sprite texture;        // assigned in Unity (not from JSON)
    public string description;

    [JsonConverter(typeof(StringEnumConverter))]
    public ItemType itemType = ItemType.NA;

    // ---- Inventory info ----
    public int quantity;
    public int maxQuantity = 1;

    // ---- Optional data blocks ----
    // Only meaningful if itemType == Consumable.
    public ConsumableData consumable;

    // NOTE:
    // We intentionally do NOT embed full equipment stats here because those live
    // in EquippableData, addressed by itemID.

    // ---- Static "empty" ----
    public static readonly Item_Data Empty = new Item_Data
    {
        itemID = "empty",
        itemName = "Empty",
        description = "No item",
        itemType = ItemType.NA,
        quantity = 0,
        maxQuantity = 0,
        consumable = null
    };

    // ---- Constructors ----
    public Item_Data() { }

    public Item_Data(string id, string name, Sprite tex, string desc, ItemType type, int qty, int maxQty)
    {
        itemID = id;
        itemName = name;
        texture = tex;
        description = desc;
        itemType = type;
        quantity = Mathf.Clamp(qty, 0, maxQty);
        maxQuantity = maxQty;
    }

    // ---- Helpers ----

    /// <summary>
    /// Returns true if this entry is effectively empty OR represents 0 quantity.
    /// </summary>
    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(itemID)
            || itemID == "empty"
            || quantity <= 0;
    }

    /// <summary>
    /// Returns true if this item is a usable consumable with valid data.
    /// </summary>
    public bool IsConsumable()
    {
        return itemType == ItemType.Consumable
            && consumable != null;
    }

    /// <summary>
    /// Returns true if this is equippable gear. The actual gear stats
    /// live in EquippableData and are looked up by itemID at equip time.
    /// </summary>
    public bool IsEquippable()
    {
        return itemType == ItemType.Equippable;
    }
}

[Serializable]
public class ConsumableData
{
    // Health restoration
    public int healHPFlat = 0;        // +X HP instantly
    public float healHPPercent = 0f;  // 0.25f = 25% MaxHP

    // Mana / resource restoration
    public int healMPFlat = 0;        // +X Mana instantly
    public float healMPPercent = 0f;  // 0.10f = 10% MaxMana

    // Usage rules
    public bool usableInCombat = true;
    public float cooldownSeconds = 0f;
}