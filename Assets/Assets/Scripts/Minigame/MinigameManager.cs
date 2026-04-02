using System;
using UnityEngine;

/// <summary>
/// Orchestrates all minigames. Listens for "StartMinigame" on the EventBus,
/// picks the right prefab based on minigame type, scales difficulty by
/// (minigameLevel - playerPerceptionLevel), then fires "MinigameResult" (bool).
/// </summary>
public class MinigameManager : MonoBehaviour
{
    public static MinigameManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject timingBarPrefab;
    [SerializeField] private GameObject memoryGamePrefab;
    [SerializeField] private GameObject mashGamePrefab;
    [SerializeField] private GameObject aetherGridPrefab;
    [SerializeField] private GameObject timedActionPrefab;

    [Header("Spawn Locations")]
    [Tooltip("Assign the SpawnLoc placed over the dialog/interactable panel. " +
             "TimedAction prefabs spawn here so they sit on top of the box. " +
             "Other minigames use the parent passed by the dialog manager via SetSpawnParent().")]
    [SerializeField] private Transform timedActionSpawnParent;

    [Header("Timing Bar — Base Difficulty (at equal levels)")]
    [SerializeField] private float baseSpeed     = 300f;
    [SerializeField] private float baseZoneWidth = 80f;

    [Header("Timing Bar — Scaling per Level Delta")]
    [Tooltip("Arrow speed increase per level the minigame is above the player")]
    [SerializeField] private float speedPerDelta = 20f;
    [Tooltip("Zone width decrease per level the minigame is above the player")]
    [SerializeField] private float zonePerDelta  = 3f;

    [Header("Timing Bar — Clamps")]
    [SerializeField] private float minSpeed      = 80f;
    [SerializeField] private float maxSpeed      = 900f;
    [SerializeField] private float minZoneWidth  = 15f;
    [SerializeField] private float maxZoneWidth  = 200f;

    [Header("Memory Game — Base Difficulty (at equal levels)")]
    [Tooltip("Seconds the items are visible at equal player/minigame level")]
    [SerializeField] private float baseShowDuration     = 2.5f;
    [Tooltip("Seconds subtracted from show duration per level the minigame is above the player")]
    [SerializeField] private float showDurationPerDelta = 0.1f;
    [SerializeField] private float minShowDuration      = 0.6f;
    [SerializeField] private float maxShowDuration      = 6f;

    [Header("Memory Game — Slot Count Scaling")]
    [Tooltip("Number of slots shown at equal player/minigame level")]
    [SerializeField] private int baseItemCount = 4;
    [Tooltip("Extra slots added per level the minigame is above the player")]
    [SerializeField] private int itemsPerDelta = 1;
    [SerializeField] private int minItemCount  = 2;
    [SerializeField] private int maxItemCount  = 10;

    [Header("Mash Game — Base Difficulty (at equal levels)")]
    [Tooltip("% per second the bar drains at equal player/minigame level")]
    [SerializeField] private float baseDrainRate       = 15f;
    [Tooltip("Extra drain % per second per level the minigame is above the player")]
    [SerializeField] private float drainRatePerDelta   = 2f;
    [Tooltip("% of bar filled per click at equal level")]
    [SerializeField] private float baseFillPerClick    = 8f;
    [Tooltip("% reduction in fill-per-click per level the minigame is above the player")]
    [SerializeField] private float fillReductionPerDelta = 0.5f;
    [Tooltip("Starting bar fill % at equal levels")]
    [SerializeField] private float baseStartFill       = 30f;
    [Header("Mash Game — Clamps")]
    [SerializeField] private float maxDrainRate        = 60f;
    [SerializeField] private float minDrainRate        = 5f;
    [SerializeField] private float maxFillPerClick     = 20f;
    [SerializeField] private float minFillPerClick     = 2f;

    [Header("Aether Grid — Base Difficulty (at equal levels)")]
    [SerializeField] private int   baseAetherCols     = 4;
    [SerializeField] private int   baseAetherRows     = 3;
    [SerializeField] private float baseCorruption     = 0.25f;
    [Tooltip("Seconds the player has to complete the path at equal levels")]
    [SerializeField] private float baseAetherTime     = 12f;

    [Header("Aether Grid — Scaling per Level Delta")]
    [Tooltip("Extra columns added per N levels the minigame is above the player")]
    [SerializeField] private int   colsPerDelta       = 2; // divisor: +1 col per N delta
    [Tooltip("Extra corruption % per level above player")]
    [SerializeField] private float corruptionPerDelta = 0.05f;
    [Tooltip("Seconds removed from timer per level above player")]
    [SerializeField] private float timePerDelta       = 1f;

    [Header("Aether Grid — Clamps")]
    [SerializeField] private int   maxAetherCols      = 6;
    [SerializeField] private float maxCorruption      = 0.60f;
    [SerializeField] private float minAetherTime      = 5f;

    [Header("Timed Action — Base Duration (at equal levels)")]
    [Tooltip("Seconds to fill the bar when player skill equals the action level")]
    [SerializeField] private float baseTimedDuration      = 3f;
    [Tooltip("Extra seconds added per level the action is above the player's skill")]
    [SerializeField] private float timedDurationPerDelta  = 0.5f;
    [Header("Timed Action — Clamps")]
    [SerializeField] private float minTimedDuration       = 1f;
    [SerializeField] private float maxTimedDuration       = 12f;

    private Transform _spawnParent;

    // Tracks the active minigame so OnMinigameComplete can award XP
    private MinigameDifficultyEntry _activeEntry;
    private int _activeDelta;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public void SetSpawnParent(Transform parent) => _spawnParent = parent;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()  => EventBus.OnTrigger += OnBusEvent;
    private void OnDisable() => EventBus.OnTrigger -= OnBusEvent;

    private void OnBusEvent(string trigger, object payload)
    {
        if (trigger == "StartMinigame")
            Launch(payload as string ?? "");
    }

    // ── Launch routing ────────────────────────────────────────────────────────

    private void Launch(string rawParams)
    {
        var entry = ParseEntry(rawParams);

        // Each minigame type is gated by a different skill (TimedAction skill is caller-specified)
        Enum_Skills relevantSkill = GetSkillForEntry(entry);
        int playerLevel = GetPlayerSkillLevel(relevantSkill);
        int delta       = entry.level - playerLevel;

        // Remember for XP award on completion
        _activeEntry = entry;
        _activeDelta = delta;

        switch (entry.type)
        {
            case MinigameType.Memory:      LaunchMemory(entry, delta);              break;
            case MinigameType.Mash:        LaunchMash(entry.minigameId, delta);     break;
            case MinigameType.Aether:      LaunchAether(entry.minigameId, delta);   break;
            case MinigameType.TimedAction: LaunchTimedAction(entry, delta);         break;
            case MinigameType.TimingBar:
            default:                       LaunchTimingBar(entry.minigameId, delta); break;
        }
    }

    private static Enum_Skills SkillForType(MinigameType type)
    {
        switch (type)
        {
            case MinigameType.Memory:   return Enum_Skills.Perception;
            case MinigameType.Mash:     return Enum_Skills.Strength;
            case MinigameType.Aether:   return Enum_Skills.Aethur;
            // TimedAction uses a caller-specified skill; Speed is the safe fallback
            case MinigameType.TimedAction:
            case MinigameType.TimingBar:
            default:                    return Enum_Skills.Speed;
        }
    }

    /// <summary>
    /// Like <see cref="SkillForType"/> but honours the per-entry skill override
    /// used by <see cref="MinigameType.TimedAction"/>.
    /// </summary>
    private static Enum_Skills GetSkillForEntry(MinigameDifficultyEntry entry)
    {
        if (entry.type == MinigameType.TimedAction && entry.timedActionSkill.HasValue)
            return entry.timedActionSkill.Value;
        return SkillForType(entry.type);
    }

    private void LaunchTimingBar(string minigameId, int delta)
    {
        if (timingBarPrefab == null)
        {
            Debug.LogError("[MinigameManager] timingBarPrefab is not assigned!");
            EventBus.Fire("MinigameResult", false);
            return;
        }

        float speed     = Mathf.Clamp(baseSpeed     + delta * speedPerDelta, minSpeed,     maxSpeed);
        float zoneWidth = Mathf.Clamp(baseZoneWidth - delta * zonePerDelta,  minZoneWidth, maxZoneWidth);

        Debug.Log($"[MinigameManager] TimingBar '{minigameId}' delta={delta} speed={speed:F0} zone={zoneWidth:F0}");

        var go = Instantiate(timingBarPrefab, _spawnParent);
        var ui = go.GetComponent<TimingBarUI>();
        if (ui == null) { Destroy(go); EventBus.Fire("MinigameResult", false); return; }

        ui.Play(minigameId, speed, zoneWidth, OnMinigameComplete, OnMinigameInstantResult);
    }

    private void LaunchMash(string minigameId, int delta)
    {
        if (mashGamePrefab == null)
        {
            Debug.LogError("[MinigameManager] mashGamePrefab is not assigned!");
            EventBus.Fire("MinigameResult", false);
            return;
        }

        float drain      = Mathf.Clamp(baseDrainRate     + delta * drainRatePerDelta,   minDrainRate,    maxDrainRate);
        float fillClick  = Mathf.Clamp(baseFillPerClick  - delta * fillReductionPerDelta, minFillPerClick, maxFillPerClick);
        float startFill  = Mathf.Clamp(baseStartFill     - delta * 2f,                   5f,              60f);

        Debug.Log($"[MinigameManager] Mash '{minigameId}' delta={delta} drain={drain:F1}%/s fill={fillClick:F1}%/click start={startFill:F0}%");

        var go = Instantiate(mashGamePrefab, _spawnParent);
        var ui = go.GetComponent<MashGameUI>();
        if (ui == null) { Destroy(go); EventBus.Fire("MinigameResult", false); return; }

        ui.Play(drain, fillClick, startFill, OnMinigameComplete, OnMinigameInstantResult);
    }

    private void LaunchAether(string minigameId, int delta)
    {
        if (aetherGridPrefab == null)
        {
            Debug.LogError("[MinigameManager] aetherGridPrefab is not assigned!");
            EventBus.Fire("MinigameResult", false);
            return;
        }

        int   cols    = Mathf.Clamp(baseAetherCols + (delta / colsPerDelta), baseAetherCols, maxAetherCols);
        float corrupt = Mathf.Clamp(baseCorruption  + delta * corruptionPerDelta, 0f,            maxCorruption);
        float time    = Mathf.Clamp(baseAetherTime  - delta * timePerDelta,       minAetherTime, baseAetherTime);

        Debug.Log($"[MinigameManager] Aether '{minigameId}' delta={delta} cols={cols} rows={baseAetherRows} corrupt={corrupt:P0} time={time:F1}s");

        var go = Instantiate(aetherGridPrefab, _spawnParent);
        var ui = go.GetComponent<AetherGridUI>();
        if (ui == null) { Destroy(go); EventBus.Fire("MinigameResult", false); return; }

        ui.Play(cols, baseAetherRows, corrupt, time, OnMinigameComplete, OnMinigameInstantResult);
    }

    private void LaunchMemory(MinigameDifficultyEntry entry, int delta)
    {
        if (memoryGamePrefab == null)
        {
            Debug.LogError("[MinigameManager] memoryGamePrefab is not assigned!");
            EventBus.Fire("MinigameResult", false);
            return;
        }

        float showDuration = Mathf.Clamp(
            baseShowDuration - delta * showDurationPerDelta,
            minShowDuration, maxShowDuration);

        int itemCount = Mathf.Clamp(baseItemCount + delta * itemsPerDelta, minItemCount, maxItemCount);

        Debug.Log($"[MinigameManager] Memory '{entry.minigameId}' delta={delta} slots={itemCount} showDuration={showDuration:F2}s");

        var go = Instantiate(memoryGamePrefab, _spawnParent);
        var ui = go.GetComponent<MemoryGameUI>();
        if (ui == null) { Destroy(go); EventBus.Fire("MinigameResult", false); return; }

        ui.Play(entry.memoryConfig, showDuration, itemCount, OnMinigameComplete, OnMinigameInstantResult);
    }

    private void LaunchTimedAction(MinigameDifficultyEntry entry, int delta)
    {
        if (timedActionPrefab == null)
        {
            Debug.LogError("[MinigameManager] timedActionPrefab is not assigned!");
            EventBus.Fire("MinigameResult", false);
            return;
        }

        float duration = Mathf.Clamp(
            baseTimedDuration + delta * timedDurationPerDelta,
            minTimedDuration, maxTimedDuration);

        string label = !string.IsNullOrWhiteSpace(entry.displayLabel)
            ? entry.displayLabel
            : entry.minigameId;

        Debug.Log($"[MinigameManager] TimedAction '{entry.minigameId}' delta={delta} duration={duration:F1}s label='{label}'");

        // Use the dedicated SpawnLoc if one is assigned, otherwise fall back to whatever
        // the dialog manager passed via SetSpawnParent (e.g. the button layout group).
        timedActionSpawnParent.gameObject.SetActive(true);
        Transform parent = timedActionSpawnParent != null ? timedActionSpawnParent : _spawnParent;

        var go = Instantiate(timedActionPrefab, parent);
        var ui = go.GetComponent<TimedActionUI>();
        if (ui == null) { Destroy(go); EventBus.Fire("MinigameResult", false); return; }

        ui.Play(label, duration, OnMinigameComplete, OnMinigameInstantResult);
    }

    private void OnMinigameInstantResult(bool success)
    {
        // Fires the frame the player clicks — before any result animations
        if (success && _activeEntry != null)
            AwardMinigameXP(_activeEntry, _activeDelta);
    }

    private void OnMinigameComplete(bool success)
    {
        _activeEntry = null;
        EventBus.Fire("MinigameResult", success);
        timedActionSpawnParent.gameObject.SetActive(false);


    }

    /// <summary>
    /// Awards XP for completing a minigame successfully.
    /// Base XP = minigame level × 10, with a bonus for beating harder challenges.
    /// Shows a floating XP toast via XPToastSpawner.
    /// </summary>
    private void AwardMinigameXP(MinigameDifficultyEntry entry, int delta)
    {
        if (PlayerSkills.Instance == null) return;

        Enum_Skills skill = GetSkillForEntry(entry);

        // Base XP scales with minigame level; harder challenges give a bonus
        int baseXP  = entry.level * 10;
        int bonus   = Mathf.Max(0, delta) * 5; // extra per level above player
        int totalXP = baseXP + bonus;

        PlayerSkills.Instance.AddXP(skill, totalXP);

        if (XPToastSpawner.Instance != null)
        {
            string xpText = $"{totalXP}xp";
            Sprite icon   = PlayerSkills.Instance.GetIconForSkill(skill);
            XPToastSpawner.Instance.ShowXPToast(xpText, icon);
        }

        Debug.Log($"[MinigameManager] Awarded {totalXP} {skill} XP (base={baseXP} bonus={bonus})");
    }

    // ── Public difficulty query ───────────────────────────────────────────────

    /// <summary>
    /// Colour-coded difficulty label + skill name for the choice button UI.
    /// e.g. ("Hard", "#FF9800", "Speed")
    /// </summary>
    public (string label, string hexColor, string skillName) GetDifficultyInfo(string minigameId)
    {
        var entry         = ParseEntry(minigameId);
        Enum_Skills skill = GetSkillForEntry(entry);
        int playerLevel   = GetPlayerSkillLevel(skill);
        int delta       = entry.level - playerLevel;

        string skillName = skill.ToString(); // "Speed", "Perception", etc.

        if (delta <= -10) return ("Trivial",   "#4CAF50", skillName);
        if (delta <=  -4) return ("Easy",      "#8BC34A", skillName);
        if (delta <=   3) return ("Fair",      "#FFC107", skillName);
        if (delta <=   9) return ("Hard",      "#FF9800", skillName);
        if (delta <=  14) return ("Very Hard", "#F44336", skillName);
                          return ("Deadly",    "#9C27B0", skillName);
    }

    // ── Public visual helpers (used by DialogChoiceButton) ────────────────────

    /// <summary>
    /// Returns a smoothly interpolated colour based on difficulty delta.
    ///   delta 0        → yellow (equal level)
    ///   delta +5 or more → full red (too hard)
    ///   delta -5 or less → full green (easy)
    /// </summary>
    public Color GetDifficultyColor(string minigameId)
    {
        var entry         = ParseEntry(minigameId);
        Enum_Skills skill = GetSkillForEntry(entry);
        int playerLevel   = GetPlayerSkillLevel(skill);
        int delta       = entry.level - playerLevel;

        Color green  = new Color(0.35f, 0.85f, 0.35f);
        Color yellow = new Color(1f,    0.76f, 0.03f);
        Color red    = new Color(0.85f, 0.3f,  0.3f);

        float t = Mathf.Clamp(delta / 5f, -1f, 1f); // -1 = green, 0 = yellow, +1 = red

        return t < 0
            ? Color.Lerp(yellow, green, -t)
            : Color.Lerp(yellow, red,    t);
    }

    /// <summary>Returns the skill icon sprite for this minigame's linked skill.</summary>
    public Sprite GetSkillSprite(string minigameId)
    {
        var entry         = ParseEntry(minigameId);
        Enum_Skills skill = GetSkillForEntry(entry);
        return PlayerSkills.Instance != null ? PlayerSkills.Instance.GetIconForSkill(skill) : null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses an inline minigame param string from Ink.
    ///
    /// Formats:
    ///   "id, level, TimingBar"
    ///   "id, level, Memory, targetItemId"
    ///   "id, level, TimedAction, SkillName"
    ///   "id, level, TimedAction, SkillName, Display Label"
    ///
    /// Examples:
    ///   "haystackSearch, 5, TimingBar"
    ///   "haystackMemory, 3, Memory, dog_bone"
    ///   "searchArea, 2, TimedAction, Perception"
    ///   "lockpickDoor, 4, TimedAction, Speed, Picking the lock..."
    /// </summary>
    private static MinigameDifficultyEntry ParseEntry(string raw)
    {
        var parts = raw.Split(',');
        for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();

        var entry = new MinigameDifficultyEntry { level = 1, type = MinigameType.TimingBar };

        if (parts.Length >= 1) entry.minigameId = parts[0];

        if (parts.Length >= 2)
        {
            if (!int.TryParse(parts[1], out entry.level))
            {
                // Gracefully handle float strings like "1.5" — round to nearest int
                if (float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float fLevel))
                    entry.level = Mathf.RoundToInt(fLevel);
                else
                    entry.level = 1; // default if completely unparseable
            }
        }

        if (parts.Length >= 3)
        {
            // Allow "Timing Bar" (with space) as well as "TimingBar"
            string typeName = parts[2].Replace(" ", "");
            if (System.Enum.TryParse(typeName, true, out MinigameType t))
                entry.type = t;
        }

        if (parts.Length >= 4)
        {
            if (entry.type == MinigameType.Memory)
            {
                entry.memoryConfig = new MemoryGameConfig { targetItemId = parts[3] };
            }
            else if (entry.type == MinigameType.TimedAction)
            {
                if (System.Enum.TryParse(parts[3], true, out Enum_Skills skill))
                    entry.timedActionSkill = skill;
                else
                    Debug.LogWarning($"[MinigameManager] Unknown skill '{parts[3]}' for TimedAction '{entry.minigameId}' — falling back to Speed.");
            }
        }

        // Parts[4] = optional display label for TimedAction
        if (parts.Length >= 5 && entry.type == MinigameType.TimedAction)
            entry.displayLabel = parts[4];

        return entry;
    }

    private int GetPlayerSkillLevel(Enum_Skills skill)
    {
        if (PlayerSkills.Instance == null) return 1;
        SkillData data = PlayerSkills.Instance.GetSkill(skill);
        return data != null ? data.level : 1;
    }
}

// ── Data types ────────────────────────────────────────────────────────────────

public enum MinigameType { TimingBar, Memory, Mash, Aether, TimedAction }

[Serializable]
public class MinigameDifficultyEntry
{
    public string       minigameId;
    public int          level = 1;
    public MinigameType type  = MinigameType.TimingBar;

    [Tooltip("Only used when type = Memory")]
    public MemoryGameConfig memoryConfig;

    [Tooltip("Only used when type = TimedAction — the skill that scales duration and awards XP")]
    public Enum_Skills? timedActionSkill;

    [Tooltip("Only used when type = TimedAction — text shown on the bar label (optional, falls back to minigameId)")]
    public string displayLabel;
}

[Serializable]
public class MemoryGameConfig
{
    [Tooltip("The item ID the player must locate — distractors are pulled randomly from the item database")]
    public string targetItemId;
}
