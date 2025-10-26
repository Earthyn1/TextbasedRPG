using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class TimedActionLoader : MonoBehaviour
{
    [SerializeField] private TextAsset actionsJsonFile;

    // Newtonsoft settings: string enums, ignore extras, be tolerant of nulls
    static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        Converters = new List<JsonConverter> { new StringEnumConverter() },
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore
    };

    public List<TimedActionDef> LoadActions()
    {
        if (actionsJsonFile == null)
        {
            Debug.LogError("TimedActions JSON file not assigned!");
            return new List<TimedActionDef>();
        }

        try
        {
            var root = JsonConvert.DeserializeObject<TimedActionDefList>(actionsJsonFile.text, Settings);
            var actions = root?.actions ?? new List<TimedActionDef>();

            // Normalize so downstream code is safe
            Normalize(actions);

            // Optional sanity checks (warns but doesn’t fail)
            Validate(actions);

            LogActionsBrief(actions);
            return actions;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load TimedActions JSON: " + e.Message);
            return new List<TimedActionDef>();
        }
    }

    static void Normalize(List<TimedActionDef> actions)
    {
        foreach (var a in actions)
        {
            if (a == null) continue;

            a.milestones ??= new List<TimedActionMilestone>();
            a.endXp ??= new List<XPGrant>();

            // Normalize branch objects so null-safe to read
            if (a.onSuccess == null) a.onSuccess = new Outcome();
            if (a.onFail == null) a.onFail = new Outcome();

            a.onSuccess.giveItems ??= new List<ItemStack>();
            a.onSuccess.takeItems ??= new List<ItemStack>();
            a.onSuccess.setFlags ??= new List<string>();

            a.onFail.giveItems ??= new List<ItemStack>();
            a.onFail.takeItems ??= new List<ItemStack>();
            a.onFail.setFlags ??= new List<string>();

            // Milestone XP lists
            foreach (var m in a.milestones)
                m.xp ??= new List<XPGrant>();

            // If no skillCheck, treat as guaranteed success (your resolver will just use onSuccess)
            if (a.skillCheck != null && a.skillCheck.chance != null)
            {
                // Ensure min/max sane (optional: clamp to [0,1])
                a.skillCheck.chance.Min = Mathf.Clamp01(a.skillCheck.chance.Min);
                a.skillCheck.chance.Max = Mathf.Clamp01(a.skillCheck.chance.Max);
            }
        }
    }

    static void Validate(List<TimedActionDef> actions)
    {
        foreach (var a in actions)
        {
            if (string.IsNullOrEmpty(a.id))
                Debug.LogWarning("TimedAction row has empty id.");

            if (a.skillCheck != null)
            {
                if (a.onSuccess == null)
                    Debug.LogWarning($"[{a.id}] has skillCheck but no onSuccess.");
                if (a.onFail == null)
                    Debug.LogWarning($"[{a.id}] has skillCheck but no onFail.");

                if (a.reportQuestOnComplete && (a.onSuccess?.reportQuest ?? false))
                {
                    // Not an error—just a reminder about precedence
                    Debug.Log($"[{a.id}] both reportQuestOnComplete and onSuccess.reportQuest set; runtime should prefer onSuccess.reportQuest for skill checks.");
                }
            }
        }
    }

    public static void LogActionsBrief(List<TimedActionDef> actions)
    {
        Debug.Log($"Loaded {actions.Count} timed actions.");
        foreach (var a in actions)
        {
            if (a == null) continue;
            var skill = a.skillCheck != null ? a.skillCheck.skill.ToString() : "—";
            Debug.Log($"- {a.id} ({a.displayName}), dur {a.durationMs}ms, milestones {a.milestones.Count}, endXP {a.endXp.Count}, skillCheck {skill}");
        }
    }
}
