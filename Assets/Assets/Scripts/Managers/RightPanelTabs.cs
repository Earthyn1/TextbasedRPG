using UnityEngine;

public class RightPanelTabs : MonoBehaviour
{
    [Header("Panels")]
    public GameObject journalPanel;
    public GameObject characterPanel;
    public GameObject inventoryPanel;
    public GameObject skillsPanel;

    [Header("Interaction State")]
    [SerializeField] private InteractionUI interactionUI;

    void Start()
    {
        CloseAll();
    }

    public void ToggleJournal()
    {
        TogglePanel(journalPanel);
    }

    public void ToggleCharacter()
    {
        TogglePanel(characterPanel);
    }

    public void ToggleInventory()
    {
        TogglePanel(inventoryPanel);
    }

    public void ToggleSkills()
    {
        TogglePanel(skillsPanel);
    }

    void TogglePanel(GameObject panel)
    {
        // 🚫 Don't allow panels to open during dialog
        if (interactionUI != null && interactionUI.IsDialogsOpen)
            return;

        bool isActive = panel.activeSelf;

        CloseAll();

        if (!isActive)
            panel.SetActive(true);
    }

    public void CloseAll()
    {
        journalPanel.SetActive(false);
        characterPanel.SetActive(false);
        inventoryPanel.SetActive(false);
        skillsPanel.SetActive(false);
    }
}