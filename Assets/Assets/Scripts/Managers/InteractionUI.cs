using Ink.Runtime;
using UnityEngine;

public class InteractionUI : MonoBehaviour, IWorldInteractionUI
{
    [SerializeField] private DialogManager dialogManager;
    [SerializeField] private WorldInteractableManager worldInteractableManager;
    [SerializeField] private GameObject RootPanels;
    [SerializeField] private GameObject RootDialogsPanel; // container that stays active

    bool AnyDirectChildActive(GameObject root, GameObject excludeChild = null)
    {
        if (root == null) return false;

        foreach (Transform child in root.transform)
        {
            if (excludeChild != null && child.gameObject == excludeChild)
                continue;

            // use activeSelf so only "opened" panels count, not parent hierarchy
            if (child.gameObject.activeSelf)
                return true;
        }
        return false;
    }

    bool AnyChildActive(GameObject root)
    {
        if (root == null) return false;

        foreach (Transform child in root.transform)
        {
            if (child.gameObject.activeSelf)
                return true;
        }
        return false;
    }

    // Dialog open = any child inside RootDialogsPanel is enabled (DialogPrefab, WorldInteractablesDialogBox, etc.)
    public bool IsDialogsOpen => AnyChildActive(RootDialogsPanel);

    // Busy = any top-level panel open (excluding dialog container) OR dialogs open
    public bool IsBusy =>
        AnyDirectChildActive(RootPanels, excludeChild: RootDialogsPanel) ||
        IsDialogsOpen;

    public void OpenNpcDialog(ZoneData npc, string startKnot)
    {
        Debug.Log($"[InteractionUI] OpenNpcDialog npc='{npc.id}' knot='{startKnot}'");

        if (dialogManager == null)
        {
            Debug.LogError("[InteractionUI] dialogManager is NULL!");
            return;
        }

        TextAsset inkJSON = Resources.Load<TextAsset>($"InkDialogs/{startKnot}");

        if (inkJSON == null)
        {
            Debug.LogError($"Ink file not found at InkDialogs/{startKnot}");
            return;
        }

        dialogManager.StartDialog(inkJSON, startKnot, npc.id);
    }

    public void OpenWorldObject(ZoneData worldObj, string startKnot)
    {
        Debug.Log($"[InteractionUI] OpenWorldObject worldObj='{worldObj.id}' knot='{startKnot}'");

        if (worldInteractableManager == null)
        {
            Debug.LogError("[InteractionUI] worldInteractableManager is NULL!");
            return;
        }

        TextAsset inkJSON = Resources.Load<TextAsset>($"InkDialogs/{startKnot}");
        if (inkJSON == null)
        {
            Debug.LogError($"Ink file not found at InkDialogs/{startKnot}");
            return;
        }
        worldInteractableManager.Open(inkJSON, startKnot, worldObj.id);
    }

    public void OpenZoneTravel(ZoneData zone)
    {
        GameManager.Instance.GoToZone(zone.id);
        GameLog_Manager.Instance.AddEntry("You head to the " + zone.displayName);
    }
}