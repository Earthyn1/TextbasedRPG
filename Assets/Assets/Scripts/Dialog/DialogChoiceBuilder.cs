using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared logic for populating a DialogChoiceButton from an Ink choice.
/// Used by both DialogManager (NPC dialog) and WorldInteractableManager (world objects).
///
/// Ink tag format:
///   * [Do the thing<mg:id,level,Type>]                    — minigame, no requirement
///   * [Pick the lock<mg:id,level,Type><req:Perception,3>] — minigame + skill gate
/// </summary>
public static class DialogChoiceBuilder
{
    private static readonly Regex _mgTagRegex  = new Regex(@"<mg:([^>]+)>",  RegexOptions.Compiled);
    private static readonly Regex _reqTagRegex = new Regex(@"<req:([^>]+)>", RegexOptions.Compiled);

    /// <summary>
    /// Applies display text, minigame icon, and skill gate to a button GameObject.
    /// Returns the clean display text (quotes and tags stripped).
    /// </summary>
    public static string Build(Ink.Runtime.Choice choice, GameObject btnObj)
    {
        string raw        = choice.text;
        var    mgMatch    = _mgTagRegex.Match(raw);
        var    reqMatch   = _reqTagRegex.Match(raw);
        var    dcb        = btnObj.GetComponent<DialogChoiceButton>();
        var    button     = btnObj.GetComponent<Button>();

        // Strip tags then quotes from display text
        string displayText = _mgTagRegex.Replace(raw, "");
        displayText        = _reqTagRegex.Replace(displayText, "").Trim();
        displayText        = StripQuotes(displayText);

        // ── Plain choice ──────────────────────────────────────────────────────
        if (!mgMatch.Success)
        {
            dcb?.HideMinigameInfo();
            return displayText;
        }

        // ── Minigame choice ───────────────────────────────────────────────────
        string mgId = mgMatch.Groups[1].Value.Trim();

        if (dcb != null && MinigameManager.Instance != null)
            dcb.SetMinigameInfo(MinigameManager.Instance.GetSkillSprite(mgId));

        // ── Skill requirement check ───────────────────────────────────────────
        if (reqMatch.Success)
        {
            var    parts       = reqMatch.Groups[1].Value.Split(',');
            string skillStr    = parts.Length >= 1 ? parts[0].Trim() : "";
            int    reqLevel    = parts.Length >= 2 && int.TryParse(parts[1].Trim(), out int lvl) ? lvl : 1;
            int    playerLevel = 0;

            if (PlayerSkills.Instance != null &&
                System.Enum.TryParse(skillStr, true, out Enum_Skills skill))
            {
                playerLevel = PlayerSkills.Instance.GetSkill(skill)?.level ?? 0;
            }

            bool reqMet = playerLevel >= reqLevel;

            if (!reqMet)
            {
                dcb?.ShowRequirementNotMet(playerLevel, reqLevel);
                if (button != null) button.interactable = false;
            }
            else
            {
                dcb?.ShowRequirementMet(reqLevel);
            }
        }
        else
        {
            dcb?.HideRequirement();
        }

        return displayText;
    }

    public static string StripQuotes(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        text = text.Trim();
        if (text.StartsWith("\"") && text.EndsWith("\"") && text.Length > 2)
            return text.Substring(1, text.Length - 2);
        return text;
    }
}
