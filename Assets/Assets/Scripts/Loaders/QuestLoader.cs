using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class QuestLoader : MonoBehaviour
{
    [SerializeField] private TextAsset questsJsonFile;

    public List<QuestData> LoadQuests()
    {
        if (questsJsonFile == null)
        {
            Debug.LogError("Quests JSON file not assigned!");
            return null;
        }

        try
        {
            var quests = JsonConvert.DeserializeObject<List<QuestData>>(questsJsonFile.text);
            Debug.Log($"Loaded {quests.Count} quests.");

            // 🔹 Debug each quest
          //  PrintQuestDetails(quests);

            return quests;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load quests JSON: " + e.Message);
            return null;
        }
    }

    private void PrintQuestDetails(List<QuestData> quests)
    {
        foreach (var quest in quests)
        {
            string actions = "";
            if (quest.requiredActions != null)
            {
                foreach (var action in quest.requiredActions)
                {
                    actions += $"[{action.actionID}: {action.currentQty}/{action.requiredQty}] ";
                }
            }

            string reward = "No reward";
            if (quest.reward != null)
            {
                reward = quest.reward.rewardGold + " Gold";
                if (quest.reward.rewardItem != null)
                {
                    reward += $", Item: {quest.reward.rewardItem.itemID} x{quest.reward.rewardItem.quantity}";
                }
            }

            Debug.Log(
                $"Quest: {quest.questName}\n" +
                $"Description: {quest.description}\n" +
                $"Type: {quest.questType}\n" +
                $"Required Actions: {actions}\n" +
                $"Reward: {reward}\n" +
                $"Completed: {quest.isCompleted}\n"
            );
        }
    }
}
