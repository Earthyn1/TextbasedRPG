using UnityEngine;
using UnityEngine.UI;

public class ZoneElementSpawner : MonoBehaviour
{
    [Header("Parents/Prefabs")]
    [SerializeField] Transform actionsParent;
    [SerializeField] GameObject actionButtonPrefab;

    [SerializeField] Transform npcParent;
    [SerializeField] GameObject npcButtonPrefab;

    [SerializeField] Transform worldParent;
    [SerializeField] GameObject worldButtonPrefab;

    [SerializeField] Image BGImage;

    public void SetBGImage(ZoneData zone)
    {
        if (BGImage == null) { Debug.LogWarning("[ZoneElementSpawner] BGImage not assigned."); return; }
        BGImage.sprite = Resources.Load<Sprite>($"BG/{zone.bgImage}");
    }

    public void BuildForZone(ZoneData zone)
    {
        ClearAll();
        if (zone == null) return;

        BuildActions(zone);
        BuildNpc(zone);
        BuildWorld(zone);

        if (BGImage != null)
            BGImage.sprite = Resources.Load<Sprite>($"BG/{zone.bgImage}");
    }

    void BuildActions(ZoneData zone)
    {
        if (!actionsParent || !actionButtonPrefab)
        {
            Debug.LogWarning("[ZoneElementSpawner] Missing actionsParent or actionButtonPrefab");
            return;
        }
        if (zone.actions == null) return;

        foreach (var a in zone.actions)
        {
            if (!ShouldShowAction(a)) continue;

            // --- Timed one-and-done guard (skip spawning entirely) ---
            if (a.type == ActionType.Timed)
            {
                string timedId = a.target; // your schema: TimedActionDef.id lives in ActionData.target
                if (a.hideOnSuccess && GameManager.Instance.IsTimedActionDone(timedId))
                    continue;
            }

            var go = Instantiate(actionButtonPrefab, actionsParent);
            var btn = go.GetComponent<Action_Button>();
            btn.SetupButton(a);
           

            // soft gate for timed
            var timer = go.GetComponent<ActionTimerUI>();
            timer?.ConfigureSoftGate(a.enableWhen, a.lockedMessage);

            // --- "already done" UX for repeatables (not hidden after done) ---
            if (a.type == ActionType.Timed && !a.hideOnSuccess && GameManager.Instance.IsTimedActionDone(a.target))
            {
                // prevent starting again and show subtle label
                timer?.ClearActionId();
                if (btn?.text) btn.text.text = $"{a.name} <color=#888888>(already done)</color>";
                // optional: also disable the button if you prefer
                // var uiButton = go.GetComponent<UnityEngine.UI.Button>();
                // if (uiButton) uiButton.interactable = false;
            }
        }

        bool ShouldShowAction(ActionData a)
        {
            if (a == null) return false;

            // hide if one-and-done and already completed
            if (a.type == ActionType.Timed && a.hideOnSuccess && WorldState.GetFlag($"ACT_DONE_{a.target}"))
                return false;

            if (a.visibleWhen != null)
                foreach (var cond in a.visibleWhen)
                    if (!RequirementEvaluator.EvaluateAll(cond)) return false;

            return true;
        }
    }

    void BuildNpc(ZoneData zone)
    {
        if (!npcParent || !npcButtonPrefab)
        {
            Debug.LogWarning("[ZoneElementSpawner] Missing npcParent or npcButtonPrefab");
            return;
        }
        if (zone.npcs == null) return;

        foreach (var i in zone.npcs)
        {
            if (!IsVisible("VISIBLE_" + i.id)) continue;

            var go = Instantiate(npcButtonPrefab, npcParent);
            var btn = go.GetComponent<Interactables_Button>();
            btn.NPCSetupButton(i);
        }
    }

    void BuildWorld(ZoneData zone)
    {
        if (!worldParent || !worldButtonPrefab)
        {
            Debug.LogWarning("[ZoneElementSpawner] Missing worldParent or worldButtonPrefab");
            return;
        }
        if (zone.worldObjects == null) return;

        foreach (var i in zone.worldObjects)
        {
            if (!IsVisible("VISIBLE_" + i.id)) continue;

            var go = Instantiate(worldButtonPrefab, worldParent);
            var btn = go.GetComponent<Interactables_Button>();
            btn.WorldSetupButton(i);
        }
    }

    bool IsVisible(string key) => WorldState.GetFlag(key) || !WorldState.HasFlag(key);

    public void ClearAll()
    {
        Clear(actionsParent);
        Clear(npcParent);
        Clear(worldParent);

        static void Clear(Transform parent)
        {
            if (!parent) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }
    }
}
