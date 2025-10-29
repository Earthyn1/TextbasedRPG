using System.Collections;
using UnityEngine;
using TMPro;

public class RespawnUI : MonoBehaviour
{
    public static RespawnUI Instance { get; private set; }

    [Header("UI Refs")]
    [SerializeField] private GameObject rootPanel;      // the overlay panel object
    [SerializeField] private TMP_Text defeatText;       // "You were defeated..."
    [SerializeField] private float respawnDelay = 2f;   // seconds before we send you back

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    // Called by CombatManager when the player dies
    public void BeginRespawnSequence()
    {
        if (rootPanel != null)
            rootPanel.SetActive(true);

        if (defeatText != null)
            defeatText.text = "You were defeated...";

        // start the flow back to the overworld
        StartCoroutine(RespawnFlow());
    }

    private IEnumerator RespawnFlow()
    {
        // little dramatic pause
        yield return new WaitForSeconds(respawnDelay);

        // 1. Restore player vitals
        var ps = PlayerStats.Instance;
        if (ps != null)
            ps.RestoreHalfVitals();

        // 2. Figure out what zone we're returning to
        var gm = GameManager.Instance;
        ZoneData zone = null;
        if (gm != null)
            zone = gm.GetZoneByID(WorldState.CurrentZoneId);

        Debug.Log($"[Respawn] Returning to zone {zone?.id ?? "NULL"}");

        // 3. Transition UI back to the world / last zone
        ZoneUIManager.Instance?.PrepareZoneTransition();

        if (zone != null)
        {
            ZoneUIManager.Instance?.DisplayZone(zone);

            GameLog_Manager.Instance?.AddEntry(
                "You come to... weak, aching.",
                "#FFAACC"
            );
        }
        else
        {
            Debug.LogWarning("[Respawn] No zone found via WorldState.CurrentZoneId. Falling back to LastZoneEntered.");

            ZoneUIManager.Instance?.DisplayZone(ZoneUIManager.Instance.LastZoneEntered);
        }

        // 4. Hide this overlay again
        if (rootPanel != null)
            rootPanel.SetActive(false);
    }
}
