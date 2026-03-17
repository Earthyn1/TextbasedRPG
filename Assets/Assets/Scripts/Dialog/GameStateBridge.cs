using Ink.Runtime;
using UnityEngine;

public static class GameStateBridge
{


    public static void Bind(Story story, string npcId = null)
    {

        Debug.Log($"[Bridge] Bound for npcId='{npcId ?? "NULL"}'");
        story.BindExternalFunction("dbg", (string msg) => Debug.Log($"[Ink dbg] {msg}"));
        story.BindExternalFunction("dbgInt", (int n) => Debug.Log($"[Ink dbgInt] {n}"));

        if (story == null) return;

        // ----- FLAGS (global / per-NPC) -----
        story.BindExternalFunction("hasFlag", (string key) =>
        {
            return WorldStateManager.Instance.HasFlag(NamespaceKey(npcId, key));
        });

        story.BindExternalFunction("setFlag", (string key) =>
        {
            string finalKey = NamespaceKey(npcId, key);
            Debug.Log($"[Bridge] setFlag called. npcId={npcId} raw={key} final={finalKey}");
            WorldStateManager.Instance.SetFlag(finalKey);
        });

        story.BindExternalFunction("setWorldFlag", (string key) =>
        {
            Debug.Log($"[Bridge] setWorldFlag called.  final={key}");

            WorldStateManager.Instance.SetFlag(key);
        });
        story.BindExternalFunction("hasWorldFlag", (string key) =>
        {
            return WorldStateManager.Instance.HasFlag(key);
        });

        story.BindExternalFunction("clearWorldFlag", (string key) =>
        {
            WorldStateManager.Instance.ClearFlag(key);
        });

        // ----- QUESTS -----
        story.BindExternalFunction("questActive",
            (string questId) => QuestManager.Instance != null && QuestManager.Instance.IsActive(questId));

        story.BindExternalFunction("questCompleted",
            (string questId) => QuestManager.Instance != null && QuestManager.Instance.IsCompleted(questId));

        story.BindExternalFunction("questEverAccepted",
            (string questId) => QuestManager.Instance != null && QuestManager.Instance.WasEverAccepted(questId));

        story.BindExternalFunction("questEverCompleted",
            (string questId) => QuestManager.Instance != null && QuestManager.Instance.WasEverCompleted(questId));

        story.BindExternalFunction("questEverHandedIn",
            (string questId) => QuestManager.Instance != null && QuestManager.Instance.WasEverHandedIn(questId));

        // Collection quest helpers (based on your Action_ItemGain_ tracking)
        story.BindExternalFunction("questItemCount",
            (string questId, string itemId) => QuestManager.Instance != null ? QuestManager.Instance.GetQuestItemCount(questId, itemId) : 0);

        story.BindExternalFunction("questItemRequired",
            (string questId, string itemId) => QuestManager.Instance != null ? QuestManager.Instance.GetQuestItemRequired(questId, itemId) : 0);

        // ----- INVENTORY -----
        story.BindExternalFunction("itemCount",
            (string itemId) => InventoryManager.Instance != null ? InventoryManager.Instance.CountOf(itemId) : 0);

        story.BindExternalFunction("hasItem",
            (string itemId, int count) => InventoryManager.Instance != null && InventoryManager.Instance.CountOf(itemId) >= count);

        // Optional actions (enable only if you want Ink to trigger gameplay)
        story.BindExternalFunction("giveItem",
            (string itemId, int count) =>
            {
                if (InventoryManager.Instance != null)
                    InventoryManager.Instance.AddItem(itemId, count, silent: true);
            });

        story.BindExternalFunction("takeItem",
            (string itemId, int count) =>
            {
                if (InventoryManager.Instance != null)
                    InventoryManager.Instance.TakeClamped(itemId, count);
            });

        // Hand-in a quest (your RemoveQuest does rewards + removes)
        story.BindExternalFunction("handInQuest",
            (string questId) =>
            {
                if (QuestManager.Instance != null)
                    QuestManager.Instance.RemoveQuest(questId);
            });
    }

    private static string NamespaceKey(string npcId, string key)
    {
        // If npcId is provided, keep flags per-NPC. Otherwise use global flags.
        return string.IsNullOrEmpty(npcId) ? key : $"{npcId}:{key}";
    }
}