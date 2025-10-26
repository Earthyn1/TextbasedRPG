using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class EquippableLoader : MonoBehaviour
{
    [SerializeField] private TextAsset equipmentJsonFile;

    // Dictionary for fast lookup by itemID
    private Dictionary<string, EquippableData.EquippableRow> equipmentDatabase;

    private void Awake()
    { 
        LoadEquipment();
    }

    private void Start()
    {
        // If you want to pass the DB to a manager (optional)
        var eqMgr = FindFirstObjectByType<EquipmentManager>();
        if (eqMgr != null)
        {
            eqMgr.SetEquipmentDatabase(equipmentDatabase);
        }
        else
        {
            Debug.LogWarning("[EquippableLoader] EquipmentManager not found in scene.");
        }
    }

    private void LoadEquipment()
    {
        if (equipmentJsonFile == null)
        {
            Debug.LogError("[EquippableLoader] Equipment JSON file not assigned!");
            equipmentDatabase = new Dictionary<string, EquippableData.EquippableRow>();
            return;
        }

        try
        {
            // Ensure enum strings (MainHand/OneHanded) parse correctly
            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Include,
                Converters = new List<JsonConverter> { new StringEnumConverter() }
            };

            var rows = JsonConvert.DeserializeObject<List<EquippableData.EquippableRow>>(equipmentJsonFile.text, settings);
            equipmentDatabase = new Dictionary<string, EquippableData.EquippableRow>();

            if (rows == null || rows.Count == 0)
            {
                Debug.LogWarning("[EquippableLoader] equipment.json parsed but had no rows.");
                return;
            }

            foreach (var row in rows)
            {
                if (row == null || string.IsNullOrEmpty(row.itemID))
                {
                    Debug.LogWarning("[EquippableLoader] Skipping a row with missing itemID.");
                    continue;
                }

                if (!equipmentDatabase.ContainsKey(row.itemID))
                {
                    // Soft validations
                    if (row.attackSpeed < 0f) { row.attackSpeed = 0f; }
                    if (row.levelRequirement < 0) { row.levelRequirement = 0; }

                    equipmentDatabase[row.itemID] = row;
                }
                else
                {
                    Debug.LogWarning($"[EquippableLoader] Duplicate itemID in equipment JSON: {row.itemID}");

                    
                }
            }

            Debug.Log($"[EquippableLoader] Loaded {equipmentDatabase.Count} equippable rows.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[EquippableLoader] Failed to load equipment JSON: " + e.Message);
            equipmentDatabase = new Dictionary<string, EquippableData.EquippableRow>();
        }
    }

    // Public getters (mirrors your ItemLoader style)
    public Dictionary<string, EquippableData.EquippableRow> GetDatabase() => equipmentDatabase;

    public bool TryGet(string itemID, out EquippableData.EquippableRow row)
    {
        row = null;
        return equipmentDatabase != null && equipmentDatabase.TryGetValue(itemID, out row);
    }

    public EquippableData.EquippableRow Get(string itemID)
        => (equipmentDatabase != null && equipmentDatabase.TryGetValue(itemID, out var r)) ? r : null;
}
