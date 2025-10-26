using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EquippableData;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    // ---- FORMULA KNOBS ----
    [Header("Derived formulas")]
    [SerializeField] private int baseHP = 20;
    [SerializeField] private int baseMana = 0;
    [SerializeField] private float baseAttackSpeed = 2f;

    [Header("Tuning - per level scaling")]
    [SerializeField] private int hpPerEndurance = 10;           
    [SerializeField] private int manaPerAether = 5;              
    [SerializeField] private float strengthDamagePerLevel = 1f;     
    [SerializeField] private float armorPerDefLevel = 1f;          
    [SerializeField] private float hitChancePerPrecision = 0.02f;   //  (+2% per Precision)
    [SerializeField] private float critPerPrecisionStep = 0.01f;    //  (+1% per 5 Prec)

    public int Armor { get; private set; }
    public int WeaponDamage { get; private set; }
    public float StrengthBonusDamage { get; private set; }
    public float HitChanceBonus { get; private set; }
    public float CritChanceFinal { get; private set; }


    [SerializeField] private float attackFromStrength = 1f;
    [SerializeField] private float attackFromPower = 1f;
    [SerializeField] private float defenceFromDef = 1f;
    [SerializeField] private float defenceFromBlock = 0.5f;

    // ---- Primary stats (final after equip/skills) ----
    public int strength, defence, fortitude, precision, aether;
    public int attackPower, blockPower, elementalAether;
    public float attackSpeed;
    public float critChance;      // expressed as decimal (0.1f = 10%)
    public float critMultiplier;  // multiplier on crit (e.g., 1.5f = 150%)
    public float GearDodgeChance { get; private set; }


    // ---- Derived (read-only) ----
    public int MaxHP { get; private set; }
    public int MaxMana { get; private set; }

    // ---- Vitals (runtime, save/restore) ----
    public int CurrentHP { get; private set; }
    public int CurrentMana { get; private set; }

    // ---- Events ----
    public event Action OnStatsChanged;   // structure changed (max values, ratings, etc.)
    public event Action OnVitalsChanged;  // current HP/Mana changed
    public event Action OnDeath;       // fired once per death (transition >0 -> 0)
    public event Action OnOutOfMana;   // fired on failed spend OR when mana reaches 0 from >0
    public event System.Action OnPlayerDied;

    // Internal guard so OnDeath doesn't spam
    private bool _isDead = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable() { StartCoroutine(SubscribeWhenReady()); }
    private void OnDisable()
    {
        if (PlayerSkills.Instance != null)
            PlayerSkills.Instance.OnSkillLevelChanged -= HandleSkillChanged;
    }

    private void Start()
    {
        // Initial compute at scene start
        RecalculateFromEquipment(EquipmentManager.Instance != null
            ? EquipmentManager.Instance.GetAllEquipped()
            : Array.Empty<Item_Data>());
        // If first time, start full
        if (CurrentHP <= 0 || CurrentHP > MaxHP) SetHPToMax();
        if (CurrentMana < 0 || CurrentMana > MaxMana) SetManaToMax();
    }

    private IEnumerator SubscribeWhenReady()
    {
        while (PlayerSkills.Instance == null) yield return null;
        PlayerSkills.Instance.OnSkillLevelChanged += HandleSkillChanged;
        RecalcNow();
    }

    private void HandleSkillChanged(Enum_Skills skill, int newLevel) => RecalcNow();

    private void RecalcNow()
    {
        var equipped = (EquipmentManager.Instance != null)
            ? EquipmentManager.Instance.GetAllEquipped()
            : Array.Empty<Item_Data>();
        RecalculateFromEquipment(equipped);
    }

    [ContextMenu("Log Naked Stats")]
    public void LogNakedStats()
    {
        Debug.Log($"Strength: {strength}, Defence: {defence}, Endurance: {fortitude}, Precision: {precision}, Aether: {aether}");
        Debug.Log($"MaxHP: {MaxHP}, MaxMana: {MaxMana}");
        Debug.Log($"BlockPower: {blockPower}, AttackPower: {attackPower}, AttackSpeed: {attackSpeed}, ElementalAether: {elementalAether}");
        Debug.Log($"CritChance: {critChance}, CritMultiplier: {critMultiplier}");
    }

    // === Public ===
    public void RecalculateFromEquipment(IEnumerable<Item_Data> equippedItems)
    {
        // Cache percentages so we don't instantly heal/hurt the player when max changes
        float hpPct = MaxHP > 0 ? (float)CurrentHP / MaxHP : 1f;
        float mpPct = MaxMana > 0 ? (float)CurrentMana / MaxMana : 1f;

        // 1) BASE from skills (skills are your "naked stats")
        var psSkills = PlayerSkills.Instance;
        if (psSkills != null)
        {
            int lvl(Enum_Skills s) => psSkills.GetSkill(s)?.level ?? 1;
            strength = lvl(Enum_Skills.Strength);
            defence = lvl(Enum_Skills.Defence);
            fortitude = lvl(Enum_Skills.Fortitude);
            precision = lvl(Enum_Skills.Precision);
            aether = lvl(Enum_Skills.Aethur);
        }
        else
        {
            strength = defence = fortitude = precision = aether = 1;
        }

        // Reset gear-driven stats
        attackPower = 0;
        attackSpeed = baseAttackSpeed;
        blockPower = 0;
        elementalAether = 0;
        critChance = 0f;
        critMultiplier = 0f;
        GearDodgeChance = 0f;

        // 2) Apply equipment modifiers
        foreach (var it in equippedItems)
        {
            if (it == null) continue;
            if (!EquipmentManager.Instance.TryGetEquipRow(it.itemID, out var row)) continue;

            // --- Core stats ---
            strength += row.strength;
            defence += row.defence;
            fortitude += row.fortitude;
            precision += row.precision;
            aether += row.aether;

            // --- Combat impact ---
            if (row.damage > 0) WeaponDamage = row.damage;
            if (row.attackSpeed > 0f) attackSpeed = row.attackSpeed;   // ✅ replace, not add
            blockPower += row.block;
            elementalAether += row.elementalAether;

            critChance += row.critChance;
            critMultiplier = Mathf.Max(critMultiplier, row.critMultiplier);
            GearDodgeChance += row.dodgeChance;
        }

        // 3) Derived stats
        MaxHP = baseHP + (fortitude * hpPerEndurance);
        MaxMana = baseMana + (aether * manaPerAether);

        StrengthBonusDamage = strength * strengthDamagePerLevel;
        Armor = Mathf.RoundToInt(defence * armorPerDefLevel) + blockPower;

        HitChanceBonus = precision * hitChancePerPrecision;
        float bonusCritFromPrecision = Mathf.Floor(precision / 5f) * critPerPrecisionStep;
        CritChanceFinal = critChance + bonusCritFromPrecision;

        // 4) Preserve vitals %
        SetHP(Mathf.Clamp(Mathf.RoundToInt(hpPct * MaxHP), 0, MaxHP), silent: true);
        SetMana(Mathf.Clamp(Mathf.RoundToInt(mpPct * MaxMana), 0, MaxMana), silent: true);

        // 5) Fire events
        OnStatsChanged?.Invoke();
        OnVitalsChanged?.Invoke();
    }




    // === Vitals API ===
    public void SetHPToMax(bool silent = false) => SetHP(MaxHP, silent);
    public void SetManaToMax(bool silent = false) => SetMana(MaxMana, silent);

    public void ApplyDamage(int amount)
    {
        if (amount <= 0) return;
        SetHP(Mathf.Max(0, CurrentHP - amount));

        if (CurrentHP <= 0)
            Die();
    }

    public void RestoreHalfVitals()
    {
        // round up so you don't respawn at 0 if MaxHP is 1, etc.
        int halfHP = Mathf.CeilToInt(MaxHP * 0.5f);
        int halfMP = Mathf.CeilToInt(MaxMana * 0.5f);

        CurrentHP = Mathf.Clamp(halfHP, 1, MaxHP);
        CurrentMana = Mathf.Clamp(halfMP, 0, MaxMana);

        OnVitalsChanged?.Invoke();
    }


    private void Die()
    {
        Debug.Log("💀 Player has fallen...");
        OnPlayerDied?.Invoke();

        // Optional: fade screen, pause combat, etc.
        CombatManager.Instance?.HandlePlayerDeath();
    }

    public void RestoreFullVitals()
    {
        CurrentHP = MaxHP;
        CurrentMana = MaxMana;
        OnVitalsChanged?.Invoke();
    }
    public int ApplyHealing(int amount)
    {
        if (amount <= 0) return 0;

        int prev = CurrentHP;
        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
        int healed = CurrentHP - prev;

        OnVitalsChanged?.Invoke();  // 🔥 instant HP bar update

        return healed;
    }

    public int ApplyMana(int manaAmount)
    {
        if (manaAmount <= 0) return 0;

        int before = CurrentMana;
        CurrentMana = Mathf.Min(MaxMana, CurrentMana + manaAmount);
        int restored = CurrentMana - before;

        OnVitalsChanged?.Invoke();  // 🔥 instant MP bar update

        return restored;
    }



    public bool CanSpendMana(int amount) => amount <= CurrentMana;
    public bool SpendMana(int amount)
    {
        if (amount <= 0) return true;
        if (CurrentMana < amount)
        {
            // Not enough mana: signal OOM
            OnOutOfMana?.Invoke();
            return false;
        }
        SetMana(CurrentMana - amount);
        return true;
    }

    public void RestoreMana(int amount)
    {
        if (amount <= 0) return;
        SetMana(Mathf.Min(MaxMana, CurrentMana + amount));
    }

    // Smooth regen tick example (optional: call from a coroutine / Update)
    public void RegenTick(int hpPerTick, int manaPerTick)
    {
        if (hpPerTick != 0) SetHP(Mathf.Min(MaxHP, CurrentHP + hpPerTick));
        if (manaPerTick != 0) SetMana(Mathf.Min(MaxMana, CurrentMana + manaPerTick));
    }

    /// <summary>Revive the player with a given HP (defaults to full). Resets death guard.</summary>
    public void Revive(int hp = -1)
    {
        _isDead = false;
        if (hp < 0) hp = MaxHP;
        SetHP(Mathf.Clamp(hp, 1, MaxHP));
    }


    // === Private setters (fire events once) ===
    private void SetHP(int newHP, bool silent = false)
    {
        newHP = Mathf.Clamp(newHP, 0, MaxHP);
        if (newHP == CurrentHP) return;

        int prev = CurrentHP;
        CurrentHP = newHP;

        if (!silent) OnVitalsChanged?.Invoke();

        // Fire OnDeath once when crossing to 0
        if (prev > 0 && CurrentHP == 0 && !_isDead)
        {
            _isDead = true;
            OnDeath?.Invoke();
        }
        // If you want to clear dead state when leaving 0 (e.g., heal from 0):
        if (prev == 0 && CurrentHP > 0) _isDead = false;
    }

    private void SetMana(int newMana, bool silent = false)
    {
        newMana = Mathf.Clamp(newMana, 0, MaxMana);
        if (newMana == CurrentMana) return;

        int prev = CurrentMana;
        CurrentMana = newMana;

        if (!silent) OnVitalsChanged?.Invoke();

        // Fire when crossing to exactly 0 from >0 (natural depletion)
        if (prev > 0 && CurrentMana == 0)
        {
            OnOutOfMana?.Invoke();
        }
    }
   
}
