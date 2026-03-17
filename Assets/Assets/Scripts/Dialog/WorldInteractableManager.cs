using System.Text;
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

        RefreshUI();
    }

    public void Close()
    {
        ClearButtons();

        if (descriptionText) descriptionText.text = "";

        _story = null;
        _contextId = null;

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
            if (!string.IsNullOrEmpty(line))
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(line);
            }
        }

        if (descriptionText) descriptionText.text = sb.ToString();

        // Render choices
        var choices = _story.currentChoices;
        int shown = Mathf.Min(maxChoicesShown, choices.Count);

        for (int i = 0; i < shown; i++)
        {
            int choiceIndex = i;

            var btnObj = Instantiate(worldInteractableButtonPrefab, buttonLayoutGroup);

            var label = btnObj.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label) label.text = choices[i].text; // supports TMP rich text tags

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