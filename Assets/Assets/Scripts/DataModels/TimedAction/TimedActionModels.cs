// Scripts/TimedActions/TimedActionModels.cs
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class XPGrant
{
    public Enum_Skills skill;
    public int amount;
}

[Serializable]
public class TimedActionMilestone
{
    [Range(0f, 1f)] public float atPercent; // 0..1 progress
    public string log;
    public string eventId;
    public List<XPGrant> xp = new();
}

// ---------- NEW Supporting Models ----------
[Serializable]
public class ItemStack
{
    public string id;
    public int amount;
}

[Serializable]
public class Outcome
{
    public string log;                 // branch text
    public List<ItemStack> giveItems;  // items granted on this branch
    public List<ItemStack> takeItems;  // items removed on this branch
    public List<string> setFlags;      // flags set on this branch
    public int damage;                 // positive = take damage; 0 omitted
    public bool reportQuest;           // success-branch only (emit quest progress)
}


[Serializable]
public class SkillChance
{
    [JsonProperty("base")] public float Base;     // map JSON "base" -> C# "Base"
    [JsonProperty("perLevel")] public float PerLevel;
    [JsonProperty("min")] public float Min;
    [JsonProperty("max")] public float Max;
}


[Serializable]
public class SkillCheck
{
    public Enum_Skills skill;  // e.g., Precision, Strength
    public SkillChance chance; // linear curve params
}
// ------------------------------------------------

[Serializable]
public class TimedActionDef
{
    public string id;
    public string displayName;
    public int durationMs = 1000;

    // Keep for safety; you can set false per action if you later block zone moves during timers.
    public bool cancelOnZoneChange = true;

    public string startLog;
    public string completeLog;
    public string completeEventId;

    public bool reportQuestOnComplete = false; // legacy path (always report on finish)
    public string questActionOverride;         // e.g. "Action_Stable_GroomMaple"

    public List<TimedActionMilestone> milestones = new();
    public List<XPGrant> endXp = new();

    // ---------- NEW Fields (optional) ----------
    public SkillCheck skillCheck;  // presence => use success/fail resolution
    public Outcome onSuccess;      // applied when success (or always if no skillCheck)
    public Outcome onFail;         // applied when fail (ignored if no skillCheck)
}

[Serializable]
public class TimedActionDefList
{
    public List<TimedActionDef> actions = new();
}
