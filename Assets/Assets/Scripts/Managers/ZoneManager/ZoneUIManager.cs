using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZoneUIManager : MonoBehaviour
{
    public static ZoneUIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] TMP_Text zoneNameText;
    [SerializeField] TMP_Text zoneDescriptionText;
    [SerializeField] TMP_Text interactablesTitle;
    [SerializeField] Button narrationContinueButton;

    // ⬇️ Back-compat shims
    [SerializeField] public GameObject NPCInfoHolder;   // assign same GO in Inspector


    [Header("Panels / Parents")]
    [SerializeField] RectTransform actionPanel;
    [SerializeField] GameObject interactablesHolders;  // parent wrapper for NPC + World sections

    [Header("Portraits / Fade")]
    [SerializeField] Image npcPortraitImage;
    [SerializeField] Image npcPortraitImage2;
    [SerializeField] Sprite defaultNarratorPortrait;
    [SerializeField] CanvasGroup descriptionGroup;
    [SerializeField] CanvasGroup actionsGroup;
    [SerializeField] float fadeDuration = 0.25f;

    [Header("Delegates")]
    [SerializeField] DialogUI dialogUI;
    [SerializeField] ZoneElementSpawner elementSpawner;


    // state
    public ZoneData LastZoneEntered { get; private set; }
    public string CurrentDialogId { get; private set; }
    public bool IsInDialogMode => _inDialogMode;
    public bool IsBlockingNarration => _isBlockingNarration;

    public event System.Action<ZoneData> OnZoneDisplayed;


    private bool _inDialogMode;
    private bool _isBlockingNarration;
    private bool _waitingForContinue;
    private string _pendingNarration;
    private Coroutine _zoneDisplayRoutine;
    private int _zoneVersion;

    private bool _pendingTakeover;
    public bool HasPendingTakeover => _pendingTakeover;
    public void FlagPendingTakeover() { _pendingTakeover = true; }
    public void ClearPendingTakeover() { _pendingTakeover = false; }

    private bool _takeoverPreFaded;
    private Coroutine _takeoverFadeRoutine;

    private bool _firstClickArmed;
    private string _armedZoneId;
    private int _armedAtFrame;

    private bool _pendingActionsRefresh;




    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (narrationContinueButton != null)
        {
            narrationContinueButton.gameObject.SetActive(false);
            narrationContinueButton.onClick.AddListener(() => _waitingForContinue = false);
        }
    }

    private void OnEnable()
    {
        EventBus.OnTrigger += OnEvent;

        var svc = ActionTimerService.Instance;
        if (svc != null)
            svc.OnCompleted.AddListener(HandleTimedActionCompleted);
    }

    private void Start()
    {
        // In case ActionTimerService woke up after us
        var svc = ActionTimerService.Instance;
        if (svc != null)
            svc.OnCompleted.AddListener(HandleTimedActionCompleted);
    }
    private void OnDisable()
    {
        EventBus.OnTrigger -= OnEvent;

        var svc = ActionTimerService.Instance;
        if (svc != null)
            svc.OnCompleted.RemoveListener(HandleTimedActionCompleted);
    }

    private void HandleTimedActionCompleted(string timedId)
    {
        // Don’t stomp narration or dialog
        if (_isBlockingNarration || _inDialogMode) return;

        // Rebuild interactables/actions; your existing method is perfect
        RefreshCurrentZone();
    }

    private void OnEvent(string trig, object payload)
    {
        if (trig == "ui.actions.refresh")
        {
            if (_isBlockingNarration || _inDialogMode)
            {
                _pendingActionsRefresh = true; // queue it
            }
            else
            {
                RefreshCurrentZone();
            }
        }
        // your existing inventory.changed case is great too
    }


    // ------------------------------------------------------------------
    // Narration helpers
    // ------------------------------------------------------------------

    public void ArmFirstClickTrigger(string zoneId)
    {
        _firstClickArmed = true;
        _armedZoneId = zoneId;
        _armedAtFrame = Time.frameCount;

        WorldState.SetFlag($"FirstClickArmed_{zoneId}", true);  // <-- add this
    }

    public bool TryConsumeFirstClickTrigger(out string zoneId)
    {
        if (_firstClickArmed &&
            _armedZoneId == WorldState.CurrentZoneId &&
            Time.frameCount > _armedAtFrame)
        {
            _firstClickArmed = false;
            zoneId = _armedZoneId;
            _armedZoneId = null;

            return true;
        }
        zoneId = null;
        return false;
    }

    public void ShowNarration(string body, bool blocking = false)
    {
        StartCoroutine(FadeNarration(body, blocking));
    }

    public void SetInteractablesTitle(string text)
    {
        if (interactablesTitle != null) interactablesTitle.text = text;
    }
    private IEnumerator FadeNarration(string body, bool blocking)
    {
        yield return FadeDescriptionOut();

        SetPortraitToNarrator();
        _pendingNarration = $"<b>Narrator:</b> <i>{body}</i>";
        zoneDescriptionText.text = _pendingNarration;

        if (narrationContinueButton) narrationContinueButton.gameObject.SetActive(blocking);
        _waitingForContinue = blocking;

        yield return FadeDescriptionIn();
    }
    public void BeginEventTakeoverFade()
    {
        if (_takeoverPreFaded) return;
        _takeoverPreFaded = true;

        // ensure no previous fade is running
        if (_takeoverFadeRoutine != null) StopCoroutine(_takeoverFadeRoutine);
        _takeoverFadeRoutine = StartCoroutine(FadeGroupsOut(descriptionGroup, actionsGroup, fadeDuration, 0.05f));
    }

    public void ClearEventTakeoverFade()
    {
        _takeoverPreFaded = false;
        _takeoverFadeRoutine = null;
    }

    public IEnumerator ShowNarrationAndWait(string body, bool autoReturn = true)
    {
        _isBlockingNarration = true;

        if (!_takeoverPreFaded) yield return FadeGroupsOut(descriptionGroup, null, fadeDuration);
        else if (_takeoverFadeRoutine != null) { yield return _takeoverFadeRoutine; _takeoverFadeRoutine = null; }

        SetPortraitToNarrator();
        _pendingNarration = $"<b>Narrator:</b> <i>{body}</i>";
        zoneDescriptionText.text = _pendingNarration;
        if (narrationContinueButton) narrationContinueButton.gameObject.SetActive(true);

        yield return FadeGroupsIn(descriptionGroup, null, fadeDuration);

        _waitingForContinue = true;
        while (_waitingForContinue) yield return null;

        if (narrationContinueButton) narrationContinueButton.gameObject.SetActive(false);
        _pendingNarration = null;
        _isBlockingNarration = false;

        ClearEventTakeoverFade();

        if (autoReturn)
        {
            var gm = GameManager.Instance;
            var zone = gm != null ? gm.GetZoneByID(WorldState.CurrentZoneId) : null;
            if (zone != null) DisplayZone(zone);
            else Debug.LogWarning("[ZoneUI] ShowNarrationAndWait: Current zone not found after narration.");
        }
    }



    public void PrepareZoneTransition()
    {
        // Let the spawner own the cleanup of spawned content
        elementSpawner?.ClearAll();

        if (actionPanel) actionPanel.gameObject.SetActive(false);
        if (interactablesHolders) interactablesHolders.gameObject.SetActive(false);
    }

    private IEnumerator FadeDescriptionOut()
    {
        if (!descriptionGroup) yield break;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            descriptionGroup.alpha = 1f - Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        descriptionGroup.alpha = 0f;
    }

    private IEnumerator FadeDescriptionIn()
    {
        if (!descriptionGroup) yield break;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            descriptionGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        descriptionGroup.alpha = 1f;
    }

    private IEnumerator FadeGroupsOut(CanvasGroup a, CanvasGroup b, float duration, float bDelay = 0f)
    {
        if (!a && !b) yield break;
        if (a) { a.interactable = false; a.blocksRaycasts = false; }
        if (b) { b.interactable = false; b.blocksRaycasts = false; }

        float t = 0f, total = Mathf.Max(duration, duration + bDelay);
        while (t < total)
        {
            t += Time.unscaledDeltaTime;
            float na = Mathf.Clamp01(t / duration);
            float nb = Mathf.Clamp01((t - bDelay) / duration);
            if (a) a.alpha = 1f - na;
            if (b) b.alpha = 1f - nb;
            yield return null;
        }
        if (a) a.alpha = 0f;
        if (b) b.alpha = 0f;
    }

    private IEnumerator FadeGroupsIn(CanvasGroup a, CanvasGroup b, float duration, float bDelay = 0f)
    {
        if (!a && !b) yield break;
        float t = 0f, total = Mathf.Max(duration, duration + bDelay);
        while (t < total)
        {
            t += Time.unscaledDeltaTime;
            float na = Mathf.Clamp01(t / duration);
            float nb = Mathf.Clamp01((t - bDelay) / duration);
            if (a) a.alpha = na;
            if (b) b.alpha = nb;
            yield return null;
        }
        if (a) { a.alpha = 1f; a.interactable = true; a.blocksRaycasts = true; }
        if (b) { b.alpha = 1f; b.interactable = true; b.blocksRaycasts = true; }
    }

    private void SetPortraitToNarrator()
    {
        if (!defaultNarratorPortrait) return;
        if (npcPortraitImage) npcPortraitImage.sprite = defaultNarratorPortrait;
        if (npcPortraitImage2) npcPortraitImage2.sprite = defaultNarratorPortrait;
    }


    public void SetDescriptionLine(string body, bool asNarratorFormat = true, bool persist = false)
    {
        if (string.IsNullOrEmpty(body) || zoneDescriptionText == null)
        {
            // allow explicit clear when persist == false
            if (!persist) _pendingNarration = null;
            return;
        }

        if (_isBlockingNarration || _inDialogMode) return;

        zoneDescriptionText.text = asNarratorFormat
            ? $"<b>Narrator:</b> <i>{body}</i>"
            : body;

        if (persist)
            _pendingNarration = zoneDescriptionText.text;
        else
            _pendingNarration = null; // <-- add this line
    }


    // ------------------------------------------------------------------
    // Zone / Dialog entry
    // ------------------------------------------------------------------

    public void DisplayZoneHeaderOnly(ZoneData zone)
    {
        // Don't flip _inDialogMode here; we only show header
        if (zone == null)
        {
            SetPortraitToNarrator();
            if (zoneNameText) zoneNameText.text = "Unknown Zone";
            if (zoneDescriptionText) zoneDescriptionText.text = "";
            return;
        }

        // Hide body panels so no buttons spawn yet
        if (actionPanel) actionPanel.gameObject.SetActive(false);
        if (interactablesHolders) interactablesHolders.gameObject.SetActive(false);

        // Clear spawned content
        elementSpawner?.ClearAll();

        // Header bits
        SetPortraitToNarrator();
        LastZoneEntered = zone;
        if (zoneNameText) zoneNameText.text = zone.displayName;
        if (zoneDescriptionText) zoneDescriptionText.text = $"<b>Narrator:</b> <i>{zone.description}</i>";

        elementSpawner.SetBGImage(zone);
    }
    public void DisplayZone(ZoneData zone, bool suppressHeader = false)
    {
        _inDialogMode = false;
        if (_zoneDisplayRoutine != null) StopCoroutine(_zoneDisplayRoutine);

        _zoneVersion++;
        _zoneDisplayRoutine = StartCoroutine(DisplayZoneCo(zone, _zoneVersion, suppressHeader));
    }

    private IEnumerator DisplayZoneCo(ZoneData zone, int versionToken, bool suppressHeader)
    {
        yield return FadeGroupsOut(descriptionGroup, actionsGroup, fadeDuration, 0.05f);

        elementSpawner?.ClearAll();

        GameManager.Instance.clearMiddlePanels();
        GameManager.Instance.clearRightPanels();

        if (actionPanel) actionPanel.gameObject.SetActive(true);
        if (interactablesHolders) interactablesHolders.gameObject.SetActive(true);
        if (interactablesTitle) interactablesTitle.text = "Interactables";

        if (zone == null)
        {
            SetPortraitToNarrator();
            if (zoneNameText) zoneNameText.text = "Unknown Zone";
            if (zoneDescriptionText) zoneDescriptionText.text = "";
        }
        else
        {
            LastZoneEntered = zone;

            SetPortraitToNarrator();
            if (!suppressHeader && zoneNameText) zoneNameText.text = zone.displayName;
            if (zoneDescriptionText) zoneDescriptionText.text = $"<b>Narrator:</b> <i>{zone.description}</i>";

            // Delegate all spawning (actions + NPC + world) to the spawner
            elementSpawner?.BuildForZone(zone);
        }

        yield return FadeGroupsIn(descriptionGroup, actionsGroup, fadeDuration, 0.05f);

        if (!string.IsNullOrEmpty(_pendingNarration) && zoneDescriptionText)
            zoneDescriptionText.text = _pendingNarration;

        _zoneDisplayRoutine = null;
        if (versionToken == _zoneVersion) OnZoneDisplayed?.Invoke(zone);
    }

    public void DisplayDialogNode(DialogNode node)
    {
        Debug.Log($"[ZoneUI] >>> DisplayDialogNode('{node?.dialogId}')");

        _isBlockingNarration = true;
        Debug.Log($"[ZoneUI] _isBlockingNarration = {_isBlockingNarration}");

        if (_zoneDisplayRoutine != null) StopCoroutine(_zoneDisplayRoutine);

        _zoneDisplayRoutine = StartCoroutine(DisplayDialogNodeCo(node, ++_zoneVersion));
    }


    private IEnumerator DisplayDialogNodeCo(DialogNode node, int versionToken)
    {
        // Fade out only the actions panel, leave the description visible
        if (!_takeoverPreFaded)
        {
            yield return FadeGroupsOut(actionsGroup, null, fadeDuration);
        }
        else
        {
            // make sure the pre-fade has actually finished
            if (_takeoverFadeRoutine != null)
            {
                yield return _takeoverFadeRoutine;
                _takeoverFadeRoutine = null;
            }
        }

        if (actionPanel) actionPanel.gameObject.SetActive(true);      // <-- ADD

        // Clear whatever zone content was there
        elementSpawner?.ClearAll();

        // Render dialog content (portraits/options) into the same parent
        dialogUI?.RenderNode(node);

        // >>> NEW: update the NPC Info panel for the current zone
        var gm = GameManager.Instance;
        var zoneId = WorldState.CurrentZoneId;
        var zone = gm != null ? gm.GetZoneByID(node.npcId) : null;
        if (zone != null)
        {
            gm.SetupNPCInfo_Panel(zone);
        }

        // If we pre-faded BOTH groups, bring BOTH back in; otherwise only actions
        if (_takeoverPreFaded)
            yield return FadeGroupsIn(descriptionGroup, actionsGroup, fadeDuration, 0.05f);
        else
            yield return FadeGroupsIn(actionsGroup, null, fadeDuration);

        ClearEventTakeoverFade();
        ClearPendingTakeover();


        _zoneDisplayRoutine = null;

    }

    public void RefreshCurrentZone()
    {
        if (LastZoneEntered == null || _isBlockingNarration || _inDialogMode) return;
        PrepareZoneTransition();
        DisplayZone(LastZoneEntered);
        _pendingActionsRefresh = false;
    }

    public void RefreshCurrentDialog()
    {
        if (string.IsNullOrEmpty(CurrentDialogId)) return;
        var node = GameManager.Instance.GetDialogById(CurrentDialogId);
        if (node != null) DisplayDialogNode(node);
    }
}
