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
        if (title != null) title.text = questData.questName;
        if (description != null) description.text = questData.description;

        // Wire up click automatically
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClicked);
        }

        UpdateProgress();
    }

    // Call this to refresh the progress UI
    public void UpdateProgress()
    {
        if (QuestData.requiredActions != null && QuestData.requiredActions.Count > 0)
        {
            if (QuestObjective != null) QuestObjective.text = QuestData.requiredActions[0].name;
            if (Qty != null) Qty.text = string.Join("\n", QuestData.requiredActions
                .Select(a => $"{a.currentQty}/{a.requiredQty}"));
        }
        else
        {
            if (Qty != null) Qty.text = "";
        }

        // Change color if quest is completed
        if (QuestData.isCompleted)
        {
            if (title != null) title.color = Color.green;
            if (background != null) background.color = new Color(0.8f, 1f, 0.8f);
        }
    }

    public void OnClicked()
    {
        QuestDetailPanel.Instance?.Show(QuestData);
    }
}
