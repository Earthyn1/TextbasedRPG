using System.Collections;
using System.Collections.Generic;
using System.Text;
using Ink.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogManager : MonoBehaviour
{
    [Header("UI Refs")]
    public GameObject         dialogButtonPrefab;
    public Transform          buttonLayoutGroup;
    public TextMeshProUGUI    descriptionText;
    public TextMeshProUGUI    npcNameText;
    public Image              portait;
    public GameObject         mainTextBox;

    [Header("Animation")]
    [SerializeField] private CanvasGroup dialogCanvasGroup;  // root panel — fades on open/close
    [SerializeField] private CanvasGroup textCanvasGroup;    // description text area — fades per choice
    [SerializeField] private float panelFadeDuration  = 0.25f;
    [SerializeField] private float textFadeDuration   = 0.18f;
    [SerializeField] private float buttonFadeDuration = 0.15f;
    [SerializeField] private float buttonStagger      = 0.08f;

    [Header("Ink")]
    public TextAsset inkJson;

    private Story     _story;
    private bool      _waitingForMinigame;
    private Coroutine _animCoroutine;

    // ── EventBus ───────────────────────────────────────────────────────────────

    private void OnEnable()  { EventBus.OnTrigger += OnBusEvent; }
    private void OnDisable() { EventBus.OnTrigger -= OnBusEvent; }

    private void OnBusEvent(string trigger, object payload)
    {
        if (trigger == "MinigameResult" && _waitingForMinigame)
        {
            _waitingForMinigame = false;
            bool   success = payload is bool b && b;
            string knot    = success ? "MinigameFound" : "MinigameMissed";
            try { _story?.ChoosePathString(knot); }
            catch { Debug.LogWarning($"[DialogManager] Ink knot '{knot}' not found in story."); }

            RunAnim(RefreshAfterMinigame());
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public void PauseForMinigame()
    {
        _waitingForMinigame = true;
        MinigameManager.Instance?.SetSpawnParent(buttonLayoutGroup);
    }

    public void StartDialog(TextAsset inkJSON, string startKnot, string npcId, string displayName = "")
    {
        FindFirstObjectByType<RightPanelTabs>()?.CloseAll();
        LockedMessageToast.Instance?.Hide();

        inkJson = inkJSON;
        if (inkJson == null) { Debug.LogError("[DialogManager] inkJson is not assigned."); return; }

        if (npcNameText) npcNameText.text = string.IsNullOrEmpty(displayName) ? npcId : displayName;

        portait.sprite = Resources.Load<Sprite>($"Portraits/Dialog_Portraits/{npcId}");
        _story = new Story(inkJson.text);
        GameStateBridge.Bind(_story, npcId);

        // Start fully transparent then animate in
        if (dialogCanvasGroup) dialogCanvasGroup.alpha = 0f;
        if (textCanvasGroup)   textCanvasGroup.alpha   = 1f;

        gameObject.SetActive(true);
        mainTextBox.SetActive(false);

        RunAnim(OpenSequence());
    }

    public void CloseDialog()
    {
        RunAnim(CloseSequence());
    }

    // ── Animation Sequences ────────────────────────────────────────────────────

    private IEnumerator OpenSequence()
    {
        BuildDescriptionText();

        // Whole panel (including text) fades in together
        if (dialogCanvasGroup)
            yield return FadeCG(dialogCanvasGroup, 0f, 1f, panelFadeDuration);

        if (_waitingForMinigame) yield break;

        // Buttons stagger in after panel is fully visible
        yield return SpawnAndFadeButtons();
    }

    private IEnumerator ChoiceTransition(int choiceIndex)
    {
        // Fade out buttons and NPC text simultaneously
        var fadeButtons = StartCoroutine(FadeOutButtons());
        Coroutine fadeText = null;
        if (textCanvasGroup)
            fadeText = StartCoroutine(FadeCG(textCanvasGroup, 1f, 0f, textFadeDuration));

        yield return fadeButtons;
        if (fadeText != null) yield return fadeText;

        // Advance story
        ClearButtons();
        _story.ChooseChoiceIndex(choiceIndex);
        BuildDescriptionText();

        // If story is over, show final line then close
        if (!_story.canContinue && _story.currentChoices.Count == 0)
        {
            if (textCanvasGroup) yield return FadeCG(textCanvasGroup, 0f, 1f, textFadeDuration);
            yield return new WaitForSeconds(1.2f);
            yield return CloseSequence();
            yield break;
        }

        // Fade NPC text back in
        if (textCanvasGroup) yield return FadeCG(textCanvasGroup, 0f, 1f, textFadeDuration);

        if (!_waitingForMinigame)
            yield return SpawnAndFadeButtons();
    }

    private IEnumerator CloseSequence()
    {
        yield return FadeOutButtons();

        if (textCanvasGroup) yield return FadeCG(textCanvasGroup, 1f, 0f, textFadeDuration);
        if (dialogCanvasGroup) yield return FadeCG(dialogCanvasGroup, 1f, 0f, panelFadeDuration);

        ClearButtons();
        if (descriptionText) descriptionText.text = "";
        if (npcNameText)     npcNameText.text      = "";
        _story              = null;
        _waitingForMinigame = false;
        gameObject.SetActive(false);
        mainTextBox.SetActive(true);
    }

    private IEnumerator RefreshAfterMinigame()
    {
        ClearButtons();
        if (textCanvasGroup) yield return FadeCG(textCanvasGroup, 1f, 0f, textFadeDuration);
        BuildDescriptionText();
        if (textCanvasGroup) yield return FadeCG(textCanvasGroup, 0f, 1f, textFadeDuration);
        if (!_waitingForMinigame)
            yield return SpawnAndFadeButtons();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void BuildDescriptionText()
    {
        if (_story == null) return;

        var sb = new StringBuilder();
        while (_story.canContinue)
        {
            var line = _story.Continue().Trim();
            if (_waitingForMinigame) break;
            if (!string.IsNullOrEmpty(line))
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(FormatDialogLine(line));
            }
        }

        if (descriptionText) descriptionText.text = sb.ToString();
    }

    private IEnumerator SpawnAndFadeButtons()
    {
        if (_story == null) yield break;

        var choices = _story.currentChoices;
        int shown   = Mathf.Min(3, choices.Count);
        var groups  = new List<CanvasGroup>(shown);

        for (int i = 0; i < shown; i++)
        {
            int idx    = i;
            var btnObj = Instantiate(dialogButtonPrefab, buttonLayoutGroup);

            var label = btnObj.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label) label.text = StripQuotes(choices[i].text);

            var button = btnObj.GetComponent<Button>();
            button.onClick.AddListener(() => OnChoiceClicked(idx));

            var cg   = btnObj.GetComponent<CanvasGroup>() ?? btnObj.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            groups.Add(cg);
        }

        // Stagger each button in
        foreach (var cg in groups)
        {
            StartCoroutine(FadeCG(cg, 0f, 1f, buttonFadeDuration));
            yield return new WaitForSeconds(buttonStagger);
        }

        // Wait for the last button to finish fading
        if (groups.Count > 0)
            yield return new WaitForSeconds(buttonFadeDuration);
    }

    private IEnumerator FadeOutButtons()
    {
        var coroutines = new List<Coroutine>();

        foreach (Transform child in buttonLayoutGroup)
        {
            var cg = child.GetComponent<CanvasGroup>();
            if (cg != null)
                coroutines.Add(StartCoroutine(FadeCG(cg, cg.alpha, 0f, textFadeDuration)));
        }

        foreach (var c in coroutines)
            yield return c;
    }

    private void OnChoiceClicked(int choiceIndex)
    {
        if (_story == null) return;
        if (choiceIndex < 0 || choiceIndex >= _story.currentChoices.Count) return;
        RunAnim(ChoiceTransition(choiceIndex));
    }

    private void ClearButtons()
    {
        if (buttonLayoutGroup == null) return;
        for (int i = buttonLayoutGroup.childCount - 1; i >= 0; i--)
            Destroy(buttonLayoutGroup.GetChild(i).gameObject);
    }

    /// <summary>Stops any running animation and starts a new one.</summary>
    private void RunAnim(IEnumerator sequence)
    {
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(sequence);
    }

    private IEnumerator FadeCG(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;
        float t = 0f;
        cg.alpha = from;
        while (t < duration)
        {
            t       += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        cg.alpha = to;
    }

    private static string FormatDialogLine(string line) => StripQuotes(line);

    private static string StripQuotes(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        text = text.Trim();
        if (text.StartsWith("\"") && text.EndsWith("\"") && text.Length > 2)
            return text.Substring(1, text.Length - 2);
        return text;
    }
}
