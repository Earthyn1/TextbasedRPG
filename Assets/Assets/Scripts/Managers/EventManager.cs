using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;



public class EventManager : MonoBehaviour
{
    public static EventManager I { get; private set; }

    private List<GameEventDef> _events = new();
    private HashSet<string> _firedOnce = new();

    private const string TRIG_FIRST_CLICK = "FirstClickInZone";

    private static bool NameEq(string a, string b)
    => string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);


    void Awake()
    {
        I = this;
        EventBus.OnTrigger += HandleTrigger;
    }

    public void LoadEvents(IEnumerable<GameEventDef> defs)
    {
        _events = defs.OrderByDescending(e => e.priority).ToList();
    }


    private void HandleTrigger(string trigger, object payload)
    {
        Debug.Log($"[EVENT DEBUG] Received Trigger: {trigger} (zone={WorldState.CurrentZoneId})");

        string zone = WorldState.CurrentZoneId;
        if (NameEq(trigger, TRIG_FIRST_CLICK) && payload is string pz && !string.IsNullOrEmpty(pz))
            zone = pz;

        for (int i = 0; i < _events.Count; i++)
        {
            var e = _events[i];

            // Trigger match (case-insensitive) without LINQ alloc
            bool trigMatch = false;
            var ts = e.triggers;
            for (int t = 0; t < ts.Count; t++)
            {
                if (NameEq(ts[t], trigger)) { trigMatch = true; break; }
            }
            if (!trigMatch) continue;

            // Scope / zone match
            if (e.scope == "zone" && e.zoneId != zone) continue;

            // Once-only guard
            if (e.once && _firedOnce.Contains(e.id)) continue;

            // Conditions
            if (!CheckConditions(e.conditions)) continue;

            // Run
            if (e.blocking) StartCoroutine(ExecuteActionsCo(e.actions));
            else ExecuteActions(e.actions);

            if (e.once) _firedOnce.Add(e.id);

            if (e.blocking) break; // preserve early-exit on blocking events
        }
    }




    private bool CheckConditions(ConditionBlock c)
    {
        if (c == null) return true;
        // treat null lists as empty
        var all = (c.allOf ?? new List<string>()).All(ParseCond);
        var anyList = (c.anyOf ?? new List<string>());
        var any = anyList.Count == 0 || anyList.Any(ParseCond);
        var none = (c.noneOf ?? new List<string>()).All(x => !ParseCond(x));
        return all && any && none;
    }


    private bool ParseCond(string s)
    {
        s = s.Trim();

        if (s.StartsWith("Flag("))
            return WorldState.GetFlag(Between(s, "Flag(", ")"));

        if (s.StartsWith("ZoneIs("))
            return WorldState.CurrentZoneId == Between(s, "ZoneIs(", ")");

        if (s.StartsWith("FirstTimeInZone("))
            return !WorldState.HasVisited(Between(s, "FirstTimeInZone(", ")"));

        if (s.StartsWith("QuestStage("))
        {
            var q = Between(s, "QuestStage(", ")");
            if (s.Contains("==")) return WorldState.GetQuestStage(q) == IntAfter(s, "==");
            if (s.Contains(">=")) return WorldState.GetQuestStage(q) >= IntAfter(s, ">=");
        }

        return false;
    }

    // --- Action runners ---

    // Fast path: run a single action (non-blocking). Returns a pending zone jump if any.
    private string RunActionImmediate(string a)
    {
        a = a.Trim();

        if (a.StartsWith("Narrate("))
        {
            string msg = Between(a, "Narrate(", ")");
            ZoneUIManager.Instance?.ShowNarration(msg, blocking: false);
            return null;
        }
        if (a.StartsWith("SetFlag("))
        {
            var body = Between(a, "SetFlag(", ")").Split(',');
            var key = body[0].Trim();
            var val = bool.Parse(body[1].Trim());
            WorldState.SetFlag(key, val);
            ZoneUIManager.Instance?.RefreshCurrentZone();
            return null;
        }
        if (a.StartsWith("StartDialog("))
        {
            string nodeId = Between(a, "StartDialog(", ")").Trim();
            ZoneUIManager.Instance?.FlagPendingTakeover();
            ZoneUIManager.Instance?.BeginEventTakeoverFade(); // optional
            StartDialogById(nodeId); // (deferred by a few frames as you have)
            Debug.Log("[FirstClick] Takeover flagged (StartDialog)");
            return null;
        }
        if (a.StartsWith("GotoZone(") || a.StartsWith("Zone("))
        {
            string target = Between(a, a.StartsWith("GotoZone(") ? "GotoZone(" : "Zone(", ")").Trim();
            return target; // defer actual nav so multiple actions can queue, last wins
        }
        if (a.StartsWith("GiveGold("))
        {
            int amount = int.Parse(Between(a, "GiveGold(", ")"));
            Inventory_Manager.Instance?.AddItem("gold_coin", amount);
            GameLog_Manager.Instance?.AddEntry($"+{amount} gold.");
            ZoneUIManager.Instance?.RefreshCurrentZone();
            return null;
        }
        if (a.StartsWith("GiveItem("))
        {
            var body = Between(a, "GiveItem(", ")").Split(',');
            string itemId = body[0].Trim();
            int amount = int.Parse(body[1].Trim());
            Inventory_Manager.Instance?.AddItem(itemId, amount);
            GameLog_Manager.Instance?.AddEntry($"Received {itemId} x{amount}.");
            ZoneUIManager.Instance?.RefreshCurrentZone();
            return null;
        }

        Debug.LogWarning($"[Events] Unknown action: {a}");
        return null;
    }
    private IEnumerator RunActionBlockingCo(string a, System.Action<string> setPendingZone, int index, List<string> allActions)
    {
        a = a.Trim();

        if (a.StartsWith("Narrate("))
        {
            string msg = Between(a, "Narrate(", ")");
            if (ZoneUIManager.Instance != null)
            {
                // this narrate is a blocking step; pre-fade and gate on Continue
                ZoneUIManager.Instance.FlagPendingTakeover();
                ZoneUIManager.Instance.BeginEventTakeoverFade();

                bool autoReturn = !WillTakeoverOrZoneSoon(allActions, index + 1);
                yield return ZoneUIManager.Instance.ShowNarrationAndWait(msg, autoReturn);

                // We’re done with this blocking step; clear pending takeover
                ZoneUIManager.Instance.ClearPendingTakeover();
            }
            yield break;
        }

        // Non-waiting actions reuse immediate handler
        var pending = RunActionImmediate(a);
        if (!string.IsNullOrEmpty(pending))
            setPendingZone?.Invoke(pending);
    }

    // Blocking path: yields for Narrate(...), mirrors the verbs above.
    private IEnumerator RunActionBlockingCo(string a, System.Action<string> setPendingZone)
    {
        a = a.Trim();

        if (a.StartsWith("Narrate("))
        {
            string msg = Between(a, "Narrate(", ")");
            if (ZoneUIManager.Instance != null)
            {
                ZoneUIManager.Instance.FlagPendingTakeover();
                ZoneUIManager.Instance.BeginEventTakeoverFade();
                // 👇 Do NOT auto redraw the zone; next actions will run (e.g., Zone(...))
                yield return ZoneUIManager.Instance.ShowNarrationAndWait(msg, autoReturn: false);
            }
            yield break;
        }

        // Non-waiting actions reuse immediate handler
        var pending = RunActionImmediate(a);
        if (!string.IsNullOrEmpty(pending))
            setPendingZone?.Invoke(pending);
    }

    private static bool WillTakeoverOrZoneSoon(List<string> acts, int startIndex)
    {
        if (acts == null) return false;
        for (int i = startIndex; i < acts.Count; i++)
        {
            var s = acts[i].TrimStart();
            // Skip instant, non-visual mutations
            if (s.StartsWith("SetFlag(") || s.StartsWith("GiveItem(") || s.StartsWith("GiveGold("))
                continue;

            // Any of these means we don't want to redraw the zone in between
            if (s.StartsWith("Zone(") || s.StartsWith("GotoZone(") ||
                s.StartsWith("StartDialog(") || s.StartsWith("Narrate("))
                return true;

            // Unknown verb → be conservative: do NOT auto-return
            return true;
        }
        return false; // nothing left
    }

    private IEnumerator ExecuteActionsCo(List<string> actions)
    {
        if (actions == null || actions.Count == 0) yield break;

        string pendingZone = null;

        for (int i = 0; i < actions.Count; i++)
            yield return RunActionBlockingCo(actions[i], z => pendingZone = z, i, actions);

        if (!string.IsNullOrEmpty(pendingZone))
        {
            yield return null; // let UI settle
            GameManager.Instance.GoToZone(pendingZone);
        }
    }


    private void ExecuteActions(List<string> actions)
    {
        if (actions == null || actions.Count == 0) return;

        string pendingZone = null;

        foreach (var a in actions)
        {
            var p = RunActionImmediate(a);
            if (!string.IsNullOrEmpty(p)) pendingZone = p;
        }

        if (!string.IsNullOrEmpty(pendingZone))
            GameManager.Instance.GoToZone(pendingZone);
    }


    public void StartDialogById(string dialogId)
    {
        Debug.Log($"[Dialog] Request to start '{dialogId}' (defer 1 frame)");
        StartCoroutine(DeferredStartDialog(dialogId));
    }

    private IEnumerator DeferredStartDialog(string dialogId)
    {
        // Wait 3 total frame passes
        yield return null;
        yield return null;
        yield return null;

        var gm = GameManager.Instance;
        var node = gm?.GetDialogById(dialogId);
        if (node == null) { Debug.LogError($"[Dialog] Missing node '{dialogId}'"); yield break; }

        var ui = ZoneUIManager.Instance ?? gm?.zoneUIManager;
        if (ui == null) { Debug.LogError("[Dialog] ZoneUIManager is null"); yield break; }

        Debug.Log($"[Dialog] Now displaying '{node.dialogId}' after zone render");
        ui.DisplayDialogNode(node);
    }



    static string Between(string s, string a, string b) { int i = s.IndexOf(a) + a.Length; int j = s.IndexOf(b, i); return s.Substring(i, j - i); }
    static int IntAfter(string s, string tok) { int i = s.IndexOf(tok) + tok.Length; return int.Parse(s.Substring(i).Trim()); }
}
