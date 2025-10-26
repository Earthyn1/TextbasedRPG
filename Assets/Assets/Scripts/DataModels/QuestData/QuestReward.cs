using System;

[System.Serializable]
public class QuestReward
{
    public int rewardGold = 0;
    public RewardItem rewardItem = null;

    [Serializable]
    public class RewardItem
    {
        public string itemID;
        public int quantity = 1;

        public RewardItem() { }

        public RewardItem(string itemID, int quantity)
        {
            this.itemID = itemID;
            this.quantity = quantity;
        }
    }

    public QuestReward() { }

    public QuestReward(int rewardGold, RewardItem rewardItem = null)
    {
        this.rewardGold = rewardGold;
        this.rewardItem = rewardItem;
    }
}
