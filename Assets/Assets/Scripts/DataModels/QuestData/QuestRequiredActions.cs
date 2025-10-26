[System.Serializable]


public class QuestRequiredActions
{
    public string actionID;
    public string name;

    public int requiredQty;
    public int currentQty;

    public QuestRequiredActions() { }


    public QuestRequiredActions(string actionID, string name, int requiredQty, int currentQty = 0)
    {
        this.actionID = actionID;
        this.name = name;
        this.requiredQty = requiredQty;
        this.currentQty = currentQty;
    }

    public bool IsComplete()
    {
        return currentQty >= requiredQty;
    }

}
