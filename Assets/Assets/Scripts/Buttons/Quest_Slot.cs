using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class Quest_Slot : MonoBehaviour
{
    public QuestData QuestData;
    public TMP_Text title;
    public TMP_Text description;
    public TMP_Text QuestObjective;
    public TMP_Text Qty;

    public Image background; // optional: change color on completion

    // Setup slot with a quest
    public void OnSetup(QuestData questData)
    {
        QuestData = questData;
        title.text = questData.questName;
        description.text = questData.description;
      

        UpdateProgress();
    }

    // Call this to refresh the progress UI
    public void UpdateProgress()
    {
        if (QuestData.requiredActions != null && QuestData.requiredActions.Count > 0)
        {
            QuestObjective.text = QuestData.requiredActions[0].name;
            // Show all actions in the format: ActionID: current/required
            Qty.text = string.Join("\n", QuestData.requiredActions
                .Select(a => $"{a.currentQty}/{a.requiredQty}"));
        }
        else
        {
            Qty.text = "";
        }

        // Change color if quest is completed
        if (QuestData.isCompleted)
        {
            title.color = Color.green;
            if (background != null) background.color = new Color(0.8f, 1f, 0.8f); // light green
        }
    }

    public void OnClicked()
    {
        // optional: show quest details in a panel
    }
}
