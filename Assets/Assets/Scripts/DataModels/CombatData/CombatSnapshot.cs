using UnityEngine;

public struct CombatSnapshot
{
    // Offensive stats
    public float BaseDamage;        // e.g. WeaponDamage + StrengthBonusDamage
    public float HitChanceBonus;    // e.g. Precision bonus to hit (as a decimal like 0.10 = +10%)
    public float CritChance;        // final [0..1] crit chance
    public float CritMultiplier;    // e.g. 1.5f = 150% damage on crit

    // Defensive stats
    public int Armor;               // flat damage reduction / block
}
