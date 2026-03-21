using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;


public class Interactables_Button : MonoBehaviour
{
    public TMP_Text text;
    public string InteractableName;
    public string FriendlyName;
    public Image NPCImage;
    public Image BGImage;
    public Image IconType;
    public bool isNPC;
    public string autoDialogID;
    [SerializeField] private Sprite defaultPortrait; // drag Portraits/default here if you have it


    public void NPCSetupButton(NPCInteractableData NPCData)
    {
        isNPC = true;
        text.text = NPCData.displayName;
        FriendlyName = NPCData.displayName;
        InteractableName = NPCData.id;
        autoDialogID = NPCData.autoDialog;

        // === SAFE PORTRAIT LOAD ===
        string portraitPath = $"Portraits/{NPCData.portrait}";
        Sprite loadedPortrait = Resources.Load<Sprite>(portraitPath);

        if (loadedPortrait != null)
        {
            NPCImage.sprite = loadedPortrait;
        }
        else
        {
            Debug.LogWarning($"[NPCSetupButton] Missing portrait at: {portraitPath}. Using default portrait.");
            NPCImage.sprite = Resources.Load<Sprite>("Portraits/default");
        }

        // === AGGRESSIVE DISPOSITION HANDLING ===
        if (NPCData.disposition == NPCDisposition.Aggressive)
        {
            BGImage.color = new Color32(215, 64, 64, 255);
            IconType.gameObject.SetActive(true);

            // === SAFE ATTACK ICON LOAD ===
            string attackIconPath = "Portraits/AttackIcon";
            Sprite attackIcon = Resources.Load<Sprite>(attackIconPath);

            if (attackIcon != null)
            {
                IconType.sprite = attackIcon;
            }
            else
            {
                Debug.LogWarning($"[NPCSetupButton] Missing AttackIcon at: {attackIconPath}. Hiding icon.");
                IconType.gameObject.SetActive(false);
            }
        }
        else
        {
            IconType.gameObject.SetActive(false);
        }
    }


    public void WorldSetupButton(InteractableData WorldInteractable)
    {
        if (WorldInteractable == null)
        {
            Debug.LogError("[WorldSetupButton] WorldInteractable is NULL.");
            SetDefaultPortrait();
            return;
        }
        if (NPCImage == null)
        {
            Debug.LogError("[WorldSetupButton] NPCImage ref is NULL on Interactables_Button.");
            return;
        }

        isNPC = false;
        InteractableName = WorldInteractable.id ?? "<null-id>";
        FriendlyName     = WorldInteractable.displayName ?? WorldInteractable.id;
        autoDialogID     = WorldInteractable.autoDialog;

        if (text != null)
            text.text = FriendlyName;

        if (string.IsNullOrWhiteSpace(WorldInteractable.portrait))
        {
            SetDefaultPortrait();
            return;
        }

        string portraitPath = $"Portraits/{WorldInteractable.portrait}";
        Sprite loadedSprite = Resources.Load<Sprite>(portraitPath);

        if (loadedSprite != null)
            NPCImage.sprite = loadedSprite;
        else
        {
            Debug.LogWarning($"[WorldSetupButton] Portrait not found at: {portraitPath}. Using default.");
            SetDefaultPortrait();
        }
    }

    private void SetDefaultPortrait()
    {
        if (NPCImage == null) return;

        // Prefer Inspector-assigned fallback if present
        if (defaultPortrait != null)
        {
            NPCImage.sprite = defaultPortrait;
            return;
        }

        // Otherwise try a conventional resources fallback
        var fallback = Resources.Load<Sprite>("Portraits/default");
        if (fallback != null)
        {
            NPCImage.sprite = fallback;
        }
        else
        {
            Debug.LogWarning("[WorldSetupButton] No fallback sprite assigned and 'Portraits/default' not found.");
            // Optionally: keep existing sprite instead of clearing
            // NPCImage.sprite = null;
        }
    }




public void ButtonPressed()
    {
        //Here we setup new interactable world object ui

        if (!string.IsNullOrEmpty(autoDialogID))
        {
            var node = GameManager.Instance.GetDialogById(autoDialogID);
            if (node != null)
            {
                GameManager.Instance.zoneUIManager.DisplayDialogNode(node);
                return;
            }
        }
        Debug.Log($"Action clicked: Go to zone {InteractableName}");

        var nextZone = GameManager.Instance.GetZoneByID(InteractableName);
        if (nextZone == null)
        {
            Debug.LogWarning($"Zone '{InteractableName}' not found!");
            return;
        }

        GameLog_Manager.Instance.AddEntry(isNPC
            ? "You approach " + FriendlyName
            : "You approach the " + FriendlyName);

        // record intent, then start the transition
        GameManager.Instance.QueueNpcInfo(nextZone, isNPC);
        GameManager.Instance.zoneUIManager.DisplayZone(nextZone, suppressHeader: true);

        
    }
}
