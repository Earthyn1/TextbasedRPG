// RequirementEvaluator.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public static class RequirementEvaluator
{
    private static List<string> SplitTopLevel(string expr)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(expr))
            return result;

        int depth = 0;
        int start = 0;

        for (int i = 0; i < expr.Length; i++)
        {
            char c = expr[i];

            if (c == '(') depth++;
            else if (c == ')') depth--;

            // split on commas/semicolons/&& only at depth 0
            bool atTop = (depth == 0);

            // handle &&
            if (atTop && i + 1 < expr.Length && expr[i] == '&' && expr[i + 1] == '&')
            {
                // add segment before &&
                string seg = expr.Substring(start, i - start);
                if (!string.IsNullOrWhiteSpace(seg))
                    result.Add(seg.Trim());
                i++; // skip second &
                start = i + 1;
                continue;
            }

            // handle comma / semicolon
            if (atTop && (c == ',' || c == ';'))
            {
                string seg = expr.Substring(start, i - start);
                if (!string.IsNullOrWhiteSpace(seg))
                    result.Add(seg.Trim());
                start = i + 1;
            }
        }

        // last segment
        if (start < expr.Length)
        {
            string seg = expr.Substring(start);
            if (!string.IsNullOrWhiteSpace(seg))
                result.Add(seg.Trim());
        }

        return result;
    }

    public static bool Eval(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return true;

        // we now let SplitTopLevel handle && and commas
        var parts = SplitTopLevel(s);
        foreach (var p in parts)
        {
            if (!EvalOne(p))
                return false;
        }
        return true;
    }


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

        // quest_active(chicken_egg_delivery)
        if (s.StartsWith("quest_active(", System.StringComparison.OrdinalIgnoreCase) && s.EndsWith(")"))
        {
            if (TryParenArg(s, out var questId))
            {
                bool active = QuestManager.Instance != null && QuestManager.Instance.IsActive(questId);
                Debug.Log($"[REQ] quest_active('{questId}') -> {active}");
                return active;
            }
            Debug.Log("[REQ] quest_active(...) bad arg");
            return false;
        }

        // quest_item_needed(questId, itemId, required)
        if (s.StartsWith("quest_item_needed(", System.StringComparison.OrdinalIgnoreCase) && s.EndsWith(")"))
        {
            if (TryParenArg(s, out var argsStr))
            {
                var parts = argsStr.Split(',');
                if (parts.Length >= 3)
                {
                    string questId = parts[0].Trim();
                    string itemId = parts[1].Trim();
                    string reqStr = parts[2].Trim();

                    if (int.TryParse(reqStr, out int required))
                    {
                        int current = QuestManager.Instance != null
                            ? QuestManager.Instance.GetQuestItemCount(questId, itemId)
                            : 0;

                        bool needed = current < required;
                        Debug.Log($"[REQ] quest_item_needed('{questId}','{itemId}',{required}) -> have={current} needed={needed}");
                        return needed;
                    }
                    else
                    {
                        Debug.Log($"[REQ] quest_item_needed(...) bad required='{reqStr}'");
                        return false;
                    }
                }
                Debug.Log("[REQ] quest_item_needed(...) not enough args");
                return false;
            }
            Debug.Log("[REQ] quest_item_needed(...) bad parens");
            return false;
        }





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

    public static bool EvaluateAll(string expr)
    {
        if (string.IsNullOrWhiteSpace(expr))
            return true;

        // support: commas, semicolons, and "&&" (AND)
        // we'll just treat them all as AND
        var rawTokens = expr
            .Replace("&&", ",")   // normalize && to comma
            .Split(new[] { ',', ';' }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (var raw in rawTokens)
        {
            var token = raw.Trim();
            if (!EvalOne(token))
                return false;
        }

        return true;
    }


    public static bool EvaluateAll(IEnumerable<string> list)
    {
        if (list == null) return true;

        foreach (var cond in list)
        {
            if (string.IsNullOrWhiteSpace(cond))
                continue;

            var parts = SplitTopLevel(cond);
            foreach (var p in parts)
            {
                if (!EvalOne(p))
                    return false;
            }
        }

        return true;
    }


}
