using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Populates the right-side detail page of the quest book when a quest slot is clicked.
/// Attach to the right-page root GameObject and wire up the fields in the Inspector.
/// </summary>
public class QuestDetailPanel : MonoBehaviour
{
    public static QuestDetailPanel Instance { get; private set; }

    [Header("Right Page UI")]
    [SerializeField] private TMP_Text questTitle;
    [SerializeField] private TMP_Text questDescription;
    [SerializeField] private TMP_Text rewardText;

    [Header("Objectives")]
    [SerializeField] private Transform objectivesContainer;  // parent layout group for objective rows
    [SerializeField] private GameObject objectiveRowPrefab;  // prefab with a single TMP_Text

    [Header("Empty State")]
    [SerializeField] private GameObject emptyStateRoot;      // optional "select a quest" message

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        // Show empty state when book opens with nothing selected
        ShowEmpty();
    }

    public void Show(QuestData quest)
    {
        if (quest == null) { ShowEmpty(); return; }

        if (emptyStateRoot != null) emptyStateRoot.SetActive(false);

        // Title
        if (questTitle != null)
            questTitle.text = quest.questName;

        // Description
        if (questDescription != null)
            questDescription.text = quest.description;

        // Objectives
        if (objectivesContainer != null && objectiveRowPrefab != null)
        {
            // Clear old rows
            foreach (Transform child in objectivesContainer)
                Destroy(child.gameObject);

            if (quest.requiredActions != null)
            {
                foreach (var action in quest.requiredActions)
                {
                    var row = Instantiate(objectiveRowPrefab, objectivesContainer);
                    var label = row.GetComponentInChildren<TMP_Text>(true);
                    if (label != null)
                    {
                        // Show progress if > 1 required, otherwise just the name
                        label.text = action.requiredQty > 1
                            ? $"{action.name} ({action.currentQty}/{action.requiredQty})"
                            : action.name;

                        // Grey out completed objectives
                        if (action.IsComplete())
                            label.color = new Color(0.5f, 0.5f, 0.5f);
                    }
                }
            }
        }

        // Reward
        if (rewardText != null && quest.reward != null)
        {
            string rewardStr = quest.reward.rewardGold > 0 ? $"{quest.reward.rewardGold}g" : "";

            if (quest.reward.rewardItem != null && !string.IsNullOrEmpty(quest.reward.rewardItem.itemID))
            {
                if (rewardStr.Length > 0) rewardStr += "  ";
                rewardStr += $"{quest.reward.rewardItem.itemID} x{quest.reward.rewardItem.quantity}";
            }

            rewardText.text = rewardStr.Length > 0 ? $"Reward:  {rewardStr}" : "";
        }
    }

    private void ShowEmpty()
    {
        if (emptyStateRoot != null) emptyStateRoot.SetActive(true);
        if (questTitle != null)       questTitle.text = "";
        if (questDescription != null) questDescription.text = "";
        if (rewardText != null)       rewardText.text = "";

        if (objectivesContainer != null)
            foreach (Transform child in objectivesContainer)
                Destroy(child.gameObject);
    }
}
