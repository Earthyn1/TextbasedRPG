using UnityEngine;

public class CombatUI : MonoBehaviour
{
    [Header("Player Bars")]
    public ProgressBar PlayerHPBar;
    public ProgressBar PlayerSpeedBar;
    // public ProgressBar PlayerMPBar; // TODO: hook up when mana is ready

    private void OnEnable()
    {
        var cm = CombatManager.Instance;
        if (cm != null)
            cm.OnTimersChanged += HandleTimers;

        var ps = PlayerStats.Instance;
        if (ps != null)
            ps.OnVitalsChanged += UpdatePlayerBars;
    }

    private void OnDisable()
    {
        var cm = CombatManager.Instance;
        if (cm != null)
            cm.OnTimersChanged -= HandleTimers;

        var ps = PlayerStats.Instance;
        if (ps != null)
            ps.OnVitalsChanged -= UpdatePlayerBars;
    }

    public void InitialSetup(string enemyID)
    {
        var ps = PlayerStats.Instance;
        if (ps == null) return;

        PlayerHPBar?.SetProgress((float)ps.CurrentHP / ps.MaxHP, $"{ps.CurrentHP}/{ps.MaxHP}");
    }

    // ── Event Handlers ────────────────────────────────────────────────────────

    private void HandleTimers(float playerNorm, float enemyNorm)
    {
        // playerNorm: 1 = just attacked, 0 = ready to attack
        // Invert so the bar fills toward full as the swing winds up
        PlayerSpeedBar?.SetProgress(1f - playerNorm, "");
    }

    private void UpdatePlayerBars()
    {
        var ps = PlayerStats.Instance;
        if (ps == null) return;

        PlayerHPBar?.SetProgress((float)ps.CurrentHP / ps.MaxHP, $"{ps.CurrentHP}/{ps.MaxHP}");
    }
}
