using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogOption
{
    public int ord;                 // optional but nice to keep ordering
    public string optionText;
    public string type;             // "Dialog" | "Zone" | "Quest" | ...
    public string nextDialogId;     // for Dialog/Zone/Quest follow-up
    public string questId;          // <-- needed for type=Quest
    public string requirement;      // e.g., "QuestInProgress:GroomMaple"
}


[System.Serializable]
public class ItemGrant
{
    public string id;
    public int amount;
}

[System.Serializable]
public class DialogNode
{
    public string dialogId;
    public string npcName;
    public string npcId;
    public string npcText;
    public string npcPortrait;
    public System.Collections.Generic.List<ItemGrant> onEnterGiveItems;
    public System.Collections.Generic.List<ItemGrant> onEnterTakeItems;
    public List<DialogOption> playerOptions;
}
