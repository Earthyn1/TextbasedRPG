using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Action_Button : MonoBehaviour
{
    public TMP_Text text;

    public string nextAction;          // for Zone/Dialog/etc.
    private string _questId;
    public string actionName;
    public ActionType actionType;
    public string actionRequirement;   // used for dialog hand-in
    public string examineText;


    [SerializeField] private Button _btn;

    public bool HideOnSuccess { get; private set; }   // authored from ActionData.hideWhenDone
    public string TimedId { get; private set; }    // the TimedActionDef id (your ActionData.zone)

    // Add fields to cache gating for this button instance
    private List<string> _enableWhenCached;
    private string _lockedMessageCached;
    private  string actionId;


    private void Awake()
    {
        if (_btn == null) _btn = GetComponent<Button>();
    }

    // ------------ ZONE/ENTITY ACTION BUTTONS ------------
    public void SetupButton(ActionData action)
    {
        if (_btn == null) _btn = GetComponent<Button>();

        // cache for ButtonPressed hard gate (belt & suspenders)
        _enableWhenCached = action.enableWhen;   // can be null
        _lockedMessageCached = string.IsNullOrWhiteSpace(action.lockedMessage)
            ? "You can’t do that yet."
            : action.lockedMessage;

        // label/fields
        text.text = action.name;
        actionType = action.type;
        nextAction = action.zone;
        actionName = action.name;
        HideOnSuccess = action.hideOnSuccess;
        TimedId = (action.type == ActionType.Timed) ? action.zone : null;

        // 1) visibleWhen → hide entirely if false
        if (!RequirementEvaluator.EvaluateAll(action.visibleWhen))
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);

        var timerUI = GetComponent<ActionTimerUI>();

        // Always clear prior listeners
        _btn.onClick = new Button.ButtonClickedEvent();

        if (action.type == ActionType.Timed)
        {
            // Timed: let ActionTimerUI own the click; soft-gate evaluated on each click
            timerUI?.SetActionId(action.zone);
            timerUI?.ConfigureSoftGate(action.enableWhen, action.lockedMessage);

            // rebind because we just cleared listeners
            if (timerUI != null) _btn.onClick.AddListener(timerUI.HandleClick);
            else Debug.LogWarning($"[{name}] Timed action without ActionTimerUI component.");
        }
        else
        {
            timerUI?.ClearActionId();

            // Non-timed: re-evaluate enableWhen on each click (soft-gate)
            _btn.onClick.AddListener(() =>
            {
                if (!RequirementEvaluator.EvaluateAll(action.enableWhen))
                {
                    GameLog_Manager.Instance.AddEntry(_lockedMessageCached ?? "You can’t do that yet.");
                    Debug.Log($"[DEBUG] Locked Attempt Fired: '{("LockedAttempt_" + actionName)}' (len={("LockedAttempt_" + actionName).Length})");
                    EventBus.Fire($"LockedAttempt_{actionName.Trim()}");


                    return;
                }

                ButtonPressed(); // action allowed
            });
        }
    }







    // ------------ DIALOG OPTION BUTTONS ------------
    private static ActionType ParseActionType(string s) =>
        Enum.TryParse<ActionType>(s, true, out var val) ? val : ActionType.Dialog;

    public void SetupDialogButton(DialogOption option)
    {
        if (_btn == null) _btn = GetComponent<Button>();

        actionType = ParseActionType(option.type);
        nextAction = option.nextDialogId ?? string.Empty;
        _questId = option.questId ?? string.Empty;
        actionRequirement = option.requirement;

        var label = option.optionText ?? string.Empty;
        if (label.Length >= 2 && label.StartsWith("*") && label.EndsWith("*"))
        {
            var inner = label.Substring(1, label.Length - 2).Trim();
            text.text = $"<i>{inner}</i>";
        }
        else
        {
            text.text = label;
        }

        _btn.onClick = new Button.ButtonClickedEvent();
        _btn.onClick.AddListener(ButtonPressed);
    }

    // ------------ MAIN CLICK HANDLER (non-timed) ------------
    public void ButtonPressed()
    {
        // ✅ HARD GATE: re-check enableWhen at click time
        if (!RequirementEvaluator.EvaluateAll(_enableWhenCached))
        {
            GameLog_Manager.Instance.AddEntry(_lockedMessageCached ?? "You can’t do that yet.");
            return;
        }
        var ui = ZoneUIManager.Instance;
        bool isNonWorldClick =
           actionType == ActionType.Dialog ||
           actionType == ActionType.Quest;

        if (ui != null && !ui.IsInDialogMode && !ui.IsBlockingNarration && !isNonWorldClick)
        {
            if (ui.TryConsumeFirstClickTrigger(out var zId) && !string.IsNullOrEmpty(zId))
            {
                EventBus.Fire("FirstClickInZone", zId);

                // ✅ NOW clear the flag after the event had a chance to read it
                WorldState.SetFlag($"FirstClickArmed_{zId}", false);

                // If an event took over immediately, abort the original click
                if (ui.HasPendingTakeover || ui.IsBlockingNarration || ui.IsInDialogMode)
                    return;
            }
        }


        switch (actionType)
        {
            case ActionType.Zone:
                {
                    var nextZone = GameManager.Instance.GetZoneByID(nextAction);
                    if (nextZone != null)
                    {
                       
                        GameManager.Instance.GoToZone(nextAction);
                        GameLog_Manager.Instance.AddEntry("You head to the " + nextZone.displayName);
                    }
                    else Debug.LogWarning($"Zone '{nextAction}' not found!");
                    break;
                }

            case ActionType.Dialog:
                {
                    var nextDialog = GameManager.Instance.GetDialogById(nextAction);
                    if (nextDialog != null)
                        GameManager.Instance.zoneUIManager.DisplayDialogNode(nextDialog);
                    else
                        Debug.LogWarning($"Dialog '{nextAction}' not found!");
                    break;
                }

            case ActionType.Quest:
                {
                    if (string.IsNullOrWhiteSpace(_questId))
                    {
                        Debug.LogWarning("[Dialog] Quest option clicked but questId is empty.");
                    }
                    else
                    {
                        var newQuest = GameManager.Instance.GetQuestById(_questId);
                        if (newQuest == null)
                        {
                            Debug.LogWarning($"[Dialog] Quest '{_questId}' not found.");
                        }
                        else if (QuestManager.Instance.GetActiveQuest(_questId) == null && !newQuest.isCompleted)
                        {
                            QuestManager.Instance.AddQuest(newQuest);
                            GameLog_Manager.Instance.AddEntry($"Quest accepted: {newQuest.questName}");
                            GameManager.Instance.zoneUIManager.RefreshCurrentDialog(); // hide accept line immediately
                        }
                    }

                    var nextDialog_Q = GameManager.Instance.GetDialogById(nextAction);
                    if (nextDialog_Q != null)
                        GameManager.Instance.zoneUIManager.DisplayDialogNode(nextDialog_Q);
                    else
                        Debug.LogWarning($"Dialog '{nextAction}' not found!");
                    break;
                }

            // inside your Action/Interactable handler (MonoBehaviour context)
            // wherever your ActionType switch lives (e.g., in the Interactable button handler)
            case ActionType.Examine:
                {
                  //var ui = ZoneUIManager.Instance;
                    if (ui == null || string.IsNullOrWhiteSpace(examineText)) break;

                    // ✅ Do what zone transitions do: hide actions & interactables first
                    ui.PrepareZoneTransition();

                    // ✅ Then run the same blocking narrator flow
                    ui.StartCoroutine(ui.ShowNarrationAndWait(examineText));
                    break;
                }



            case ActionType.Read:
                Book_Manager.Instance.PopulateBookList(nextAction);
                break;

            case ActionType.Combat:
                NPCData_Manager.Instance.SetupCombatUI(nextAction);
                break;

            case ActionType.Timed:
                // Timed actions are wired in SetupButton via ActionTimerUI
                return;
        }

        // --- Hand-in cleanup: works with "QuestCompleted:GroomMaple" or "QuestCompleted(GroomMaple)" ---
        if (TryParseRequirement(actionRequirement, out var reqState, out var qid) &&
            !string.IsNullOrWhiteSpace(qid) &&
            reqState.Equals("QuestCompleted", StringComparison.OrdinalIgnoreCase))
        {
            QuestManager.Instance.RemoveQuest(qid);
            GameLog_Manager.Instance.AddEntry($"Quest handed in: {qid}");
            GameManager.Instance.zoneUIManager.RefreshCurrentDialog();
        }
    }


    // ------------ Helper: parse "State:Id" OR "State(Id)" ------------
    private static bool TryParseRequirement(string requirement, out string state, out string questId)
    {
        state = questId = null;
        if (string.IsNullOrWhiteSpace(requirement)) return false;
        var s = requirement.Trim();

        // function style: State(Id)
        if (s.EndsWith(")"))
        {
            int open = s.IndexOf('(');
            if (open > 0)
            {
                state = s.Substring(0, open).Trim();
                questId = s.Substring(open + 1, s.Length - open - 2).Trim();
                return state.Length > 0 && questId.Length > 0;
            }
        }

        // colon style: State:Id or Id:State
        int colon = s.IndexOf(':');
        if (colon > 0 && colon < s.Length - 1)
        {
            string a = s.Substring(0, colon).Trim();
            string b = s.Substring(colon + 1).Trim();

            bool IsState(string x) =>
                x.Equals("QuestNotStarted", StringComparison.OrdinalIgnoreCase) ||
                x.Equals("QuestInProgress", StringComparison.OrdinalIgnoreCase) ||
                x.Equals("QuestCompleted", StringComparison.OrdinalIgnoreCase) ||
                x.Equals("QuestEverCompleted", StringComparison.OrdinalIgnoreCase) ||
                x.Equals("QuestHandedIn", StringComparison.OrdinalIgnoreCase);

            if (IsState(a)) { state = a; questId = b; return true; }
            if (IsState(b)) { state = b; questId = a; return true; }
        }
        return false;
    }
}
