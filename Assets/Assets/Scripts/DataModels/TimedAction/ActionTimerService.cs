using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ActionTimerService : MonoBehaviour
{
    public static ActionTimerService Instance { get; private set; }

    private FocusModeUI _focus;

    private bool _originHideWhenDone;
    private string _originKey;  // def.id (e.g., "BrushMapleTimed")

    public GameObject xpPrefab;
    public Transform xpSpawnLocation;


    // Public read-only state
    public bool IsRunning => _running;
    public string CurrentActionId => _currentDef?.id;

    private Button _sourceButton;   // <--- add this line


    // Signals (global so UIs can subscribe)
    public UnityEvent<string> OnStarted = new();      // actionId
    public UnityEvent<string, float> OnProgress = new(); // actionId, 0..1
    public UnityEvent<string> OnCompleted = new();    // actionId
    public UnityEvent<string> OnAborted = new();      // actionId
    public UnityEvent<string, bool> OnResolved = new(); // (actionId, success)
    public bool LastOutcomeSuccess { get; private set; }  // <-- fallback property



    private TimedActionDef _currentDef;
    private Coroutine _routine;
    private bool _running;
    private float _elapsed;
    private float _duration;
    private readonly List<bool> _milestoneFired = new();

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _focus = FindFirstObjectByType<FocusModeUI>();
        if (_focus == null)
            Debug.LogWarning("[ActionTimerService] FocusModeUI not found. Focus mode will be disabled.");

    }

    private void OnEnable()
    {
        GameManager.OnZoneWillChange += HandleZoneWillChange;
    }

    private void OnDisable()
    {
        GameManager.OnZoneWillChange -= HandleZoneWillChange;
        if (_running) AbortInternal();
    }

    /// <summary>
    /// Starts an action if none is running. Returns false if busy or not found.
    /// </summary>
    public bool Begin(string actionId, Button source = null)
    {
        if (_running) return false;

        _currentDef = GameManager.Instance?.GetTimedAction(actionId);
        if (_currentDef == null)
        {
            Debug.LogWarning($"ActionTimerService: action '{actionId}' def not found.");
            return false;
        }

        _sourceButton = source;
        _originKey = _currentDef.id;
        _originHideWhenDone = false;

        if (_sourceButton)
        {
            var ab = _sourceButton.GetComponent<Action_Button>();
            if (ab != null)
            {
                _originHideWhenDone = ab.HideOnSuccess;
                // sanity: prefer explicit TimedId when available
                if (!string.IsNullOrEmpty(ab.TimedId))
                    _originKey = ab.TimedId;
            }
        }

        // Turn on focus mode if available
        _focus?.Enter(_sourceButton);

        _running = true;
        _elapsed = 0f;
        _duration = Mathf.Max(0.001f, _currentDef.durationMs / 1000f);

        _milestoneFired.Clear();
        if (_currentDef.milestones != null)
            for (int i = 0; i < _currentDef.milestones.Count; i++)
                _milestoneFired.Add(false);

        if (!string.IsNullOrEmpty(_currentDef.startLog))
        {
            ZoneUIManager.Instance?.SetDescriptionLine(_currentDef.startLog, asNarratorFormat: true, persist: false);
        }


        OnStarted?.Invoke(_currentDef.id);
        _routine = StartCoroutine(Run());
        return true;
    }


    private IEnumerator Run()
    {
        while (_elapsed < _duration)
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            TryFireMilestones(t);
            OnProgress?.Invoke(_currentDef.id, t);
            yield return null;
        }

        _running = false;

        var completedId = _currentDef.id; // capture early

        // ===== Resolve success/fail =====
        bool success = ComputeSuccess(_currentDef);
        Outcome branch = success ? _currentDef.onSuccess : _currentDef.onFail;

        // ✅ Tell UIs the outcome BEFORE any UI is hidden/refreshed
        LastOutcomeSuccess = success;                    // optional fallback for UIs
        OnResolved?.Invoke(completedId, success);

        // ===== Choose one log to show =====
        string logToShow = null;
        if (!string.IsNullOrEmpty(branch?.log)) logToShow = branch.log;
        else if (success && !string.IsNullOrEmpty(_currentDef.completeLog)) logToShow = _currentDef.completeLog;

        if (!string.IsNullOrEmpty(logToShow))
        {
            ZoneUIManager.Instance?.SetDescriptionLine(logToShow, asNarratorFormat: true, persist: true);

            GameManager.Instance?.SetNavigationLocked(true);
            yield return new WaitForSecondsRealtime(1.5f);
            GameManager.Instance?.SetNavigationLocked(false);
        }

        // 🔑 Exit focus next
        _focus?.Exit();

        // Triggers
        if (!string.IsNullOrEmpty(_currentDef.completeEventId))
            FireTrigger(_currentDef.completeEventId, 1f);
        FireTrigger($"action.{completedId}.complete", 1f);

        // XP and outcome effects
        if (_currentDef.endXp != null && _currentDef.endXp.Count > 0)
            GrantXP(_currentDef.endXp);
        ApplyOutcome(_currentDef, branch);

        // Quest
        MaybeReportQuest(_currentDef, success, success ? _currentDef.onSuccess : null);

        // ✅ Defer hiding the button to next frame so listeners stay subscribed this frame
        if (success && _originHideWhenDone && !string.IsNullOrEmpty(_originKey))
        {
            GameManager.Instance.MarkTimedActionDone(_originKey);
            Debug.Log($"[ActionTimerService] Marked timed action done: '{_originKey}'");
            StartCoroutine(DeactivateSourceButtonNextFrame());
        }

        // Give UI a frame to paint the outcome color
        yield return null;

        // Now refresh the zone/UI
        EventBus.Trigger("ui.actions.refresh", _currentDef.id);
        ZoneUIManager.Instance?.RefreshCurrentZone();

        ZoneUIManager.Instance?.SetDescriptionLine("", asNarratorFormat: false, persist: false);

        OnCompleted?.Invoke(completedId);

        _routine = null;
        _currentDef = null;
        _sourceButton = null;
        _originHideWhenDone = false;
        _originKey = null;
    }

    private IEnumerator DeactivateSourceButtonNextFrame()
    {
        yield return null; // wait one frame so OnResolved has delivered
        if (_sourceButton != null)
            _sourceButton.gameObject.SetActive(false);
    }




    private void TryFireMilestones(float normalized)
    {
        if (_currentDef?.milestones == null || _currentDef.milestones.Count == 0) return;

        for (int i = 0; i < _currentDef.milestones.Count; i++)
        {
            if (_milestoneFired[i]) continue;
            var m = _currentDef.milestones[i];

            if (normalized >= m.atPercent)
            {
                _milestoneFired[i] = true;

                if (!string.IsNullOrEmpty(m.log))
                {
                    ZoneUIManager.Instance?.SetDescriptionLine(m.log, asNarratorFormat: true, persist: false);
                }


                if (!string.IsNullOrEmpty(m.eventId))
                    FireTrigger(m.eventId, normalized);

                int pct = Mathf.Clamp(Mathf.RoundToInt(m.atPercent * 100f), 0, 100);
                FireTrigger($"action.{_currentDef.id}.milestone.{pct}", normalized);

                if (m.xp != null && m.xp.Count > 0) GrantXP(m.xp);
            }
        }
    }


    private void HandleZoneWillChange(ZoneData next)
    {
        if (_running && _currentDef != null && _currentDef.cancelOnZoneChange)
            AbortInternal();
    }

    private void AbortInternal()
    {
        if (_routine != null) StopCoroutine(_routine);
        string abortedId = _currentDef?.id;

        _routine = null;
        _running = false;

        OnAborted?.Invoke(abortedId ?? "");

        _focus?.Exit();

        _currentDef = null;
        _sourceButton = null;
    }

    // ---- helpers ----

    private struct TimedActionPayload { public string actionId; public float percent; }

    private void FireTrigger(string triggerId, float percent)
    {
        if (string.IsNullOrEmpty(triggerId)) return;
        var payload = new TimedActionPayload { actionId = _currentDef.id, percent = percent };
        EventBus.Trigger(triggerId, payload);
    }

    private void GrantXP(IEnumerable<XPGrant> grants)
    {
        if (grants == null) return;
        foreach (var g in grants)
        {
            if (g == null || g.amount <= 0) continue;

            PlayerSkills.Instance?.AddXP(g.skill, g.amount); // skill is enum

            var go = Instantiate(xpPrefab, xpSpawnLocation, false);

            var xp = go.GetComponentInChildren<XPNumbers>();
            if (xp != null)
            {
                xp._Text.text = g.amount + " xp";

                var icon = PlayerSkills.Instance?.GetIconForSkill(g.skill);
                if (icon != null)
                {
                    xp._Image.sprite = icon;
                    xp._Image.enabled = true;
                }
                else
                {
                    // no icon available—optional: hide image
                    xp._Image.enabled = false;
                }
            }
            else
            {
                Debug.LogError("XPNumbers component missing on the XPPrefab instance or its children.");
            }
        }
    }

    // ===== Outcome/Skill helpers =====

    private bool ComputeSuccess(TimedActionDef def)
    {
        if (def == null || def.skillCheck == null) return true; // legacy = success

        // Get player level for this skill
        int lvl = 1;
        var sd = PlayerSkills.Instance?.GetSkill(def.skillCheck.skill); // returns SkillData
        if (sd != null) lvl = Mathf.Max(1, sd.level);

        var c = def.skillCheck.chance;
        // NOTE: using capitalized properties (Base/PerLevel/Min/Max) if you mapped with JsonProperty
        float p = Mathf.Clamp01(c.Base + c.PerLevel * lvl);
        p = Mathf.Clamp(p, c.Min, c.Max);

        return Random.value <= p;
    }

    private void ApplyOutcome(TimedActionDef def, Outcome branch)
    {
        if (branch == null) return;

        // Damage (positive = take damage)
        if (branch.damage != 0)
            PlayerStats.Instance?.ApplyDamage(-branch.damage);

        // Items
        if (branch.giveItems != null)
            foreach (var g in branch.giveItems)
                Inventory_Manager.Instance?.AddItem(g.id, g.amount);

        if (branch.takeItems != null)
            foreach (var t in branch.takeItems)
                Inventory_Manager.Instance?.TakeClamped(t.id, t.amount);

        // Flags
        if (branch.setFlags != null)
            foreach (var f in branch.setFlags)
                WorldState.SetFlag(f, true); // <-- swap to your flag system if different
    }

    // Simple quest reporter that respects skill/legacy rules
    private void MaybeReportQuest(TimedActionDef def, bool success, Outcome successBranchUsed)
    {
        if (def == null) return;

        string key = string.IsNullOrWhiteSpace(def.questActionOverride)
            ? def.id
            : def.questActionOverride.Trim();

        if (def.skillCheck != null)
        {
            // Skill-based: only report on success if author opted-in
            if (success && (successBranchUsed?.reportQuest ?? false))
                QuestManager.Instance?.ReportAction(key, 1);
            return;
        }

        // Legacy guaranteed path
        if (def.reportQuestOnComplete)
            QuestManager.Instance?.ReportAction(key, 1);
    }


}
