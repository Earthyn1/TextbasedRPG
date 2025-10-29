using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static class LootRoller
{
    // Result struct for clarity
    public struct LootResult
    {
        public List<(string itemId, int qty)> items;
        public int gold;
        public int xp;
    }

    public static LootResult Roll(LootTableDef table)
    {
        LootResult result = new LootResult
        {
            items = new List<(string itemId, int qty)>(),
            gold = 0,
            xp = 0
        };

        if (table == null)
            return result;

        // 1. XP from the kill
        result.xp = table.xpOnKill;

        // 2. Gold roll (if any)
        if (table.goldMax > 0 || table.goldMin > 0)
        {
            int gMin = Mathf.Min(table.goldMin, table.goldMax);
            int gMax = Mathf.Max(table.goldMin, table.goldMax);
            result.gold = Random.Range(gMin, gMax + 1); // inclusive max
        }

        // 3. Item rolls
        if (table.drops != null)
        {
            foreach (var drop in table.drops)
            {
                // chance gate
                float r = Random.value;
                if (r > drop.dropChance)
                    continue;

                // quantity roll
                int qtyMin = Mathf.Min(drop.minQty, drop.maxQty);
                int qtyMax = Mathf.Max(drop.minQty, drop.maxQty);
                int qty = Random.Range(qtyMin, qtyMax + 1);

                if (qty > 0 && !string.IsNullOrEmpty(drop.itemId))
                {
                    result.items.Add((drop.itemId, qty));
                }
            }
        }

        // 4. Fallback (ex: always drop at least 1 bone or whatever)
        if (result.items.Count == 0 && table.shouldFallback)
        {
            if (!string.IsNullOrEmpty(table.fallbackItemId) && table.fallbackQty > 0)
            {
                result.items.Add((table.fallbackItemId, table.fallbackQty));
            }
        }

        return result;
    }
}
