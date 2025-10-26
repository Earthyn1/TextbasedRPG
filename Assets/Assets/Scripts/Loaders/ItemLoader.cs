using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class ItemLoader : MonoBehaviour
{
    [SerializeField] private TextAsset itemsJsonFile;
    [SerializeField] private bool verboseDebug = true;   // toggle in Inspector

    // Dictionary for fast lookup by itemID
    private Dictionary<string, Item_Data> itemDatabase;

    private void Awake()
    {
        LoadItems();
    }

    private void Start()
    {
        // Pass the loaded database to the InventoryManager singleton
        if (Inventory_Manager.Instance != null)
        {
            Inventory_Manager.Instance.SetItemDatabase(itemDatabase);
        }
        else
        {
            Debug.LogWarning("[ItemLoader] InventoryManager instance not found when initializing ItemLoader!");
        }
    }

    private void LoadItems()
    {
        if (itemsJsonFile == null)
        {
            Debug.LogError("[ItemLoader] Items JSON file not assigned!");
            itemDatabase = new Dictionary<string, Item_Data>();
            return;
        }

        try
        {
            var items = JsonConvert.DeserializeObject<List<Item_Data>>(itemsJsonFile.text);
            itemDatabase = new Dictionary<string, Item_Data>();

            foreach (var item in items)
            {
                // Load sprite using itemID as filename (Icons/itemID.png)
                item.texture = Resources.Load<Sprite>($"Icons/{item.itemID}");

                if (item.texture == null)
                    Debug.LogWarning($"[ItemLoader] ⚠ No texture found for itemID: {item.itemID}");

                if (!itemDatabase.ContainsKey(item.itemID))
                    itemDatabase[item.itemID] = item;




                else
                    Debug.LogWarning($"[ItemLoader] ⚠ Duplicate itemID in JSON: {item.itemID}");

                // --- Optional: Verbose debug info ---
                if (verboseDebug)
                {
                    if (item.IsConsumable())
                    {
                        var c = item.consumable;
                        // DEBUG BEGIN
                        Debug.Log($"[ItemLoader] {item.itemID} " +
                                  $"type={item.itemType} " +
                                  $"isConsumable={item.IsConsumable()} " +
                                  $"hasConsumableBlock={(item.consumable != null)} " +
                                  $"healFlat={(item.consumable != null ? item.consumable.healHPFlat.ToString() : "n/a")} " +
                                  $"healPct={(item.consumable != null ? item.consumable.healHPPercent.ToString() : "n/a")}");
                        // DEBUG END
                    }
                    else
                    {
                        Debug.Log($"[ItemLoader] 🧱 Loaded {item.itemID} ({item.itemType})");
                    }
                }
            }

            Debug.Log($"✅ [ItemLoader] Loaded {itemDatabase.Count} total items.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[ItemLoader] ❌ Failed to load items JSON: " + e.Message);
            itemDatabase = new Dictionary<string, Item_Data>();
        }
    }
}
