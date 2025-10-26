using UnityEngine;
using TMPro;

public class Attributes_UI : MonoBehaviour
{
    [Header("Text fields")]
    [SerializeField] private TMP_Text healthText;        // e.g. "58/60"
    [SerializeField] private TMP_Text attackText;        // e.g. "7"
    [SerializeField] private TMP_Text defenceText;       // e.g. "2 (+1)"
    [SerializeField] private TMP_Text aetherText;        // e.g. "5/5"
    [SerializeField] private TMP_Text hitchanceText;     // e.g. "78%"
    [SerializeField] private TMP_Text critChanceText;    // e.g. "9%"

    private bool subscribedToStats;
    private bool subscribedToStance;
    public static Attributes_UI Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
        Refresh(); // draw once immediately
    }

    private System.Collections.IEnumerator SubscribeWhenReady()
    {
        // wait for PlayerStats to exist
        while (PlayerStats.Instance == null)
            yield return null;

        if (!subscribedToStats)
        {
            PlayerStats.Instance.OnStatsChanged += Refresh;
            PlayerStats.Instance.OnVitalsChanged += Refresh;
            subscribedToStats = true;
        }

        // wait for PlayerStance to exist (it may spawn later than PlayerStats)
        while (PlayerStance.Instance == null)
            yield return null;

        if (!subscribedToStance)
        {
            PlayerStance.OnStanceChanged += Refresh;
            subscribedToStance = true;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (PlayerStats.Instance != null && subscribedToStats)
        {
            PlayerStats.Instance.OnStatsChanged -= Refresh;
            PlayerStats.Instance.OnVitalsChanged -= Refresh;
        }
        subscribedToStats = false;

        if (PlayerStance.Instance != null && subscribedToStance)
        {
            PlayerStance.OnStanceChanged -= Refresh;
        }
        subscribedToStance = false;
    }

    public void Refresh()
    {
        var ps = PlayerStats.Instance;
        if (ps == null) return;

        // grab stance if available
        StanceType stance = PlayerStance.Instance
            ? PlayerStance.Instance.currentStance
            : StanceType.None;

        // ---- HEALTH ----
        if (healthText)
            healthText.text = $"{ps.CurrentHP}";

        // ---- ATTACK / DAMAGE PREVIEW ----
        // "your swing" with stance applied
        if (attackText)
        {
            float baseSwing = ps.WeaponDamage + ps.StrengthBonusDamage;
            float stanceMul = 1f + CombatCalculator.GetStanceDamageBonus(stance);
            float shownSwing = baseSwing * stanceMul;

            attackText.text = Mathf.RoundToInt(shownSwing).ToString();
        }

        // ---- DEFENCE / BLOCK (STANCE AFFECTED) ----
        if (defenceText)
        {
            int baseArmor = Mathf.RoundToInt(ps.Armor);

            int effectiveArmor = CombatCalculator_GetEffectiveArmorPreview(stance, baseArmor);

            int delta = effectiveArmor - baseArmor;

            if (delta > 0)
                defenceText.text = $"{effectiveArmor} (+" + delta + ")";
            else if (delta < 0)
                defenceText.text = $"{effectiveArmor} (" + delta + ")";
            else
                defenceText.text = $"{effectiveArmor}";
        }

        // ---- AETHER / MANA ----
        if (aetherText)
            aetherText.text = $"{ps.CurrentMana}/{ps.MaxMana}";

        // ---- HIT CHANCE / CRIT CHANCE ----
        // This uses the most recent resolved swing numbers from CombatManager.
        if (CombatManager.Instance != null)
        {
            // player's chance to hit enemy and crit enemy last swing
            float hit = CombatManager.Instance.PlayerHitChance * 100f;
            float crit = CombatManager.Instance.PlayerCritChance * 100f;

            if (hitchanceText)
                hitchanceText.text = $"{hit:F0}%";

            if (critChanceText)
                critChanceText.text = $"{crit:F0}%";
        }
        else
        {
            // fallback when not in combat yet
            if (hitchanceText)
                hitchanceText.text = "--";

            if (critChanceText)
                critChanceText.text = "--";
        }
    }

    // same preview logic we talked about for stance-modified armor
    private int CombatCalculator_GetEffectiveArmorPreview(StanceType stance, int baseArmor)
    {
        // Defensive: +5% of base armor (min +1)
        if (stance == StanceType.Defensive)
        {
            int bonus = Mathf.RoundToInt(baseArmor * 0.05f);
            if (bonus < 1) bonus = 1;
            return baseArmor + bonus;
        }

        // Berserker: -5% of base armor (min -1), but never below 1 if you had any armor
        if (stance == StanceType.Berserker)
        {
            int penalty = Mathf.RoundToInt(baseArmor * 0.05f);
            if (penalty < 1) penalty = 1;

            int eff = baseArmor - penalty;

            if (baseArmor > 0 && eff < 1)
                eff = 1;

            if (eff < 0)
                eff = 0;

            return eff;
        }

        // Precision / None: unchanged
        return baseArmor;
    }
}
