using System;
using System.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;


public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    public GameObject xppanel;
    public GameObject XPPrefab;
    public Sprite Strength;
    public Sprite Speed;
    public Sprite Defence;
    public Sprite Fortitude;
    public Sprite Precision;
    public Sprite Aether;
    public Sprite GenericXP; // optional fallback

    public GameObject enemyProgressBar;
    public Animator endCombatUIAnimation;

    [Header("UI Buttons")]
    [SerializeField] private Button ReturnButton;
    [SerializeField] private Button NextFightButton;

    [SerializeField] private HitSplatUISpawner playerTextSpawner;
    [SerializeField] private HitSplatUISpawner enemyTextSpawner;

    // Encounter runtime state
    private NPCData _enemyData;
    private int _enemyHP;
    private float _playerCd, _enemyCd;     // cooldown durations
    private float _playerTimer, _enemyTimer; // time remaining
    private bool _active;
    private bool _buttonPressed;

    // Events UI can subscribe to
    public event Action<float, float> OnTimersChanged; // (playerT/_playerCd, enemyT/_enemyCd) normalized 0..1
    public event Action<int, int> OnEnemyHPChanged;    // (current, max)
    public event Action<int> OnPlayerHit;              // dmg dealt to player
    public event Action<int> OnEnemyHit;               // dmg dealt to enemy
    public event Action OnWin;
    public event Action OnLose;

    [SerializeField] private Animator playerPortraitAnimator;
    [SerializeField] private Animator enemyPortraitAnimator;

    [SerializeField] private Animator playerPortraitAnimator_2;
    [SerializeField] private Animator enemyPortraitAnimator_2;

    // Debug cache for hit/crit chances
    private float lastPlayerHit, lastPlayerCrit;
    private float lastEnemyHit, lastEnemyCrit;

    public float PlayerHitChance => lastPlayerHit;        // 0..1
    public float PlayerCritChance => lastPlayerCrit;      // 0..1
    public float EnemyHitChance => lastEnemyHit;          // 0..1
    public float EnemyCritChance => lastEnemyCrit;        // 0..1


    public event Action<string, int> OnEnemyDefeated; // (enemyIdOrTag, count)
    private bool _killReported; // guard to avoid double credit

    public static CombatSnapshot BuildPlayerSnapshot(PlayerStats ps)
    {
        float critMult = ps.critMultiplier;
        if (critMult <= 1f) critMult = 1.5f; // floor so crits feel ok

        return new CombatSnapshot
        {
            BaseDamage = ps.WeaponDamage + ps.StrengthBonusDamage,
            HitChanceBonus = ps.HitChanceBonus,        // e.g. +0.10f = +10% hit
            CritChance = ps.CritChanceFinal,           // already final decimal 0..1
            CritMultiplier = critMult,
            Armor = ps.Armor
        };
    }

    public static CombatSnapshot BuildEnemySnapshot(NPCData e)
    {
        float critMult = e.critMultiplier;
        if (critMult <= 1f) critMult = 1.5f;

        return new CombatSnapshot
        {
            BaseDamage = Mathf.Max(1, e.damage),
            HitChanceBonus = e.hitChance * 0.02f,      // reuse your scaling
            CritChance = e.critChance,                 // 0..1 from data
            CritMultiplier = critMult,
            Armor = Mathf.RoundToInt(e.block)
        };
    }








    public Sprite GetSkillSprite(Enum_Skills skill)
    {
        switch (skill)
        {
            case Enum_Skills.Strength: return Strength;
            case Enum_Skills.Speed: return Speed;
            case Enum_Skills.Defence: return Defence;
            case Enum_Skills.Precision: return Precision;
            case Enum_Skills.Fortitude: return Fortitude;
            case Enum_Skills.Aethur: return Aether;

            default: return GenericXP; // can be null if you don’t have one
        }
    }
    private void Start()
    {
        // Hook up button listeners
        ReturnButton.onClick.AddListener(() => ReturnToLastzone());
        NextFightButton.onClick.AddListener(() => NextFight());
    }


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsActive => _active;

    public bool IsPlayerInCombat()
    {
        return _active;
    }

    private IEnumerator FadeCanvas(CanvasGroup cg, float targetAlpha, float duration)
    {
        float startAlpha = cg.alpha;
        float time = 0f;

        while (time < duration)
        {
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        cg.alpha = targetAlpha;
    }

    public void StartEncounter(string enemyId)
    {
        var enemy = NPCData_Manager.Instance.GetNPCS(enemyId);
        if (enemy == null) { Debug.LogError($"CombatManager: enemy '{enemyId}' not found."); return; }

        _enemyData = enemy;
        _enemyHP = Mathf.Max(1, _enemyData.maxHP);

        _killReported = false;


        var ps = PlayerStats.Instance;
        if (ps == null) { Debug.LogError("CombatManager: PlayerStats not found."); return; }

        // cooldowns
        _playerCd = Mathf.Max(0.1f, Mathf.Max(0.1f, ps.attackSpeed)); // your attackSpeed is aggregated in PlayerStats
        _enemyCd = Mathf.Max(0.1f, _enemyData.attackSpeed);
        _playerTimer = _playerCd;
        _enemyTimer = _enemyCd;

        _active = true;
        _buttonPressed = false;

        // ===== Ensure stance is set =====
        var stanceSys = PlayerStance.Instance;
        if (stanceSys != null && stanceSys.currentStance == StanceType.None)
        {
            stanceSys.SetStance(StanceType.Defensive); // uses your existing UI logic
            Debug.Log("StartEncounter: Defaulted stance to Defensive.");
        }

        // initial UI push
        OnEnemyHPChanged?.Invoke(_enemyHP, _enemyData.maxHP);
        PushTimerUpdate();

        // Precompute and store expected hit/crit before first swing
        var pSnap = CombatManager.BuildPlayerSnapshot(ps);
        var eSnap = CombatManager.BuildEnemySnapshot(_enemyData);

        // Use a neutral stance (or current stance if set)
        var stance = PlayerStance.Instance ? PlayerStance.Instance.currentStance : StanceType.None;

        // snapshots
        var playerSnap = BuildPlayerSnapshot(ps);
        var enemySnap = BuildEnemySnapshot(_enemyData);

        var stanceNow = PlayerStance.Instance
            ? PlayerStance.Instance.currentStance
            : StanceType.None;

        // Player preview vs. enemy
        CombatCalculator.PreviewPlayerVsEnemy(
            playerSnap,
            enemySnap,
            stanceNow,
            _enemyData.evasion,              // <--- NEW ARG
            out float previewPlayerHit,
            out float previewPlayerCrit
        );
        lastPlayerHit = previewPlayerHit;
        lastPlayerCrit = previewPlayerCrit;

        // Enemy preview vs. player
        CombatCalculator.PreviewEnemyVsPlayer(
            enemySnap,
            playerSnap,
            _enemyData.hitChance,            // enemyBaseHitChance
            ps.HitChanceBonus,               // playerEvasionBonus
            out float previewEnemyHit,
            out float previewEnemyCrit
        );
        lastEnemyHit = previewEnemyHit;
        lastEnemyCrit = previewEnemyCrit;

        // now that lastPlayerHit / lastPlayerCrit etc are set using the SAME formulas
        // Attributes_UI will show the "true" 77% (or whatever) from the start
        Attributes_UI.Instance?.Refresh();
    }

    public void EndEncounter(bool clear = true)
    {
        _active = false;
        CanvasGroup canvasGroup = enemyProgressBar.GetComponent<CanvasGroup>();
        StartCoroutine(FadeCanvas(canvasGroup, 0f, 0.5f));
        endCombatUIAnimation.SetTrigger("EndCombat");
    }

    public void PlayerDiedInCombat(bool clear = true)
    {
        _active = false;
      //  CanvasGroup canvasGroup = enemyProgressBar.GetComponent<CanvasGroup>();
       // StartCoroutine(FadeCanvas(canvasGroup, 0f, 0.5f));
        endCombatUIAnimation.SetTrigger("PlayerDied");
    }




    public void NextFight()
    {
        if(_buttonPressed == false)
        {
            endCombatUIAnimation.SetTrigger("TransitionToStart");
            NPCData_Manager.Instance.combatUI.InitialSetup(_enemyData.npcID);
            _buttonPressed = true;
        }
  
    }

    public void TriggerNextFightAuto()
    {
        StartEncounter(_enemyData.npcID);
    }

    public void ReturnToLastzone()
    {
        if (_buttonPressed == false)
        {
            Debug.Log("Return!!");
            GameManager.Instance.zoneUIManager.DisplayZone(GameManager.Instance.zoneUIManager.LastZoneEntered);
            _buttonPressed = true;
        }

           
        
    }
    private void Update()
    {
        if (!_active || _enemyData == null) return;

        float dt = Time.deltaTime;

        _playerTimer -= dt;
        _enemyTimer -= dt;

        // Player swing
        if (_playerTimer <= 0f)
        {
            ResolvePlayerAttack();
            _playerTimer += _playerCd;
        }

        // Enemy swing
        if (_enemyTimer <= 0f)
        {
            ResolveEnemyAttack();
            _enemyTimer += _enemyCd;
        }

        PushTimerUpdate();
    }

    private void ResolvePlayerAttack()
    {
        if (!_active) return;

        var ps = PlayerStats.Instance;
        if (ps == null) return;

        // Player snapshot (attacker)
        CombatSnapshot playerSnap = new CombatSnapshot
        {
            BaseDamage = ps.WeaponDamage + ps.StrengthBonusDamage,
            HitChanceBonus = ps.HitChanceBonus, // player's own accuracy bonus (0.10f = +10%)
            CritChance = ps.CritChanceFinal,
            CritMultiplier = (ps.critMultiplier > 1f ? ps.critMultiplier : 1.5f),
            Armor = ps.Armor
        };

        // Enemy snapshot (defender)
        CombatSnapshot enemySnap = new CombatSnapshot
        {
            BaseDamage = _enemyData.damage, // we won't actually use defender.BaseDamage
            HitChanceBonus = _enemyData.hitChance, // <-- NOTE: we'll reinterpret this in the calc
            CritChance = _enemyData.critChance,
            CritMultiplier = (_enemyData.critMultiplier > 1f ? _enemyData.critMultiplier : 1.5f),
            Armor = _enemyData.block // flat DR the player must punch through
        };

        playerPortraitAnimator_2?.SetTrigger("Atk");

        var stance = PlayerStance.Instance != null
            ? PlayerStance.Instance.currentStance
            : StanceType.None;

        CombatResult result = CombatCalculator.ResolvePlayerVsEnemy(
            playerSnap,
            enemySnap,
            stance,
            _enemyData.evasion // 👈 NEW ARG: defender's evasion
        );

        lastPlayerHit = result.hitChanceShown;
        lastPlayerCrit = result.critChanceShown;

        if (!result.hitLanded)
        {
            GameLog_Manager.Instance.AddEntry(
                $"You miss {_enemyData.displayName}.",
                "#AAAAAA"
            );
            return;
        }

        if (result.wasBlocked || result.finalDamage <= 0)
        {
            enemyTextSpawner?.ShowBlock();
            GameLog_Manager.Instance.AddEntry($"{_enemyData.displayName} blocks your attack!", "#77BBFF");
            return;
        }

        enemyPortraitAnimator?.SetTrigger("HitFlash");

        int applied = Mathf.Min(result.finalDamage, _enemyHP);
        if (applied < 0) applied = 0;

        _enemyHP -= applied;
        OnEnemyHit?.Invoke(applied);
        OnEnemyHPChanged?.Invoke(_enemyHP, _enemyData.maxHP);

        AddXP(applied);

        CombatDebugUI.Instance?.ShowChances(lastPlayerHit, lastPlayerCrit, lastEnemyHit, lastEnemyCrit);

        string[] verbs = { "slash", "pierce", "strike", "cleave", "smash", "stab" };
        string verb = verbs[UnityEngine.Random.Range(0, verbs.Length)];

        if (result.wasCrit)
        { 
            enemyTextSpawner?.ShowCritHit(result.finalDamage);
            GameLog_Manager.Instance.AddEntry(
            $"Critical! You {verb} {_enemyData.displayName} for {result.finalDamage}!", "#FFD633");
        }
                
        else
        {

            enemyTextSpawner?.ShowNormalHit(result.finalDamage);

            GameLog_Manager.Instance.AddEntry(
               $"You {verb} {_enemyData.displayName} for {result.finalDamage}.",
               "#32CD32"
           );


        }
           

        if (_enemyHP <= 0)
        {
            if (!_killReported)
            {
                string enemyKey = _enemyData.npcID;
                QuestManager.Instance.ReportAction($"Action_Kill_{enemyKey}", 1);
                _killReported = true;
            }

            Debug.Log("We won!!");
            EndEncounter();
            OnWin?.Invoke();
        }
    }

    private void ResolveEnemyAttack()
    {
        if (!_active) return;

        var ps = PlayerStats.Instance;
        if (ps == null) return;

        // Enemy as attacker
        CombatSnapshot enemySnap = new CombatSnapshot
        {
            BaseDamage = _enemyData.damage,
            HitChanceBonus = _enemyData.hitChance,
            CritChance = _enemyData.critChance,
            CritMultiplier = (_enemyData.critMultiplier > 1f ? _enemyData.critMultiplier : 1.5f),
            Armor = _enemyData.block // doesn't really matter on attack
        };

        // Player as defender
        CombatSnapshot playerSnap = new CombatSnapshot
        {
            BaseDamage = ps.WeaponDamage + ps.StrengthBonusDamage,
            HitChanceBonus = ps.HitChanceBonus, // we use this as your "evasion bonus"
            CritChance = ps.CritChanceFinal,
            CritMultiplier = (ps.critMultiplier > 1f ? ps.critMultiplier : 1.5f),
            Armor = ps.Armor          // THIS is baseArmor we're modifying with stance
        };

        enemyPortraitAnimator_2?.SetTrigger("Atk");

        StanceType stance = PlayerStance.Instance != null
            ? PlayerStance.Instance.currentStance
            : StanceType.None;

        // Calculate how much the player's Fortitude lowers enemy accuracy
        float evasionFromFortitude = CombatCalculator.GetPlayerEvasionBonusFromFortitude(ps);

        // Feed that into ResolveEnemyVsPlayer instead of ps.HitChanceBonus
        CombatResult result = CombatCalculator.ResolveEnemyVsPlayer(
            enemySnap,
            playerSnap,
            _enemyData.hitChance,    // enemy base accuracy (e.g. 0.9)
            evasionFromFortitude,    // player's avoidance from Fortitude
            stance                   // still important for armor/block math
        );

        lastEnemyHit = result.hitChanceShown;
        lastEnemyCrit = result.critChanceShown;

        if (!result.hitLanded)
        {
            // Enemy failed the accuracy roll.
            GameLog_Manager.Instance.AddEntry(
                $"{_enemyData.displayName} misses you.",
                "#AAAAAA"
            );
            return;
        }

        // At this point: the attack "landed" mechanically.
        // Now check whether armor/stance fully absorbed it.

        if (result.wasBlocked || result.finalDamage <= 0)
        {
            // BLOCK splat (blue-ish)
            playerTextSpawner.ShowBlock();

            // Fully blocked / absorbed the blow.
            GameLog_Manager.Instance.AddEntry(
                $"You block {_enemyData.displayName}'s attack!",
                "#66CCFF" // light blue/steel color for block feedback
            );

            // No HP loss, no hit flash, no damage event.
            CombatDebugUI.Instance?.ShowChances(lastPlayerHit, lastPlayerCrit, lastEnemyHit, lastEnemyCrit);
            return;
        }

        // Normal damaging hit (not blocked):

        playerPortraitAnimator?.SetTrigger("HitFlash");

        PlayerStats.Instance.ApplyDamage(result.finalDamage);
        OnPlayerHit?.Invoke(result.finalDamage);

        CombatDebugUI.Instance?.ShowChances(lastPlayerHit, lastPlayerCrit, lastEnemyHit, lastEnemyCrit);

        string[] verbs = { "swings at", "claws", "bites", "slashes", "smashes", "strikes" };
        string verb = verbs[UnityEngine.Random.Range(0, verbs.Length)];

        if (result.wasCrit)
        {
            playerTextSpawner.ShowCritHit(result.finalDamage);

            GameLog_Manager.Instance.AddEntry(
                $"{_enemyData.displayName} lands a critical hit and {verb} you for {result.finalDamage}!",
                "#FFAA33"
            );
        }
        else
        {
            playerTextSpawner.ShowNormalHit(result.finalDamage);

            GameLog_Manager.Instance.AddEntry(
                $"{_enemyData.displayName} {verb} you for {result.finalDamage}.",
                "#FF5555"
            );
        }
    }

   

    public void RefreshPreviewsForCurrentStance()
    {
        if (!_active || _enemyData == null || PlayerStats.Instance == null)
            return;

        var ps = PlayerStats.Instance;

        var playerSnap = BuildPlayerSnapshot(ps);
        var enemySnap = BuildEnemySnapshot(_enemyData);

        var stanceNow = PlayerStance.Instance
            ? PlayerStance.Instance.currentStance
            : StanceType.None;

        CombatCalculator.PreviewPlayerVsEnemy(
            playerSnap,
            enemySnap,
            stanceNow,
            _enemyData.evasion,
            out float previewPlayerHit,
            out float previewPlayerCrit
        );
        lastPlayerHit = previewPlayerHit;
        lastPlayerCrit = previewPlayerCrit;

        CombatCalculator.PreviewEnemyVsPlayer(
            enemySnap,
            playerSnap,
            _enemyData.hitChance,
            ps.HitChanceBonus,
            out float previewEnemyHit,
            out float previewEnemyCrit
        );
        lastEnemyHit = previewEnemyHit;
        lastEnemyCrit = previewEnemyCrit;

        Attributes_UI.Instance?.Refresh();
    }

    public void HandlePlayerDeath()
    {
        Debug.Log("☠️ CombatManager: Player defeated.");

        PlayerDiedInCombat();

        // Stop ongoing combat loops
        StopAllCoroutines();

        // Optional: freeze UI / disable buttons
        OnLose?.Invoke();

        // Respawn after short delay
        StartCoroutine(RespawnAfterDelay(2f));
    }

    private IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // restore vitals
        var ps = PlayerStats.Instance;
        ps.RestoreHalfVitals();

        // figure out which zone we should go back to
        var gm = GameManager.Instance;
        ZoneData zone = null;
        if (gm != null)
            zone = gm.GetZoneByID(WorldState.CurrentZoneId);

        Debug.Log($"[Respawn] Returning to zone {zone?.id ?? "NULL"}");

        // reset UI state cleanly
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
    }



    public void RegisterPlayerConsumedFoodThisTurn(Item_Data item, int healedHP, int healedMP)
    {
        // Build nice log line
        string msg;
        if (healedHP > 0 && healedMP > 0)
        {
            msg = $"You use {item.itemName}, restoring {healedHP} HP and {healedMP} MP.";
        }
        else if (healedHP > 0)
        {
            msg = $"You use {item.itemName}, restoring {healedHP} HP.";
        }
        else if (healedMP > 0)
        {
            msg = $"You drink {item.itemName}, restoring {healedMP} MP.";
        }
        else
        {
            msg = $"You use {item.itemName}.";
        }

        GameLog_Manager.Instance?.AddEntry(msg, "#32CD32"); // green text for "good"

    }





    private void AddXP(int damage)
    {
        var stanceType = PlayerStance.Instance.currentStance;

        int xpAmount = 0;
        string xpText = "";
        Sprite xpIcon = null;

        switch (stanceType)
        {
            case StanceType.Berserker:
                xpAmount = damage * 4;
                PlayerSkills.Instance.AddXP(Enum_Skills.Strength, xpAmount);
                xpText = xpAmount + " xp";
                xpIcon = Strength;
                break;

            case StanceType.Defensive:
                xpAmount = damage * 4;
                PlayerSkills.Instance.AddXP(Enum_Skills.Defence, xpAmount);
                xpText = xpAmount + " xp";
                xpIcon = Defence;
                break;

            case StanceType.Precision:
                xpAmount = damage * 4;
                PlayerSkills.Instance.AddXP(Enum_Skills.Precision, xpAmount);
                xpText = xpAmount + " xp";
                xpIcon = Speed;
                break;

            case StanceType.None:
            default:
                // No XP gain
                return;
        }

        // Create new XP UI element
        var go = Instantiate(XPPrefab, xppanel.transform, false);

        // Look for XPNumbers on children too
        var xp = go.GetComponentInChildren<XPNumbers>();
        if (xp != null)
        {
            xp._Text.text = xpText;
            xp._Image.sprite = xpIcon;
        }
        else
        {
            Debug.LogError("XPNumbers component missing on the XPPrefab instance or its children.");
        }
    }


    private void PushTimerUpdate()
    {
        float p = Mathf.Clamp01(_playerTimer / _playerCd);
        float e = Mathf.Clamp01(_enemyTimer / _enemyCd);
        OnTimersChanged?.Invoke(p, e);
    }
}
