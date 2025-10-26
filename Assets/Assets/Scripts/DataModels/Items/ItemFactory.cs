using UnityEngine;

public static class ItemFactory
{
    // Clone an item definition (from the master DB) with a runtime quantity.
    public static Item_Data CloneWithQuantity(Item_Data baseItem, int qty)
    {
        if (baseItem == null) return null;

        var clone = new Item_Data
        {
            itemID = baseItem.itemID,
            itemName = baseItem.itemName,
            description = baseItem.description,
            itemType = baseItem.itemType,
            maxQuantity = baseItem.maxQuantity,
            texture = baseItem.texture, // safe to re-use sprite reference
            quantity = qty
        };

        // deep copy consumable block if present
        if (baseItem.consumable != null)
        {
            clone.consumable = new ConsumableData
            {
                healHPFlat = baseItem.consumable.healHPFlat,
                healHPPercent = baseItem.consumable.healHPPercent,
                healMPFlat = baseItem.consumable.healMPFlat,
                healMPPercent = baseItem.consumable.healMPPercent,
                usableInCombat = baseItem.consumable.usableInCombat,
                cooldownSeconds = baseItem.consumable.cooldownSeconds
            };
        }

        return clone;
    }
}
