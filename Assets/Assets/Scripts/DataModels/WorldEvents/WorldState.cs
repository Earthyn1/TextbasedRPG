using System.Collections.Generic;

public static class WorldState
{
    public static string CurrentZoneId;

    private static HashSet<string> _visitedZones = new();
    private static Dictionary<string, bool> _flags = new();
    private static Dictionary<string, int> _questStages = new();

    // Zones
    public static bool HasVisited(string zoneId) => _visitedZones.Contains(zoneId);
    public static void MarkVisited(string zoneId) => _visitedZones.Add(zoneId);

    // Flags
    public static bool GetFlag(string key) => _flags.ContainsKey(key) && _flags[key];
    public static void SetFlag(string key, bool value) => _flags[key] = value;

    // Quests
    public static int GetQuestStage(string questId) =>
        _questStages.ContainsKey(questId) ? _questStages[questId] : 0;

    public static bool HasFlag(string key) => _flags.ContainsKey(key);


    public static void SetQuestStage(string questId, int stage) =>
        _questStages[questId] = stage;
}
