using System.Text;
using Ink.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogManager : MonoBehaviour
{
    [Header("UI Refs")]
    public GameObject dialogButtonPrefab;   // prefab with Button + TMP text
    public Transform buttonLayoutGroup;     // parent for buttons
    public TextMeshProUGUI descriptionText;
    public Image portait;
    public GameObject mainTextBox;  


    [Header("Ink")]
    public TextAsset inkJson;              // compiled .json from Inky

    private Story _story;

    public void StartDialog(TextAsset inkJSON, string startKnot, string npcId)
    {
        FindFirstObjectByType<RightPanelTabs>()?.CloseAll();

        inkJson = inkJSON;

        if (inkJson == null)
        {
            Debug.LogError("[DialogManager] inkJson is not assigned.");
            return;
        }

        var loadedPortrait = Resources.Load<Sprite>($"Portraits/Dialog_Portraits/{npcId}");

        portait.sprite = loadedPortrait;
        _story = new Story(inkJson.text);

        // ✅ Bind all game state functions for this NPC
        GameStateBridge.Bind(_story, npcId);

        gameObject.SetActive(true);
        mainTextBox.SetActive(false);
        RefreshUI();
    }

    public void CloseDialog()
    {
        ClearButtons();
        if (descriptionText) descriptionText.text = "";
        _story = null;
        gameObject.SetActive(false);
        mainTextBox.SetActive(true);
    }

    private void RefreshUI()
    {
        if (_story == null) return;

        ClearButtons();

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

        var choices = _story.currentChoices;
        int shown = Mathf.Min(3, choices.Count);

        for (int i = 0; i < shown; i++)
        {
            int choiceIndex = i;

            var btnObj = Instantiate(dialogButtonPrefab, buttonLayoutGroup);

            var label = btnObj.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label) label.text = choices[i].text;

            var button = btnObj.GetComponent<Button>();
            button.onClick.AddListener(() => OnChoiceClicked(choiceIndex));
        }

        // Auto-close if story is done and no choices remain
        if (!_story.canContinue && choices.Count == 0)
            CloseDialog();
    }

    private void OnChoiceClicked(int choiceIndex)
    {
        if (_story == null) return;
        if (choiceIndex < 0 || choiceIndex >= _story.currentChoices.Count) return;

        _story.ChooseChoiceIndex(choiceIndex);
        RefreshUI();
    }

    private void ClearButtons()
    {
        if (buttonLayoutGroup == null) return;

        for (int i = buttonLayoutGroup.childCount - 1; i >= 0; i--)
            Destroy(buttonLayoutGroup.GetChild(i).gameObject);
    }
}