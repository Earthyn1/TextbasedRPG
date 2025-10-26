using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public string startingZoneId;
    public int playerGold;
    public static GameManager Instance { get; private set; }

    public List<ZoneData> zones;
    private Dictionary<string, ZoneData> zoneLookup;

    public List<DialogNode> dialogs;
    private Dictionary<string, DialogNode> dialogLookup;

    public List<QuestData> quests;
    private Dictionary<string, QuestData> questLookup;
    public Book_Manager book_Manager;

    [SerializeField] private TimedActionLoader timedActionLoader;
    private Dictionary<string, TimedActionDef> _timedActionDefs;

    private readonly HashSet<string> _completedTimedActions = new HashSet<string>();




    [SerializeField] private ZoneLoader zoneLoader; // assign in inspector
    [SerializeField] private DialogLoader dialogLoader; // assign in inspector
    [SerializeField] private QuestLoader questLoader; // assign in inspector


    [SerializeField] public ZoneUIManager zoneUIManager; // assign in inspector
    [SerializeField] public QuestManager questManager; // assign in inspector

    public RectTransform middlePanel_Panels;
    public RectTransform rightPanel_Panels;

    private ZoneData _pendingNpcZone;
    private bool _wantNpcPanel;

    public static event System.Action<ZoneData> OnZoneWillChange;

    private bool _navLocked;
    public bool IsNavigationLocked => _navLocked;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadZones();

        LoadDialogs();

        LoadQuests();

        LoadTimedActions();

        zoneUIManager.OnZoneDisplayed += OnZoneDisplayedHandler;

    }

    private void Start()
    {
        StartCoroutine(BootSequence());
    }

    private IEnumerator BootSequence()
    {
        var startingZone = GetZoneByID(startingZoneId);
        if (startingZone == null)
        {
            Debug.LogError("[GameManager] Starting zone not found.");
            yield break;
        }

        WorldState.CurrentZoneId = startingZone.id;

        var ui = zoneUIManager;

        // 1) Clear + header only (no buttons yet)
        ui.PrepareZoneTransition();
        ui.DisplayZoneHeaderOnly(startingZone);
        OnZoneWillChange?.Invoke(startingZone);

        // 2) Fire events BEFORE marking visited
        EventBus.Fire("OnEnterZone", startingZone.id);

        // 3) Arm first-click latch for this zone
        ui.ArmFirstClickTrigger(startingZone.id);

        // 4) Now mark visited
        WorldState.MarkVisited(startingZone.id);

        // ⚠️ NEW: Let events tick a frame so they can flag a takeover
        yield return null; // (you can make this 2–3 frames if you prefer)

        // 5) If nothing took over, render full zone (buttons/actions)
        if (!ui.HasPendingTakeover && !ui.IsBlockingNarration)
            ui.DisplayZone(startingZone);
    }


    public void SetNavigationLocked(bool locked)
    {
        _navLocked = locked;
    }
    private void LoadTimedActions()
    {
        var defs = timedActionLoader.LoadActions();
        _timedActionDefs = new Dictionary<string, TimedActionDef>();

        if (defs != null)
        {
            foreach (var d in defs)
            {
                if (!string.IsNullOrEmpty(d.id))
                    _timedActionDefs[d.id] = d;
            }
        }
    }

    public TimedActionDef GetTimedAction(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        _timedActionDefs.TryGetValue(id, out var def);
        return def;
    }

    private void LoadZones()
    {
        zones = zoneLoader.LoadZones();
        if (zones == null)
        {
            Debug.LogError("No zones loaded!");
            return;
        }

        CreateLookup();
    }
    public bool IsTimedActionDone(string timedActionId)
    {
        if (string.IsNullOrEmpty(timedActionId)) return false;
        return _completedTimedActions.Contains(timedActionId);
    }

    public void MarkTimedActionDone(string timedActionId)
    {
        if (string.IsNullOrEmpty(timedActionId)) return;
        if (_completedTimedActions.Add(timedActionId))
        {
            // TODO: persist via your save system if desired
            // SaveSystem.QueueAutosave();
            Debug.Log($"[GameManager] Timed action completed: {timedActionId}");
        }
    }
    private void CreateLookup()
    {
        zoneLookup = new Dictionary<string, ZoneData>();

        foreach (var zone in zones)
        {
            if (string.IsNullOrEmpty(zone?.id))
            {
                Debug.LogWarning("Skipping zone with null/empty id");
                continue;
            }
            if (zoneLookup.ContainsKey(zone.id))
            {
                Debug.LogWarning($"Duplicate id detected: {zone.id}, overwriting.");
            }
            zoneLookup[zone.id] = zone;
        }
    }


    private void LoadDialogs()
    {
        dialogs = dialogLoader.LoadDialogs();
        if (dialogs == null)
        {
            Debug.LogError("No dialogs loaded!");
            return;
        }

        CreateDialogLookup();
    }

    private void CreateDialogLookup()
    {
        dialogLookup = new Dictionary<string, DialogNode>();
        foreach (var dialog in dialogs)
        {
            dialogLookup[dialog.dialogId] = dialog;
        }
    }

    public ZoneData GetZoneByID(string zoneName)
    {
        if (zoneLookup == null)
        {
            Debug.LogWarning("Zone lookup dictionary not created yet.");
            return null;
        }

        zoneLookup.TryGetValue(zoneName, out var zone);
        return zone;
    }


    public void GoToZone(string zoneId)
    {
        StartCoroutine(GoToZoneCo(zoneId));
    }

    // GameManager.cs
    private IEnumerator GoToZoneCo(string zoneId)
    {
        if (_navLocked)
        {
            GameLog_Manager.Instance?.AddEntry("You’re focused on your current action.");
            yield break;
        }

        var ui = ZoneUIManager.Instance;
        var zone = GetZoneByID(zoneId);
        if (zone == null)
        {
            Debug.LogWarning($"[GameManager] Zone '{zoneId}' not found.");
            yield break;
        }

        WorldState.CurrentZoneId = zoneId;

        // ✅ Clear UI & show header first (NO buttons)
        ui?.PrepareZoneTransition();
        ui?.DisplayZoneHeaderOnly(zone);
        OnZoneWillChange?.Invoke(zone);

        // ✅ Fire BEFORE MarkVisited so FirstTimeInZone works
        Debug.Log($"[Game] OnEnterZone -> {zoneId}");
        EventBus.Fire("OnEnterZone", zoneId);

        // ✅ ARM HERE (so FirstTimeInZone is still true)
        Debug.Log($"[FirstClick] Armed for {zoneId} at frame {Time.frameCount}");
        ui?.ArmFirstClickTrigger(zoneId);

        // ✅ Now mark visited
        WorldState.MarkVisited(zoneId);

        // ✅ If no takeover, build the full zone body now
        if (!ui.HasPendingTakeover)
            ui.DisplayZone(zone);
        // else: an event/dialog will take over next; do nothing here
    }




    public DialogNode GetDialogById(string dialogId)
    {
        if (dialogLookup == null)
        {
            Debug.LogWarning("Dialog lookup dictionary not created yet.");
            return null;
        }
        Debug.Log("DIalogID" + dialogId);
        dialogLookup.TryGetValue(dialogId, out var dialogNode);
        return dialogNode;
    }

    private void LoadQuests()
    {
        quests = questLoader.LoadQuests();
        if (quests == null)
        {
            Debug.LogError("No quests loaded!");
            return;
        }

        CreateQuestLookup();
    }

    private void CreateQuestLookup()
    {
        questLookup = new Dictionary<string, QuestData>();
        foreach (var quest in quests)
        {
            questLookup[quest.questId] = quest;
        }
    }

    // Example: method to get quest by ID
    public QuestData GetQuestById(string questId)
    {
        if (questLookup == null)
        {
            Debug.LogWarning("Quest lookup dictionary not created yet.");
            return null;
        }

        questLookup.TryGetValue(questId, out var quest);
        return quest;
    }

    public void AddGold(int amount)
    {
        playerGold = playerGold + amount;
    }

    public void clearMiddlePanels()
    {
        foreach (Transform child in middlePanel_Panels)
        {
            child.gameObject.SetActive(false);
        }
    }

    public void clearRightPanels()
    {
        foreach (Transform child in rightPanel_Panels)
        {
            child.gameObject.SetActive(false);
        }
    }

    public void QueueNpcInfo(ZoneData zone, bool wantNpcInfo)
    {
        _pendingNpcZone = zone;
        _wantNpcPanel = wantNpcInfo;
    }

    private void OnZoneDisplayedHandler(ZoneData shown)
    {
        if (_wantNpcPanel && _pendingNpcZone == shown)
            SetupNPCInfo_Panel(shown);

        _wantNpcPanel = false;
        _pendingNpcZone = null;

       
    }

    public void SetupNPCInfo_Panel(ZoneData zoneData)
    {
        clearRightPanels(); // keep if you want a clean right side before showing NPC info
        zoneUIManager.NPCInfoHolder.SetActive(true);

        var npcInfoData = zoneUIManager.NPCInfoHolder.GetComponent<NpcInfo_Data>();
        npcInfoData.SetupInfo(zoneData);
    }


}
