using System.Collections.Generic;
using TMPro;
using TMPro.EditorUtilities;
using UnityEngine;
using UnityEngine.UI;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    public Dictionary<string, QuestData> activeQuests = new Dictionary<string, QuestData>();
    private Dictionary<string, QuestData> completedQuests = new Dictionary<string, QuestData>();

    // Track which quests have already paid out (prevents double rewards on repeated hand-ins)
    private readonly HashSet<string> _rewardsGranted = new HashSet<string>();

    public RectTransform questsPanel;
    public Quest_Slot slotPrefab;

    public Animator animator;
    public TMP_Text questOption;
    public TMP_Text questName;

    public bool IsActive(string questId) => activeQuests.ContainsKey(questId);
    public bool IsCompleted(string questId) => completedQuests.ContainsKey(questId);
    private readonly HashSet<string> _everAccepted = new HashSet<string>();
    private readonly HashSet<string> _everCompleted = new HashSet<string>();
    private readonly HashSet<string> _everHandedIn = new HashSet<string>();

    public bool WasEverCompleted(string id) => _everCompleted.Contains(id);
    public bool WasEverHandedIn(string id) => _everHandedIn.Contains(id);

    public bool WasEverAccepted(string questId) => _everAccepted.Contains(questId);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (questsPanel != null)
        {
            foreach (Transform child in questsPanel)
                Destroy(child.gameObject);
        }
    }

    private void Start()
    {
        if (Inventory_Manager.Instance != null)
            Inventory_Manager.Instance.OnItemDelta += HandleItemDelta;
    }

    private void HandleItemDelta(string itemId, int delta)
    {
        if (delta <= 0) return; // only count gains for collection quests
        string actionId = $"Action_ItemGain_{itemId}";
        ReportAction(actionId, delta); // will cap at requiredQty and auto-complete
    }
    public void AddQuest(QuestData quest)
    {
        if (!activeQuests.ContainsKey(quest.questId) && !completedQuests.ContainsKey(quest.questId))
        {
            activeQuests.Add(quest.questId, quest);
            _everAccepted.Add(quest.questId);  // <-- record that this quest was taken at least once


            if (questsPanel && slotPrefab)
            {
                Quest_Slot newSlot = Instantiate(slotPrefab, questsPanel);
                newSlot.name = quest.questId;
                newSlot.OnSetup(quest);
            }

            Debug.Log($"Quest started: {quest.questName}");
            GameLog_Manager.Instance.AddEntry($"Quest added: {quest.questName}");

            questOption.text = "NEW QUEST:";
            questName.text = quest.questName;
            animator.SetTrigger("PlayAnim");
        }
        else
        {
            Debug.LogWarning($"Quest {quest.questId} already exists!");
            GameLog_Manager.Instance.AddEntry($"Attempted to add quest {quest.questName}, but it already exists.");
        }
    }

    public void UpdateQuestProgress(string questId, string actionID, int amount = 1)
    {
        if (activeQuests.TryGetValue(questId, out QuestData quest))
        {
            var action = quest.requiredActions.Find(a => a.actionID == actionID);
            if (action != null)
            {
                int previousQty = action.currentQty;
                action.currentQty += amount;
                if (action.currentQty > action.requiredQty) action.currentQty = action.requiredQty;

                // Update the UI slot
                var slot = questsPanel ? questsPanel.Find(questId)?.GetComponent<Quest_Slot>() : null;
                if (slot != null) slot.UpdateProgress();

                GameLog_Manager.Instance.AddEntry(
                    $"Quest updated: {quest.questName} — {action.actionID} {previousQty} → {action.currentQty}"
                );

                // Check if quest is complete (hand-in will grant rewards later)
                if (quest.requiredActions.TrueForAll(a => a.currentQty >= a.requiredQty))
                {
                    CompleteQuest(questId);
                }
            }
        }
    }

    /// <summary>
    /// Marks quest as completed and moves it to completedQuests.
    /// Rewards are NOT granted here (they are granted on hand-in in RemoveQuest).
    /// </summary>
    public void CompleteQuest(string questId)
    {
        if (activeQuests.TryGetValue(questId, out QuestData quest))
        {
            quest.isCompleted = true;

            activeQuests.Remove(questId);
            completedQuests[questId] = quest;

            _everCompleted.Add(questId);   // <-- record completion


            // Update UI slot one last time
            var slot = questsPanel ? questsPanel.Find(questId)?.GetComponent<Quest_Slot>() : null;
            if (slot != null) slot.UpdateProgress();

            Debug.Log($"Quest completed: {quest.questName}");
            GameLog_Manager.Instance.AddEntry($"Quest completed: {quest.questName}");
        }
    }

    /// <summary>
    /// Hand-in: removes quest and grants rewards ONCE if the quest is completed.
    /// </summary>
    public void RemoveQuest(string questId)
    {
        bool removed = false;

        if (completedQuests.TryGetValue(questId, out QuestData questData))
        {
            questOption.text = "QUEST COMPLETED:";
            questName.text = questData.questName;
            animator.SetTrigger("PlayAnim");
        }
        else
        {
            Debug.LogWarning($"Quest ID not found: {questId}");
        }

        // Prefer runtime instances (completed first)
        QuestData quest = null;
        if (!completedQuests.TryGetValue(questId, out quest))
            activeQuests.TryGetValue(questId, out quest);

        // Grant rewards on hand-in (only if completed and not already granted)
        if (quest != null && quest.isCompleted && !_rewardsGranted.Contains(questId))
        {
            GrantQuestRewards(quest);
            _rewardsGranted.Add(questId);
            _everHandedIn.Add(questId);    // <-- record hand-in

        }

        if (activeQuests.Remove(questId)) removed = true;
        if (completedQuests.Remove(questId)) removed = true;

        // Destroy the UI slot if it exists
        if (questsPanel)
        {
            Transform slotTransform = questsPanel.Find(questId);
            if (slotTransform != null)
                Destroy(slotTransform.gameObject);
        }

        if (removed)
        {
            GameLog_Manager.Instance.AddEntry($"Quest removed: {questId}");
        }
        else
        {
            GameLog_Manager.Instance.AddEntry($"Attempted to remove quest {questId}, but it was not found.");
        }
    }

    public QuestData GetActiveQuest(string questId)
    {
        activeQuests.TryGetValue(questId, out QuestData quest);
        return quest;
    }

    /// <summary>
    /// Called by gameplay systems (e.g., timer) when an action completes.
    /// Increments progress for any ACTIVE quest requiring this action.
    /// </summary>
    public void ReportAction(string actionId, int amount = 1)
    {
        Debug.Log($"[Quest] ReportAction('{actionId}', {amount})");
        if (string.IsNullOrWhiteSpace(actionId) || amount <= 0) return;

        // Take a snapshot so we can safely mutate activeQuests during progress/completion
        // (CompleteQuest removes from activeQuests)
        var snapshot = new List<QuestData>(activeQuests.Values);

        foreach (var quest in snapshot)
        {
            if (quest == null || quest.isCompleted) continue;

            // Find a matching required action (case-insensitive)
            var req = quest.requiredActions.Find(a =>
                a != null &&
                !string.IsNullOrEmpty(a.actionID) &&
                string.Equals(a.actionID, actionId, System.StringComparison.OrdinalIgnoreCase));

            if (req == null) continue;

            // Skip if already fulfilled
            if (req.currentQty >= req.requiredQty) continue;

            // Cap amount so we don't overshoot
            int remaining = Mathf.Max(0, req.requiredQty - req.currentQty);
            int delta = Mathf.Min(remaining, Mathf.Max(1, amount));

            // Use the questId from the quest itself; UpdateQuestProgress may complete & move it
            UpdateQuestProgress(quest.questId, req.actionID, delta);
        }
    }


    // --- Rewards ---

    private void GrantQuestRewards(QuestData quest)
    {
        // Gold
        if (quest.reward.rewardGold > 0)
        {
            Inventory_Manager.Instance.AddItem("gold_coin", quest.reward.rewardGold);
            GameLog_Manager.Instance.AddEntry($"+{quest.reward.rewardGold} gold for completing {quest.questId}");
        }

        // Item
        if (quest.reward.rewardItem != null && !string.IsNullOrEmpty(quest.reward.rewardItem.itemID))
        {
            Inventory_Manager.Instance.AddItem(quest.reward.rewardItem.itemID, quest.reward.rewardItem.quantity);
            // Optionally log the item reward here
            // GameLog_Manager.Instance.AddEntry($"Received item: {quest.reward.rewardItem.itemID} x{quest.reward.rewardItem.quantity}");
        }
    }
}
