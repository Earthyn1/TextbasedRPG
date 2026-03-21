using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EquippableData;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    // ---- FORMULA KNOBS ----
    [Header("Base Stats")]
    [SerializeField] private int baseHP   = 15;
    [SerializeField] private int baseArmor = 1;
    [SerializeField] private int baseMana = 0;
    [SerializeField] private float baseAttackSpeed = 2f;

    [Header("Tuning - per level scaling")]
    [SerializeField] private int    manaPerAether          = 5;
    [SerializeField] private float  strengthDamagePerLevel = 1f;

    // ---- Core trainable skill levels ----
    public int strength;
    public int speed;
    public int perception;
    public int aether;

    // ---- Gear-driven combat stats ----
    public int    attackPower;
    public int    blockPower;
    public int    elementalAether;
    public float  attackSpeed;
    public float  critChance;
    public float  critMultiplier;
    public float  GearDodgeChance { get; private set; }

    // ---- Derived (read-only) ----
    public int   Armor               { get; private set; }
    public int   WeaponDamage        { get; private set; }
    public float StrengthBonusDamage { get; private set; }
    public int   MaxHP               { get; private set; }
    public int   MaxMana             { get; private set; }

    // Gear-only for now — Perception will feed into these later
    public float HitChanceBonus => 0f;
    public float CritChanceFinal => critChance;

    // ---- Vitals (runtime) ----
    public int CurrentHP   { get; private set; }
    public int CurrentMana { get; private set; }

    // ---- Events ----
    public event Action OnStatsChanged;
    public event Action OnVitalsChanged;
    public event Action OnDeath;
    public event Action OnOutOfMana;
    public event Action OnPlayerDied;

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
        RecalculateFromEquipment(EquipmentManager.Instance != null
            ? EquipmentManager.Instance.GetAllEquipped()
            : Array.Empty<Item_Data>());

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
        var equipped = EquipmentManager.Instance != null
            ? EquipmentManager.Instance.GetAllEquipped()
            : Array.Empty<Item_Data>();
        RecalculateFromEquipment(equipped);
    }

    [ContextMenu("Log Naked Stats")]
    public void LogNakedStats()
    {
        Debug.Log($"Strength: {strength}  Speed: {speed}  Perception: {perception}  Aethur: {aether}");
        Debug.Log($"MaxHP: {MaxHP}  MaxMana: {MaxMana}  Armor: {Armor}");
        Debug.Log($"WeaponDamage: {WeaponDamage}  AttackSpeed: {attackSpeed}");
        Debug.Log($"CritChance: {critChance}  CritMultiplier: {critMultiplier}");
    }

    // === Master recalculation ===
    public void RecalculateFromEquipment(IEnumerable<Item_Data> equippedItems)
    {
        // Cache vitals % so HP/Mana stay proportional when max changes
        float hpPct = MaxHP  > 0 ? (float)CurrentHP   / MaxHP   : 1f;
        float mpPct = MaxMana > 0 ? (float)CurrentMana / MaxMana : 1f;

        // 1) Pull core skill levels
        var skills = PlayerSkills.Instance;
        if (skills != null)
        {
            int lvl(Enum_Skills s) => skills.GetSkill(s)?.level ?? 1;
            strength  = lvl(Enum_Skills.Strength);
            speed     = lvl(Enum_Skills.Speed);
            perception = lvl(Enum_Skills.Perception);
            aether    = lvl(Enum_Skills.Aethur);
        }
        else
        {
            strength = speed = perception = aether = 1;
        }

        // 2) Reset gear-driven stats
        attackPower    = 0;
        attackSpeed    = baseAttackSpeed;
        blockPower     = 0;
        elementalAether = 0;
        critChance     = 0f;
        critMultiplier = 0f;
        GearDodgeChance = 0f;
        WeaponDamage   = 0;

        // 3) Apply equipment
        foreach (var it in equippedItems)
        {
            if (it == null) continue;
            if (!EquipmentManager.Instance.TryGetEquipRow(it.itemID, out var row)) continue;

            if (row.damage > 0)      WeaponDamage = row.damage;
            if (row.attackSpeed > 0f) attackSpeed = row.attackSpeed;

            blockPower      += row.block;
            elementalAether += row.elementalAether;
            critChance      += row.critChance;
            critMultiplier  = Mathf.Max(critMultiplier, row.critMultiplier);
            GearDodgeChance += row.dodgeChance;

            // Equipment defence field contributes directly to armour
            blockPower += row.defence;
        }

        // 4) Derive final stats
        MaxHP  = baseHP;                            // HP is flat — gear adds defence
        MaxMana = baseMana + (aether * manaPerAether);

        StrengthBonusDamage = strength * strengthDamagePerLevel;
        Armor = baseArmor + blockPower;             // base 1 + gear

        // 5) Restore vitals proportionally
        SetHP  (Mathf.Clamp(Mathf.RoundToInt(hpPct * MaxHP),   0, MaxHP),   silent: true);
        SetMana(Mathf.Clamp(Mathf.RoundToInt(mpPct * MaxMana), 0, MaxMana), silent: true);

        // 6) Notify
        OnStatsChanged?.Invoke();
        OnVitalsChanged?.Invoke();
    }

    // === Vitals API ===
    public void SetHPToMax(bool silent = false)   => SetHP(MaxHP, silent);
    public void SetManaToMax(bool silent = false) => SetMana(MaxMana, silent);

    public void ApplyDamage(int amount)
    {
        if (amount <= 0) return;
        SetHP(Mathf.Max(0, CurrentHP - amount));
        if (CurrentHP <= 0) Die();
    }

    public int ApplyHealing(int amount)
    {
        if (amount <= 0) return 0;
        int prev = CurrentHP;
        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
        int healed = CurrentHP - prev;
        OnVitalsChanged?.Invoke();
        return healed;
    }

    public int ApplyMana(int manaAmount)
    {
        if (manaAmount <= 0) return 0;
        int before = CurrentMana;
        CurrentMana = Mathf.Min(MaxMana, CurrentMana + manaAmount);
        int restored = CurrentMana - before;
        OnVitalsChanged?.Invoke();
        return restored;
    }

    public void RestoreHalfVitals()
    {
        CurrentHP   = Mathf.Clamp(Mathf.CeilToInt(MaxHP   * 0.5f), 1, MaxHP);
        CurrentMana = Mathf.Clamp(Mathf.CeilToInt(MaxMana * 0.5f), 0, MaxMana);
        OnVitalsChanged?.Invoke();
    }

    public void RestoreFullVitals()
    {
        CurrentHP   = MaxHP;
        CurrentMana = MaxMana;
        OnVitalsChanged?.Invoke();
    }

    public bool CanSpendMana(int amount) => amount <= CurrentMana;
    public bool SpendMana(int amount)
    {
        if (amount <= 0) return true;
        if (CurrentMana < amount) { OnOutOfMana?.Invoke(); return false; }
        SetMana(CurrentMana - amount);
        return true;
    }

    public void RestoreMana(int amount)
    {
        if (amount <= 0) return;
        SetMana(Mathf.Min(MaxMana, CurrentMana + amount));
    }

    public void RegenTick(int hpPerTick, int manaPerTick)
    {
        if (hpPerTick   != 0) SetHP  (Mathf.Min(MaxHP,   CurrentHP   + hpPerTick));
        if (manaPerTick != 0) SetMana(Mathf.Min(MaxMana, CurrentMana + manaPerTick));
    }

    public void Revive(int hp = -1)
    {
        _isDead = false;
        if (hp < 0) hp = MaxHP;
        SetHP(Mathf.Clamp(hp, 1, MaxHP));
    }

    private void Die()
    {
        Debug.Log("💀 Player has fallen...");
        OnPlayerDied?.Invoke();
        CombatManager.Instance?.HandlePlayerDeath();
    }

    // === Private setters ===
    private void SetHP(int newHP, bool silent = false)
    {
        newHP = Mathf.Clamp(newHP, 0, MaxHP);
        if (newHP == CurrentHP) return;

        int prev = CurrentHP;
        CurrentHP = newHP;

        if (!silent) OnVitalsChanged?.Invoke();

        if (prev > 0 && CurrentHP == 0 && !_isDead)
        {
            _isDead = true;
            OnDeath?.Invoke();
        }
        if (prev == 0 && CurrentHP > 0) _isDead = false;
    }

    private void SetMana(int newMana, bool silent = false)
    {
        newMana = Mathf.Clamp(newMana, 0, MaxMana);
        if (newMana == CurrentMana) return;

        int prev = CurrentMana;
        CurrentMana = newMana;

        if (!silent) OnVitalsChanged?.Invoke();

        if (prev > 0 && CurrentMana == 0)
            OnOutOfMana?.Invoke();
    }
}
