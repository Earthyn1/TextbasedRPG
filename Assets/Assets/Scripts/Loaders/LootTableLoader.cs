using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class LootTableLoader : MonoBehaviour
{
    [SerializeField] private TextAsset lootJsonFile;

    // hold onto the parsed dict until Start()
    private Dictionary<string, LootTableDef> _parsedLootTables = new Dictionary<string, LootTableDef>();

    private void Awake()
    {
        ParseLootTables();
    }

    private void Start()
    {
        // by Start(), NPCData_Manager.Instance should definitely exist
        if (NPCData_Manager.Instance == null)
        {
            Debug.LogError("❌ NPCData_Manager.Instance is STILL null in Start() of LootTableLoader. Cannot register loot tables.");
            return;
        }

        NPCData_Manager.Instance.SetAllLootTables(_parsedLootTables);
    }

    private void ParseLootTables()
    {
        if (lootJsonFile == null)
        {
            Debug.LogError("❌ Loot JSON file not assigned!");
            _parsedLootTables = new Dictionary<string, LootTableDef>();
            return;
        }

        try
        {
            var list = JsonConvert.DeserializeObject<List<LootTableDef>>(lootJsonFile.text);

            var dict = new Dictionary<string, LootTableDef>();
            foreach (var table in list)
            {
                if (!string.IsNullOrEmpty(table.id))
                    dict[table.id] = table;
            }

            _parsedLootTables = dict;

            Debug.Log($"✅ Loaded {_parsedLootTables.Count} loot tables.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Failed to load loot JSON: " + e.Message);
            _parsedLootTables = new Dictionary<string, LootTableDef>();
        }
    }
}
