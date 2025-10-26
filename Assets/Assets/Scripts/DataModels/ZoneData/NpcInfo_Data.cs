using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NpcInfo_Data : MonoBehaviour
{
    [Header("UI References")]
    public Image image;
    public GameObject NPCStats;
    public GameObject Loot_Holder;

    [Header("Stat Fields")]
    public TMP_Text level;
    public TMP_Text health;
    public TMP_Text maxHit;
    public TMP_Text defence;
    public TMP_Text description;
    public TMP_Text hitChance;
    public TMP_Text critChance;
    public TMP_Text aether;

    // we cache which NPC/zone we’re currently showing so we can redraw on stat changes
    private ZoneData _cachedZoneData;
    private NPCData _cachedNpcData;

    private void OnEnable()
    {
        // subscribe to player stat changes if player exists
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged += HandlePlayerStatsChanged;
            PlayerStats.Instance.OnVitalsChanged += HandlePlayerStatsChanged; // optional: if you ever
                                                                              // want HP display etc.
        }
    }

    private void OnDisable()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged -= HandlePlayerStatsChanged;
            PlayerStats.Instance.OnVitalsChanged -= HandlePlayerStatsChanged;
        }
    }

    private void HandlePlayerStatsChanged()
    {
        // only update if we actually have something on screen already
        if (_cachedZoneData != null && _cachedNpcData != null)
        {
            RedrawStatsSection();
        }
    }

    // Public entry point: call this when showing the NPC info panel
    public void SetupInfo(ZoneData zoneData)
    {
        _cachedZoneData = zoneData;
        _cachedNpcData = NPCData_Manager.Instance.GetNPCS(zoneData.id);

        if (_cachedNpcData == null)
            return;

        // Title
        GameManager.Instance.zoneUIManager.SetInteractablesTitle(_cachedNpcData.displayName);

        // Portrait
        Sprite sprite = Resources.Load<Sprite>($"Portraits/{zoneData.portrait}");
        image.sprite = sprite != null
            ? sprite
            : Resources.Load<Sprite>("Portraits/MagnifyingGlass");

        // Description
        description.text = _cachedNpcData.description;

        // Loot table visibility
        Loot_Holder.SetActive(!string.IsNullOrEmpty(_cachedNpcData.lootTable));

        // Fill numeric stats
        RedrawStatsSection();
    }

    private void RedrawStatsSection()
    {
        if (_cachedNpcData == null)
        {
            NPCStats.SetActive(false);
            return;
        }

        // If this NPC is actually something you can fight
        if (_cachedNpcData.damage > 0)
        {
            // Raw enemy stats
            level.text = $"Level: {_cachedNpcData.level}";
            health.text = _cachedNpcData.maxHP.ToString();
            maxHit.text = _cachedNpcData.damage.ToString();
            defence.text = _cachedNpcData.block.ToString();
            critChance.text = $"{(_cachedNpcData.critChance * 100f):0}%";

            // Aether (if you don't have this yet in data, default to 0)
            aether.text = "0";

            // Effective hit chance vs THIS player right now.
            // This is where Fortitude matters.
            float shownHitChancePct = CalculateEffectiveHitChancePctForPlayer(_cachedNpcData);
            hitChance.text = $"{shownHitChancePct:0}%";

            NPCStats.SetActive(true);
        }
        else
        {
            NPCStats.SetActive(false);
        }
    }

    private float CalculateEffectiveHitChancePctForPlayer(NPCData npc)
    {
        // Get the player's evasion bonus from Fortitude
        PlayerStats ps = PlayerStats.Instance;
        if (ps == null)
        {
            // fallback to raw hitChance if we somehow don't have player yet
            return npc.hitChance * 100f;
        }

        float playerEvasionBonus = CombatCalculator.GetPlayerEvasionBonusFromFortitude(ps);

        float effectiveHit01 = Mathf.Clamp01(
            npc.hitChance - playerEvasionBonus
        );

        return (effectiveHit01 * 100f);
    }
}
