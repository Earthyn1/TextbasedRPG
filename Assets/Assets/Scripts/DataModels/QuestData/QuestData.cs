using System.Collections.Generic;

[System.Serializable]
public class QuestData
{
    public string questId;
    public string questName;
    public string description;
    public string questType;
    public List<QuestRequiredActions> requiredActions;
    public int rewardGold;
    public QuestReward reward;
    public bool isCompleted;

    public QuestData(
    string questId,
    string questName,
    string description,
    string questType,
    List<QuestRequiredActions> requiredActions,
    QuestReward reward,
    bool isCompleted = false
)
    {
        this.questId = questId;
        this.questName = questName;
        this.description = description;
        this.questType = questType;
        this.requiredActions = requiredActions;
        this.reward = reward;
        this.isCompleted = isCompleted;
    }

}
