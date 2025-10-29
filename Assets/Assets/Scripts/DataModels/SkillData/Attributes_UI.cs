using UnityEngine;
using TMPro;

public class Attributes_UI : MonoBehaviour
{
    [Header("Text fields")]
    [SerializeField] private TMP_Text healthCurrentText;   // e.g. "322"
    [SerializeField] private TMP_Text healthMaxText;       // e.g. "1530"

    [SerializeField] private TMP_Text attackText;          // e.g. "16"
    [SerializeField] private TMP_Text defenceText;         // e.g. "232"

    [SerializeField] private TMP_Text aetherCurrentText;   // e.g. "14"
    [SerializeField] private TMP_Text aetherMaxText;       // e.g. "165"

    [SerializeField] private TMP_Text hitchanceText;       // e.g. "45%"
    [SerializeField] private TMP_Text critChanceText;      // e.g. "47%"

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

        // ---- HEALTH (current / max) ----
        if (healthCurrentText)
            healthCurrentText.text = ps.CurrentHP.ToString();
        if (healthMaxText)
            healthMaxText.text = ps.MaxHP.ToString();

        // ---- ATTACK PREVIEW (stance applied) ----
        if (attackText)
        {
            float baseSwing = ps.WeaponDamage + ps.StrengthBonusDamage;
            float stanceMul = 1f + CombatCalculator.GetStanceDamageBonus(stance);
            float shownSwing = baseSwing * stanceMul;

            attackText.text = Mathf.RoundToInt(shownSwing).ToString();
        }

        // ---- DEFENCE / ARMOR PREVIEW (stance applied) ----
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

        // ---- AETHER / MANA (current / max) ----
        if (aetherCurrentText)
            aetherCurrentText.text = ps.CurrentMana.ToString();
        if (aetherMaxText)
            aetherMaxText.text = ps.MaxMana.ToString();

        // ---- HIT CHANCE / CRIT CHANCE ----
        if (CombatManager.Instance != null)
        {
            float hit = CombatManager.Instance.PlayerHitChance * 100f;
            float crit = CombatManager.Instance.PlayerCritChance * 100f;

            if (hitchanceText)
                hitchanceText.text = $"{hit:F0}%";

            if (critChanceText)
                critChanceText.text = $"{crit:F0}%";
        }
        else
        {
            if (hitchanceText)
                hitchanceText.text = "--";

            if (critChanceText)
                critChanceText.text = "--";
        }
    }

    // stance-modified armor preview logic
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
