#if UNITY_EDITOR
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#endif

public class EventsDataLoader : MonoBehaviour
{
    [SerializeField] private TextAsset eventsJsonFile;

    [System.Serializable]
    public class GameEventDefArray
    {
        public GameEventDef[] items;
    }

    private IEnumerator Start()
    {
        // wait until EventManager singleton is ready
        while (EventManager.I == null) yield return null;
        LoadEvents();
    }

    private void LoadEvents()
    {
        if (eventsJsonFile != null)
        {
            var defs = JsonConvert.DeserializeObject<List<GameEventDef>>(eventsJsonFile.text);
            EventManager.I.LoadEvents(defs);
            Debug.Log($"[EventsDataLoader] Loaded {defs.Count} events");
        }
    }

    // --- Tiny Debug Helpers ---
    [ContextMenu("Debug: Fire OnEnterZone(castleCourtyard)")]
    private void DebugFireEnterCourtyard()
    {
        EventBus.Fire("OnEnterZone", "castleCourtyard");
        Debug.Log("[EventsDataLoader] Fired OnEnterZone(castleCourtyard)");
    }

    [ContextMenu("Debug: Fire OnQuestStageChanged(GoblinTrouble)")]
    private void DebugFireQuestStage()
    {
        WorldState.SetQuestStage("GoblinTrouble", 2);   // <-- set the stage

        EventBus.Fire("OnQuestStageChanged(GoblinTrouble)", 2);
        Debug.Log("[EventsDataLoader] Fired OnQuestStageChanged(GoblinTrouble) with stage 2");
    }
}
