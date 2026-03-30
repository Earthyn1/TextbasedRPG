using UnityEngine;

public class Debug_Manager : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            InventoryManager.Instance.AddItem("iron_sword", 1);
            InventoryManager.Instance.AddItem("potion_health", 1);
            InventoryManager.Instance.AddItem("bread_loaf", 1);
            InventoryManager.Instance.AddItem("meat_cooked", 1);
            InventoryManager.Instance.AddItem("leather_pants", 1);
            InventoryManager.Instance.AddItem("leather_boots", 1);
            InventoryManager.Instance.AddItem("chicken_egg", 5);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlayerSkills.Instance.AddXP(Enum_Skills.Aethur, 100);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
           // PlayerSkills.Instance.AddXP(Enum_Skills.Agility, 31);
           GameManager.Instance.clearMiddlePanels();
        }

        // ✅ Add Goblin Slayer quest
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            // Grab the quest from your GameManager by ID
            QuestData goblinQuest = GameManager.Instance.GetQuestById("BanditEscort");

            if (goblinQuest != null)
            {
                QuestManager.Instance.AddQuest(goblinQuest);
            }
            else
            {
                Debug.LogWarning("BanditEscort quest not found!");
            }
        }

        // ✅ Update Goblin Slayer progress by 1
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            QuestManager.Instance.UpdateQuestProgress("GroomMaple", "Action_Stable_GroomMaple", 1);
           
        }
    
}
}
