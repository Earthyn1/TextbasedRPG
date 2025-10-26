// RequirementEvaluator.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public static class RequirementEvaluator
{
    public static bool Eval(string s) => EvaluateAll(new[] { s });

    static bool TryParenArg(string s, out string arg)
    {
        arg = null; int open = s.IndexOf('('), close = s.LastIndexOf(')');
        if (open < 0 || close <= open) return false;
        arg = s[(open + 1)..close].Trim(); return arg.Length > 0;
    }

    static bool IsQuestState(string x) =>
        x.Equals("QuestNotStarted", StringComparison.OrdinalIgnoreCase) ||
        x.Equals("QuestInProgress", StringComparison.OrdinalIgnoreCase) ||
        x.Equals("QuestCompleted", StringComparison.OrdinalIgnoreCase) ||
        x.Equals("QuestEverCompleted", StringComparison.OrdinalIgnoreCase) ||
        x.Equals("QuestHandedIn", StringComparison.OrdinalIgnoreCase);

    static bool EvalOne(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return true;
        s = s.Trim();
        var qm = QuestManager.Instance;

        // Flags
        if (s.StartsWith("Flag(", StringComparison.OrdinalIgnoreCase) && s.EndsWith(")"))
            return TryParenArg(s, out var key) && WorldState.GetFlag(key);
        if (s.StartsWith("NotFlag(", StringComparison.OrdinalIgnoreCase) && s.EndsWith(")"))
            return TryParenArg(s, out var key2) && !WorldState.GetFlag(key2);

        // Items
        if (s.StartsWith("Item(", StringComparison.OrdinalIgnoreCase))
        {
            int close = s.IndexOf(')'); if (close < 5) return false;
            string itemId = s.Substring(5, close - 5).Trim();
            string rest = s.Substring(close + 1).Trim();
            int have = Inventory_Manager.Instance?.CountOf(itemId) ?? 0;
            if (string.IsNullOrEmpty(rest)) return have >= 1;
            bool N(string x, out int v) => int.TryParse(x, out v);
            if (rest.StartsWith(">=") && N(rest[2..], out var ge)) return have >= ge;
            if (rest.StartsWith("<=") && N(rest[2..], out var le)) return have <= le;
            if (rest.StartsWith("==") && N(rest[2..], out var eq)) return have == eq;
            if (rest.StartsWith(">") && N(rest[1..], out var gt)) return have > gt;
            if (rest.StartsWith("<") && N(rest[1..], out var lt)) return have < lt;
            return have >= 1;
        }

        // Quests (function style)
        if (s.StartsWith("QuestInProgress(", StringComparison.OrdinalIgnoreCase) && s.EndsWith(")"))
            return TryParenArg(s, out var q1) && qm != null && qm.IsActive(q1);
        if (s.StartsWith("QuestNotStarted(", StringComparison.OrdinalIgnoreCase) && s.EndsWith(")"))
            return TryParenArg(s, out var q2) && qm != null && !qm.IsActive(q2) && !qm.IsCompleted(q2) && !qm.WasEverAccepted(q2);
        if (s.StartsWith("QuestCompleted(", StringComparison.OrdinalIgnoreCase) && s.EndsWith(")"))
            return TryParenArg(s, out var q3) && qm != null && qm.IsCompleted(q3);
        if (s.StartsWith("QuestEverCompleted(", StringComparison.OrdinalIgnoreCase) && s.EndsWith(")"))
            return TryParenArg(s, out var q4) && qm != null && qm.WasEverCompleted(q4);
        if (s.StartsWith("QuestHandedIn(", StringComparison.OrdinalIgnoreCase) && s.EndsWith(")"))
            return TryParenArg(s, out var q5) && qm != null && qm.WasEverHandedIn(q5);

        // Colon style: "State:QuestId" or "QuestId:State"
        int colon = s.IndexOf(':');
        if (colon > 0 && colon < s.Length - 1)
        {
            string a = s[..colon].Trim(), b = s[(colon + 1)..].Trim();
            if (!IsQuestState(a) && !IsQuestState(b))
            {
                Debug.LogWarning($"RequirementEvaluator: Unrecognized token '{s}'");
                return false;
            }
            string state = IsQuestState(a) ? a : b;
            string id = IsQuestState(a) ? b : a;
            if (qm == null) return true;
            return state switch
            {
                "QuestNotStarted" => !qm.IsActive(id) && !qm.IsCompleted(id) && !qm.WasEverAccepted(id),
                "QuestInProgress" => qm.IsActive(id),
                "QuestCompleted" => qm.IsCompleted(id),
                "QuestEverCompleted" => qm.WasEverCompleted(id),
                "QuestHandedIn" => qm.WasEverHandedIn(id),
                _ => false
            };
        }

        Debug.LogWarning($"RequirementEvaluator: Unrecognized condition '{s}'");
        return false;
    }

    public static bool EvaluateAll(string csvOrSemi)
    {
        if (string.IsNullOrWhiteSpace(csvOrSemi)) return true;
        var tokens = csvOrSemi.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var raw in tokens) if (!EvalOne(raw.Trim())) return false;
        return true;
    }

    public static bool EvaluateAll(IEnumerable<string> list)
    {
        if (list == null) return true;
        foreach (var cond in list) if (!EvalOne(cond)) return false;
        return true;
    }
}
