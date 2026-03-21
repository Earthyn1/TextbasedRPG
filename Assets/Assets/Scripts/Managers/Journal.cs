using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the three inner tabs of the Journal: Quests, Character, Skills.
/// Attach to the Journal root GameObject.
/// Wire up the three panels and three buttons in the Inspector.
/// </summary>
public class Journal : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject questsPanel;
    [SerializeField] private GameObject characterPanel;
    [SerializeField] private GameObject skillsPanel;

    [Header("Tab Buttons")]
    [SerializeField] private Button questsButton;
    [SerializeField] private Button characterButton;
    [SerializeField] private Button skillsButton;

    private void OnEnable()
    {
        // Default to Quests tab whenever the journal opens
        ShowQuests();
    }

    public void ShowQuests()
    {
        SetActivePanel(questsPanel);
        SetActiveButton(questsButton);
    }

    public void ShowCharacter()
    {
        SetActivePanel(characterPanel);
        SetActiveButton(characterButton);
    }

    public void ShowSkills()
    {
        SetActivePanel(skillsPanel);
        SetActiveButton(skillsButton);
    }

    private void SetActivePanel(GameObject target)
    {
        questsPanel.SetActive(questsPanel == target);
        characterPanel.SetActive(characterPanel == target);
        skillsPanel.SetActive(skillsPanel == target);
    }

    private void SetActiveButton(Button active)
    {
        questsButton.interactable    = questsButton    != active;
        characterButton.interactable = characterButton != active;
        skillsButton.interactable    = skillsButton    != active;
    }
}
