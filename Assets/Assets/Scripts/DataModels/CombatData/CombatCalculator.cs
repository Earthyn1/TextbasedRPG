using UnityEngine;

public enum HitOutcome { Miss, Hit, Crit }

// Final result of one swing
public struct CombatResult
{
    public bool hitLanded;
    public bool wasCrit;
    public int finalDamage;

    public float hitChanceShown;
    public float critChanceShown;

    public bool wasBlocked;
}


public static class CombatCalculator
{
    // Tunables
    private const float BASE_HIT_CHANCE = 0.60f;   // baseline 60% hit chance
    private const float MIN_DAMAGE = 1f;      // never deal less than 1

    // === STANCE HELPERS ===
    public static void GetStanceMods(
      StanceType stance,
      out float dmgMul,
      out float accBonusPct,
      out float critBonusPct)
    {
        dmgMul = 1f;
        accBonusPct = 0f;
        critBonusPct = 0f;

        switch (stance)
        {
            case StanceType.Berserker:
                dmgMul = 1.25f;       // +25% damage
                accBonusPct = 0f;     // no accuracy penalty anymore
                critBonusPct = 0f;
                break;

            case StanceType.Defensive:
                dmgMul = 0.9f;        // -10% damage output
                accBonusPct = 0f;
                critBonusPct = 0f;
                break;

            case StanceType.Precision:
                dmgMul = 0.95f;       // -5% damage
                accBonusPct = 0.10f;  // +10% hit chance
                critBonusPct = 0.05f; // +5% crit chance
                break;

            default:
                break;
        }
    }
    // Accuracy bonus (additive to hitChance)
    private static float GetStanceHitBonus(StanceType stance)
    {
        switch (stance)
        {
            case StanceType.Precision: return 0.10f;   // +10% hit chance
            default: return 0f;      // Berserker/Defensive = no change
        }
    }

    // Damage bonus (multiplier-ish, applied as +/-% outgoing damage)
    public static float GetStanceDamageBonus(StanceType stance)
    {
        switch (stance)
        {
            case StanceType.Berserker: return 0.25f;   // +25% damage
            case StanceType.Defensive: return -0.10f;  // -10% damage
            case StanceType.Precision: return -0.05f;  // -5% damage
            default: return 0f;
        }
    }
    public static float GetStanceAccuracyBonus(StanceType stance)
    {
        // Precision stance grants +10% hit chance
        switch (stance)
        {
            case StanceType.Precision: return 0.10f;
            default: return 0f;
        }
    }
    // Crit bonus (additive to crit chance)
    private static float GetStanceCritBonus(StanceType stance)
    {
        switch (stance)
        {
            case StanceType.Precision: return 0.05f;   // +5% crit chance
            default: return 0f;
        }
    }

    // Defensive stance = more block/armor
    private static int GetDefensiveBlockBonus(int baseArmor)
    {
        int bonus = Mathf.Max(1, Mathf.RoundToInt(baseArmor * 0.05f));
        return bonus;
    }

    // Berserker stance = worse block/armor
    private static int GetBerserkerBlockPenalty(int baseArmor)
    {
        // take away either 5% of armor OR 1, whichever is bigger
        int penalty = Mathf.RoundToInt(baseArmor * 0.05f);
        if (penalty < 1) penalty = 1;
        return penalty;
    }

    public static float GetPlayerEvasionBonusFromFortitude(PlayerStats ps)
    {
        // Each point of Fortitude reduces enemy hit chance by 0.5%
        // Fortitude 10 -> 0.05f (5%)
        return ps.fortitude * 0.005f;
    }

    // Combined effective armor based on stance
    public static int GetEffectiveArmorWithStance(StanceType stance, int baseArmor)
    {
        int effective = baseArmor;

        switch (stance)
        {
            case StanceType.Defensive:
                {
                    int bonus = GetDefensiveBlockBonus(baseArmor);
                    effective = baseArmor + bonus;
                    break;
                }

            case StanceType.Berserker:
                {
                    int penalty = GetBerserkerBlockPenalty(baseArmor);
                    effective = baseArmor - penalty;

                    // ✅ Berserker can’t drop below 1 armor if you had any to start with
                    if (baseArmor > 0 && effective < 1)
                        effective = 1;

                    break;
                }

            // Precision, None: leave armor unchanged
            default:
                effective = baseArmor;
                break;
        }

        // Final floor (just in case)
        if (effective < 0)
            effective = 0;

        return effective;
    }


    private static float ApplyDamageVariance(float baseDamage)
    {
        // Low base damage gets flat ±1 variability.
        // High base damage gets ±15–20% variability.
        if (baseDamage <= 3f)
        {
            // Example: 2 → rolls 1–3, 3 → rolls 2–4
            int min = Mathf.Max(1, Mathf.FloorToInt(baseDamage - 1));
            int max = Mathf.CeilToInt(baseDamage + 1);
            return Random.Range(min, max + 1); // inclusive-style roll
        }
        else
        {
            // Smooth % variance for higher damage
            float variance = Mathf.Lerp(0.15f, 0.20f, Mathf.Clamp01((baseDamage - 3f) / 20f));
            float min = baseDamage * (1f - variance);
            float max = baseDamage * (1f + variance);
            return Mathf.Ceil(baseDamage <= 6f
                ? Random.Range(min, max) // round up for mid values
                : Random.Range(min, max)); // leave natural for big hits
        }
    }




    // =========================================================
    // PLAYER ➜ ENEMY
    // =========================================================
    public static CombatResult ResolvePlayerVsEnemy(
    CombatSnapshot player,          // attacker (the player)
    CombatSnapshot enemy,           // defender (the enemy)
    StanceType playerStance,
    float defenderEvasion           // enemy.evasion from sheet (0.10 = 10% dodge)
)
    {
        // --- HIT ROLL ---
        // Base 85% feels good: you usually hit unless target is slippery
        float hitChance =
            0.85f
            + GetStanceHitBonus(playerStance)   // Precision gets +10% hit
            + player.HitChanceBonus             // player accuracy bonus from Precision stat, gear, etc.
            - defenderEvasion;                  // enemy's ability to avoid being hit

        hitChance = Mathf.Clamp01(hitChance);

        bool hitRoll = (Random.value <= hitChance);

        CombatResult result = new CombatResult
        {
            hitLanded = hitRoll,
            wasCrit = false,
            finalDamage = 0,
            hitChanceShown = hitChance,
            critChanceShown = 0f
        };

        if (!hitRoll)
            return result;

        // --- BASE DAMAGE ---
        // player outgoing damage
        float dmg = ApplyDamageVariance(player.BaseDamage); // maybe ±15% for player attacks

        // stance modifies outgoing damage (berserker +25%, defensive -10%, precision -5%)
        float dmgBonus = GetStanceDamageBonus(playerStance);
        dmg *= (1f + dmgBonus);

        // --- CRIT ROLL ---
        float critChance =
            Mathf.Clamp01(
                player.CritChance            // player's base crit
              + GetStanceCritBonus(playerStance) // stance crit bonus (Precision +5%)
            );

        bool critRoll = (Random.value <= critChance);
        if (critRoll)
        {
            dmg *= player.CritMultiplier;   // e.g. 1.5x
        }

        // --- ARMOR / BLOCK (enemy flat damage reduction)
        float reduced = dmg - enemy.Armor;
        int finalInt = Mathf.Max(1, Mathf.RoundToInt(reduced));

        result.wasCrit = critRoll;
        result.finalDamage = finalInt;
        result.critChanceShown = critChance;

        return result;
    }


    // =========================================================
    // ENEMY ➜ PLAYER
    // =========================================================
    public static CombatResult ResolveEnemyVsPlayer(
     CombatSnapshot enemy,          // attacker (enemy)
     CombatSnapshot playerDefender, // defender (player)
     float enemyBaseHitChance,      // enemy.hitChance from sheet (0.9 = 90%)
     float playerEvasionBonus,      // player's avoidance bonus (ps.HitChanceBonus repurposed)
     StanceType playerStance        // so we can modify block/armor on defense
 )
    {
        // --- HIT ROLL ---
        // Start with enemy's authored hitChance (like 0.9)
        // Subtract player's evasiveness bonus (can be 0 if you don't use this)
        float hitChance =
            Mathf.Clamp01(
                enemyBaseHitChance
                - playerEvasionBonus
            );

        bool hitRoll = (Random.value <= hitChance);

        CombatResult result = new CombatResult
        {
            hitLanded = hitRoll,
            wasCrit = false,
            finalDamage = 0,
            hitChanceShown = hitChance,
            critChanceShown = 0f
        };

        if (!hitRoll)
            return result;

        // --- BASE DAMAGE ---
        float dmg = ApplyDamageVariance(enemy.BaseDamage); // ±20% random spread

        // --- CRIT ---
        float critChance = Mathf.Clamp01(enemy.CritChance);
        bool critRoll = (Random.value <= critChance);
        if (critRoll)
        {
            dmg *= enemy.CritMultiplier;
        }

        // --- APPLY PLAYER STANCE TO ARMOR/BLOCK ---
        int effectiveArmor = GetEffectiveArmorWithStance(playerStance, playerDefender.Armor);

        float reduced = dmg - effectiveArmor;
        int finalInt = Mathf.RoundToInt(reduced);

        // --- BLOCK CHECK ---
        bool blocked = false;
        if (finalInt <= 0)
        {
            finalInt = 0;
            blocked = true;
        }

        result.wasCrit = critRoll;
        result.finalDamage = finalInt;
        result.critChanceShown = critChance;
        result.wasBlocked = blocked; // ✅ TICK IT HERE

        return result;
    }

    public static void PreviewChances(
      CombatSnapshot attacker,
      CombatSnapshot defender,
      StanceType stance,
      bool attackerIsPlayer,
      out float hitChance,
      out float critChance)
    {
        // --- replicate stance hooks you already use in Resolve() ---
        GetStanceMods(
            stance,
            out float dmgMul,
            out float accBonusPct,
            out float critBonusPct
        );

        // --- ACCURACY vs EVASION model (match Resolve()) ---

        // We don't have raw "Precision" values in CombatSnapshot anymore,
        // but we *do* have HitChanceBonus, which in your build is basically:
        //   player: precision scaling (like +2% per precision)
        //   enemy: precision scaling
        //
        // So we can treat HitChanceBonus as "precision contribution".
        //
        // Let's reconstruct something equivalent to:
        //   accuracy = (ACC_BASE + attackerPrec * ACC_PER_PREC) * (1 + stanceAccBonus)
        //   evasion  = (EVA_BASE + defenderPrec * EVA_PER_PREC)

        const float ACC_BASE = 100f;
        const float EVA_BASE = 100f;
        const float ACC_PER_PREC = 100f * 0.02f; // if HitChanceBonus is ~+0.02 per prec
        const float EVA_PER_PREC = 100f * 0.02f;

        float attackerPrecLike = attacker.HitChanceBonus / 0.02f;
        float defenderPrecLike = defender.HitChanceBonus / 0.02f;

        float accuracy =
            (ACC_BASE + attackerPrecLike * ACC_PER_PREC) *
            (1f + accBonusPct);

        float evasion =
            (EVA_BASE + defenderPrecLike * EVA_PER_PREC);

        float rawHit = (accuracy <= 0f)
            ? 0f
            : (accuracy / (accuracy + evasion));

        // Player gets "free" forgiveness bonus so combat doesn't feel whiffy
        const float PLAYER_HIT_BONUS = 0.10f; // same as your live calc
        if (attackerIsPlayer)
            rawHit += PLAYER_HIT_BONUS;

        hitChance = Mathf.Clamp01(rawHit);

        // --- CRIT model (match Resolve()) ---
        //
        // critChance = baseCrit + stanceCritBonus + attacker.CritChance
        // where baseCrit was ~5% historically.
        //
        const float BASE_CRIT = 0.05f; // 5%

        float rawCrit = BASE_CRIT
            + attacker.CritChance        // gear/precision-based crit
            + critBonusPct;              // stance bonus (Precision stance +5%)

        critChance = Mathf.Clamp01(rawCrit);
    }

    public static void PreviewPlayerVsEnemy(
     CombatSnapshot player,
     CombatSnapshot enemy,
     StanceType playerStance,
     float defenderEvasion,      // enemy.evasion from data
     out float hitChance,
     out float critChance
 )
    {
        // --- HIT CHANCE (same as ResolvePlayerVsEnemy) ---
        float hc =
            0.85f
            + GetStanceHitBonus(playerStance)   // Precision +10%
            + player.HitChanceBonus             // from Precision stat, gear, etc.
            - defenderEvasion;                  // enemy's dodge/evasion

        hc = Mathf.Clamp01(hc);

        // --- CRIT CHANCE (same as ResolvePlayerVsEnemy) ---
        float cc =
            player.CritChance              // player's crit from stats/precision
            + GetStanceCritBonus(playerStance); // Precision stance +5%

        cc = Mathf.Clamp01(cc);

        hitChance = hc;
        critChance = cc;
    }



    public static void PreviewEnemyVsPlayer(
     CombatSnapshot enemyAttacker,
     CombatSnapshot playerDefender,
     float enemyBaseHitChance,   // enemy.hitChance from data (ex: 0.9f)
     float playerEvasionBonus,   // ps.HitChanceBonus (you're reusing it as "dodge bonus")
     out float hitChance,
     out float critChance
 )
    {
        // --- HIT CHANCE (same as ResolveEnemyVsPlayer) ---
        float hc = Mathf.Clamp01(
            enemyBaseHitChance    // how accurate the enemy is
            - playerEvasionBonus  // how hard you are to hit
        );

        // --- CRIT CHANCE (same as ResolveEnemyVsPlayer) ---
        float cc = Mathf.Clamp01(enemyAttacker.CritChance);

        hitChance = hc;
        critChance = cc;
    }



}
