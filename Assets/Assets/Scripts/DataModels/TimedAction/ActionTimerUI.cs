using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ActionTimerUI : MonoBehaviour
{
    [Header("Authored")]
    public string actionId;
    public Image progressFill;
    public Image TimerIcon;
    public TMP_Text successChance;

    public Sprite defaultTimerSprite;

    [Header("Styling")]
    [Tooltip("Color shown for low success (e.g., 10–30%)")]
    public Color lowColor = new Color(0.85f, 0.25f, 0.25f);     // red-ish
    [Tooltip("Color shown for mid success (around ~60%)")]
    public Color midColor = new Color(0.95f, 0.85f, 0.2f);      // yellow-ish
    [Tooltip("Color shown for high success (e.g., 85–95%)")]
    public Color highColor = new Color(0.25f, 0.9f, 0.35f);     // green-ish

    [Header("Outcome colors")]
    public Color neutralColor = Color.white;
    public Color successColor = new Color(0.25f, 0.9f, 0.35f); // green-ish
    public Color failColor = new Color(0.85f, 0.25f, 0.25f); // red-ish

    [Header("Optional hooks")]
    public UnityEvent onBeginAccepted;
    public UnityEvent onBeginRejected;

    private Button _btn;
    private Coroutine _hideRoutine;


    // soft-gate data
    private List<string> _softGateConds = new();
    private string _lockedMessage = "You need a brush!";

    // in ActionTimerUI
    private void HandleSkillLevelChanged(Enum_Skills skill, int newLevel)
    {
        var def = GameManager.Instance?.GetTimedAction(actionId);
        if (def != null && def.skillCheck != null && def.skillCheck.skill == skill)
            RefreshSuccessUI();
    }



    private void Awake()
    {
        _btn = GetComponent<Button>();
        if (TimerIcon) TimerIcon.gameObject.SetActive(!string.IsNullOrEmpty(actionId));
        if (successChance) successChance.gameObject.SetActive(!string.IsNullOrEmpty(actionId));

        if (progressFill) progressFill.gameObject.SetActive(false);

        // Initial populate of success text/color
        RefreshSuccessUI();
    }

    public void ConfigureSoftGate(List<string> conds, string message)
    {
        _softGateConds = (conds != null && conds.Count > 0) ? new List<string>(conds) : null;
        _lockedMessage = string.IsNullOrEmpty(message) ? "You can’t do that yet." : message;

        // Always leave the button interactable — gating happens at click time
        if (_btn != null)
            _btn.interactable = true;
    }



    private bool SoftGatePasses()
    {
        // RequirementEvaluator.EvaluateAll(null/empty) returns true
        return RequirementEvaluator.EvaluateAll(_softGateConds);
    }



    public void SetActionId(string id)
    {
        actionId = string.IsNullOrEmpty(id) ? "" : id;
        if (TimerIcon) TimerIcon.gameObject.SetActive(!string.IsNullOrEmpty(actionId));
        if (successChance) successChance.gameObject.SetActive(!string.IsNullOrEmpty(actionId));


        var svc = ActionTimerService.Instance;
        bool runningThis = svc != null && svc.IsRunning && IsMyAction(svc.CurrentActionId);
        if (progressFill) progressFill.gameObject.SetActive(runningThis);
        SetProgress(0f);

        var def = GameManager.Instance?.GetTimedAction(actionId);
        ApplySkillIcon(def);

        RefreshSuccessUI();

    }

    public void ClearActionId()
    {
        actionId = "";
        if (TimerIcon) TimerIcon.gameObject.SetActive(false);
        if (successChance) successChance.gameObject.SetActive(false);

        if (progressFill) progressFill.gameObject.SetActive(false);
        SetProgress(0f);
    }

    private void OnEnable()
    {
        _btn.onClick.AddListener(HandleClick);

        if (ActionTimerService.Instance != null)
        {
            var svc = ActionTimerService.Instance;
            svc.OnStarted.AddListener(HandleStarted);
            svc.OnProgress.AddListener(HandleProgress);
            svc.OnCompleted.AddListener(HandleCompleted);
            svc.OnAborted.AddListener(HandleAborted);
            svc.OnResolved.AddListener(HandleResolved);   


            // ⬇️ subscribe to skill level changes
            if (PlayerSkills.Instance != null)
                PlayerSkills.Instance.OnSkillLevelChanged += HandleSkillLevelChanged;

            if (TimerIcon) TimerIcon.gameObject.SetActive(!string.IsNullOrEmpty(actionId));
            if (successChance) successChance.gameObject.SetActive(!string.IsNullOrEmpty(actionId));

            bool runningThis = svc.IsRunning && IsMyAction(svc.CurrentActionId);
            if (progressFill) progressFill.gameObject.SetActive(runningThis);
        }

        SetProgress(0f);
        if (progressFill) progressFill.color = neutralColor; // ✅ baseline

        RefreshSuccessUI();

    }

    private void HandleResolved(string id, bool success)
    {
        if (!IsMyAction(id) || progressFill == null) return;

        // show outcome color immediately
        progressFill.color = success ? successColor : failColor;

        Debug.Log("changedcolor!");

        // If HandleCompleted is going to hide the bar, do it *after* a short delay.
     //   if (_hideRoutine != null) StopCoroutine(_hideRoutine);
     //   _hideRoutine = StartCoroutine(HideProgressSoon());
    }

    private IEnumerator HideProgressSoon()
    {
        // Small real-time delay so the player can notice the color
        yield return new WaitForSecondsRealtime(0.35f);
        if (progressFill != null) progressFill.gameObject.SetActive(false);
    }


    private void OnDisable()
    {
        _btn.onClick.RemoveListener(HandleClick);

        if (ActionTimerService.Instance != null)
        {
            var svc = ActionTimerService.Instance;
            svc.OnStarted.RemoveListener(HandleStarted);
            svc.OnProgress.RemoveListener(HandleProgress);
            svc.OnCompleted.RemoveListener(HandleCompleted);
            svc.OnAborted.RemoveListener(HandleAborted);
            svc.OnResolved.RemoveListener(HandleResolved); // ✅ clean up

        }

        // ⬇️ unsubscribe
        if (PlayerSkills.Instance != null)
            PlayerSkills.Instance.OnSkillLevelChanged -= HandleSkillLevelChanged;
    }


    public void HandleClick()
    {
        if (string.IsNullOrEmpty(actionId))
        {
            onBeginRejected?.Invoke();
            Debug.LogWarning($"[{name}] Tried to start timed action but actionId is empty.");
            return;
        }

        // 🔒 soft-gate
        if (!SoftGatePasses())
        {
            if (!string.IsNullOrEmpty(_lockedMessage))
                GameLog_Manager.Instance?.AddEntry(_lockedMessage);
            onBeginRejected?.Invoke();
            return;
        }

        bool began = ActionTimerService.Instance?.Begin(actionId, _btn) ?? false;
        if (began) onBeginAccepted?.Invoke();
        else onBeginRejected?.Invoke();
    }

    private bool IsMyAction(string id) => !string.IsNullOrEmpty(id) && id == actionId;

    private void HandleStarted(string id)
    {
        if (!IsMyAction(id)) return;

        SetProgress(0f);
        if (TimerIcon) TimerIcon.gameObject.SetActive(true);
        if (successChance) successChance.gameObject.SetActive(true);
        if (progressFill)
        {
            progressFill.gameObject.SetActive(true);
            progressFill.color = neutralColor;  // ✅ reset color each run
        }
        RefreshSuccessUI();
    }


    private void HandleProgress(string id, float t)
    {
        if (IsMyAction(id)) SetProgress(t);
    }

    private void HandleCompleted(string id)
    {
        if (IsMyAction(id))
        {
            SetProgress(1f);
            RefreshSuccessUI();

        }
    }

    private void HandleAborted(string id)
    {
        if (IsMyAction(id))
        {
            SetProgress(0f);
            RefreshSuccessUI();
        }
    }

    private void SetProgress(float t)
    {
        if (progressFill) progressFill.fillAmount = t;
    }


    private void ApplySkillIcon(TimedActionDef def)
    {
        if (TimerIcon == null) return;

        if (def != null && def.skillCheck != null)
        {
            // Get skill icon from CombatManager / UI manager / PlayerSkills
            var skill = def.skillCheck.skill;
            var icon = CombatManager.Instance?.GetSkillSprite(skill);
            if (icon != null)
            {
                TimerIcon.sprite = icon;     // Set skill icon
                TimerIcon.gameObject.SetActive(true);
                return;
            }
        }

        // Fallback if no skill check — default hourglass or hide
        TimerIcon.sprite = defaultTimerSprite;
    }


    private void RefreshSuccessUI()
    {
        if (successChance == null) return;

        // Always show when we have an actionId (to match TimerIcon behavior)
        bool hasAction = !string.IsNullOrEmpty(actionId);
        successChance.gameObject.SetActive(hasAction);
        if (!hasAction) return;

        // Pull def; if missing, show a neutral placeholder
        var def = GameManager.Instance?.GetTimedAction(actionId);
        if (def == null)
        {
            successChance.text = "—";
            successChance.color = midColor;
            return;
        }

        // Guaranteed actions (no skillCheck) => always 100%
        if (def.skillCheck == null)
        {
            successChance.text = "100%";
            successChance.color = highColor;
            return;
        }

        // Skill-gated actions
        var sd = PlayerSkills.Instance?.GetSkill(def.skillCheck.skill);
        int level = sd != null ? Mathf.Max(1, sd.level) : 1;

        var c = def.skillCheck.chance;
        float p = Mathf.Clamp01(c.Base + c.PerLevel * level);
        p = Mathf.Clamp(p, c.Min, c.Max);
        int pct = Mathf.RoundToInt(p * 100f);

        successChance.text = $"{pct}%";
        successChance.color = EvaluateSuccessColor(p);
    }



    private Color EvaluateSuccessColor(float p01)
    {
        // Two-step gradient: red->yellow up to 0.6, then yellow->green to 0.95
        float a = Mathf.InverseLerp(0.10f, 0.60f, p01);
        float b = Mathf.InverseLerp(0.60f, 0.95f, p01);

        // First blend (low to mid)
        Color lowToMid = Color.Lerp(lowColor, midColor, Mathf.Clamp01(a));
        // Second blend (mid to high)
        Color midToHigh = Color.Lerp(midColor, highColor, Mathf.Clamp01(b));

        // Mix them depending on where we are; simple approach: choose one based on threshold
        return (p01 < 0.6f) ? lowToMid : midToHigh;
    }

}
