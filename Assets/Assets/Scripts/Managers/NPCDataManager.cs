using System.Collections.Generic;
using UnityEngine;

public class NPCData_Manager : MonoBehaviour
{
    public static NPCData_Manager Instance { get; private set; }

    public RectTransform CombatUIBox;
    public CombatUI combatUI;


    // Lookup dictionary for enemies
    private Dictionary<string, NPCData> allNPCS = new Dictionary<string, NPCData>();

    // --- Loot tables ---
    private Dictionary<string, LootTableDef> allLootTables = new Dictionary<string, LootTableDef>();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // optional, keep across scenes
    }


    // ====== COMBAT START HOOK ======
    public void SetupCombatUI(string enemy)
    {
        GameManager.Instance.clearMiddlePanels();
        CombatUIBox.gameObject.SetActive(true);
        combatUI.InitialSetup(enemy);
        CombatManager.Instance.endCombatUIAnimation.SetTrigger("InitialFightStart");
        CombatManager.Instance.StartEncounter(enemy);

    }


    // ====== NPC REGISTRATION ======
    public void SetAllNPCS(List<NPCData> Npcs)
    {
        allNPCS.Clear();
        foreach (var Npc in Npcs)
        {
           // Debug.Log(Npc.npcID);
            if (!allNPCS.ContainsKey(Npc.npcID))
                allNPCS[Npc.npcID] = Npc;
            
            else
                Debug.LogWarning($"Duplicate EnemyID found: {Npc.npcID}");
        }

        Debug.Log($"Enemy_Manager initialized with {allNPCS.Count} enemies.");
    }


    public NPCData GetNPCS(string enemyid)
    {
        if (allNPCS.TryGetValue(enemyid, out var enemy))
            return enemy;

        Debug.LogError($"NPCID not found: {enemyid}");
        return null;
    }

    public IEnumerable<NPCData> GetAllNPCS()
    {
        return allNPCS.Values;
    }

    // ====== LOOT TABLE REGISTRATION ======
    public void SetAllLootTables(Dictionary<string, LootTableDef> lootDict)
    {
        allLootTables = lootDict ?? new Dictionary<string, LootTableDef>();
        Debug.Log($"NPCData_Manager stored {allLootTables.Count} loot tables.");
    }

    public LootTableDef GetLootTable(string lootID)
    {
        if (string.IsNullOrEmpty(lootID))
            return null;

        if (allLootTables.TryGetValue(lootID, out var table))
            return table;

        Debug.LogWarning($"Loot table not found: {lootID}");
        return null;
    }
}

