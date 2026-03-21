using System.Text;
using System.Text.RegularExpressions;
using Ink.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldInteractableManager : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private GameObject worldInteractableButtonPrefab; // prefab: Button + TMP label
    [SerializeField] private Transform buttonLayoutGroup;              // parent for buttons
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Behavior")]
    [SerializeField] private int maxChoicesShown = 3;
    [SerializeField] private bool autoCloseWhenDone = true;

    [Header("Ink")]
    public TextAsset inkJson;              // compiled .json from Inky

    private Story _story;
    private string _contextId; // npcId / interactableId, used by GameStateBridge
    private bool _waitingForMinigame;

    /// <summary>
    /// Opens an interactable Ink story at a knot/stitch.
    /// </summary>
    public void Open(TextAsset inkJSON, string startKnot, string contextId)
    {
        inkJson = inkJSON;

        if (inkJSON == null)
        {
            Debug.LogError("[WorldInteractableManager] inkJSON is null.");
            return;
        }

        _contextId = contextId;

        _story = new Story(inkJSON.text);

        // Bind game state functions (inventory, flags, etc) for this interactable context
        GameStateBridge.Bind(_story, _contextId);

        gameObject.SetActive(true);
        _waitingForMinigame = false;

        RefreshUI();
    }

    /// <summary>Called by GameStateBridge when Ink fires startMinigame().</summary>
    public void PauseForMinigame()
    {
        _waitingForMinigame = true;
        MinigameManager.Instance?.SetSpawnParent(buttonLayoutGroup);
    }

    private void OnEnable()
    {
        EventBus.OnTrigger += OnBusEvent;
    }

    private void OnDisable()
    {
        EventBus.OnTrigger -= OnBusEvent;
    }

    private void OnBusEvent(string trigger, object payload)
    {
        if (trigger == "MinigameResult" && _waitingForMinigame)
        {
            _waitingForMinigame = false;
            bool success = payload is bool b && b;

            // Jump directly to the correct knot — avoids variable timing issues
            string knot = success ? "MinigameFound" : "MinigameMissed";
            try { _story?.ChoosePathString(knot); }
            catch { Debug.LogWarning($"[WorldInteractableManager] Ink knot '{knot}' not found in story."); }

            RefreshUI();
        }
    }

    public void Close()
    {
        ClearButtons();

        if (descriptionText) descriptionText.text = "";

        _story = null;
        _contextId = null;
        _waitingForMinigame = false;

        gameObject.SetActive(false);
    }

    private void RefreshUI()
    {
        if (_story == null) return;

        ClearButtons();

        // Build description text from all continued lines
        var sb = new StringBuilder();
        while (_story.canContinue)
        {
            var line = _story.Continue().Trim();

            // startMinigame() fired mid-loop — stop draining, don't read ahead
            if (_waitingForMinigame) break;

            if (!string.IsNullOrEmpty(line))
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(line);
            }
        }

        if (descriptionText) descriptionText.text = sb.ToString();

        // Timing bar is spawned inside buttonLayoutGroup, just wait
        if (_waitingForMinigame) return;

        // Render choices
        var choices = _story.currentChoices;
        int shown = Mathf.Min(maxChoicesShown, choices.Count);

        for (int i = 0; i < shown; i++)
        {
            int choiceIndex = i;

            var btnObj = Instantiate(worldInteractableButtonPrefab, buttonLayoutGroup);

            var label = btnObj.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label) label.text = BuildChoiceLabel(choices[i], btnObj);

            var button = btnObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnChoiceClicked(choiceIndex));
            }
        }

        // Auto-close if story is done and no choices remain
        if (autoCloseWhenDone && !_story.canContinue && choices.Count == 0)
        {
            Close();
        }
    }

    // Matches <mg:params> and <req:SkillName,level> embedded in choice text
    private static readonly Regex _mgTagRegex  = new Regex(@"<mg:([^>]+)>",  RegexOptions.Compiled);
    private static readonly Regex _reqTagRegex = new Regex(@"<req:([^>]+)>", RegexOptions.Compiled);

    /// <summary>
    /// Returns clean display text for the button (strips mg: and req: tags).
    /// Drives DialogChoiceButton images and requirement label.
    ///
    /// Ink format:
    ///   * [Do the thing<mg:id,level,Type>]                    — minigame, no requirement
    ///   * [Pick the lock<mg:id,level,Type><req:Perception,3>] — minigame + skill gate
    /// </summary>
    private string BuildChoiceLabel(Ink.Runtime.Choice choice, GameObject btnObj)
    {
        string raw          = choice.text;
        var    mgMatch      = _mgTagRegex.Match(raw);
        var    reqMatch     = _reqTagRegex.Match(raw);
        var    choiceButton = btnObj.GetComponent<DialogChoiceButton>();
        var    button       = btnObj.GetComponent<UnityEngine.UI.Button>();

        // Strip both tags from display text
        string displayText = _mgTagRegex.Replace(raw, "");
        displayText        = _reqTagRegex.Replace(displayText, "").Trim();

        // ── No minigame tag — plain choice ────────────────────────────────────
        if (!mgMatch.Success)
        {
            choiceButton?.HideMinigameInfo();
            return displayText;
        }

        // ── Minigame choice ───────────────────────────────────────────────────
        string mgId = mgMatch.Groups[1].Value.Trim();

        if (choiceButton != null && MinigameManager.Instance != null)
        {
            Sprite skillSprite = MinigameManager.Instance.GetSkillSprite(mgId);
            choiceButton.SetMinigameInfo(skillSprite);
        }

        // ── Skill requirement check ───────────────────────────────────────────
        if (reqMatch.Success)
        {
            var    parts    = reqMatch.Groups[1].Value.Split(',');
            string skillStr = parts.Length >= 1 ? parts[0].Trim() : "";
            int    reqLevel = parts.Length >= 2 && int.TryParse(parts[1].Trim(), out int lvl) ? lvl : 1;

            bool reqMet = false;
            if (PlayerSkills.Instance != null &&
                System.Enum.TryParse(skillStr, true, out Enum_Skills skill))
            {
                int playerLevel = PlayerSkills.Instance.GetSkill(skill)?.level ?? 0;
                reqMet = playerLevel >= reqLevel;
            }

            if (!reqMet)
            {
                choiceButton?.ShowRequirement($"Req: {skillStr} {reqLevel}");
                if (button != null) button.interactable = false;
            }
            else
            {
                choiceButton?.HideRequirement();
            }
        }
        else
        {
            choiceButton?.HideRequirement();
        }

        return displayText;
    }

    private void OnChoiceClicked(int choiceIndex)
    {
        if (_story == null) return;

        var choices = _story.currentChoices;
        if (choiceIndex < 0 || choiceIndex >= choices.Count) return;

        _story.ChooseChoiceIndex(choiceIndex);
        RefreshUI();
    }

    private void ClearButtons()
    {
        if (buttonLayoutGroup == null) return;

        for (int i = buttonLayoutGroup.childCount - 1; i >= 0; i--)
        {
            Destroy(buttonLayoutGroup.GetChild(i).gameObject);
        }
    }
}