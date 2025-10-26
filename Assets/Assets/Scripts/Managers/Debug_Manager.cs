using UnityEngine;

public class Debug_Manager : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Inventory_Manager.Instance.AddItem("iron_sword", 1);
            Inventory_Manager.Instance.AddItem("potion_health", 1);
            Inventory_Manager.Instance.AddItem("bread_loaf", 1);
            Inventory_Manager.Instance.AddItem("meat_cooked", 1);
            Inventory_Manager.Instance.AddItem("leather_pants", 1);
            Inventory_Manager.Instance.AddItem("leather_boots", 1);
            Inventory_Manager.Instance.AddItem("iron_longsword", 1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlayerSkills.Instance.AddXP(Enum_Skills.Fortitude, 100);
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
            QuestData goblinQuest = GameManager.Instance.GetQuestById("GroomMaple");

            if (goblinQuest != null)
            {
                QuestManager.Instance.AddQuest(goblinQuest);
            }
            else
            {
                Debug.LogWarning("Goblin Slayer quest not found!");
            }
        }

        // ✅ Update Goblin Slayer progress by 1
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            QuestManager.Instance.UpdateQuestProgress("GroomMaple", "Action_Stable_GroomMaple", 1);
           
        }
    
}
}
