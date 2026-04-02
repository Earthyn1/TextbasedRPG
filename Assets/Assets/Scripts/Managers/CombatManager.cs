using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Orchestrates combat encounters started from Ink dialog via ~ startCombat("enemyId").
///
/// Flow:
///   Ink fires startCombat  → EventBus "StartCombat"  → StartEncounter()
///   Enemy dies             → SetFlag npcId.killed    → EventBus "CombatResult" true
///   Player dies            → RespawnUI               → EventBus "CombatResult" false
///
/// The active DialogManager / WorldInteractableManager listens for "CombatResult" and
/// resumes the Ink story at the CombatWon or CombatLost knot.
/// </summary>
public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [Header("Combat UI Panel")]
    [Tooltip("The root GameObject containing the CombatUI — activated on combat start, deactivated on end.")]
    [SerializeField] private GameObject combatUIParent;
    [Tooltip("The CombatUI component used to set up bars at encounter start.")]
    [SerializeField] private CombatUI   combatUI;

    [Header("Enemy Health Bar")]
    [Tooltip("Prefab instantiated when a combat encounter starts.")]
    [SerializeField] private GameObject enemyHealthBarPrefab;
    [Tooltip("Where to parent the spawned health bar. Leave empty to spawn at scene root.")]
    [SerializeField] private Transform  enemyHealthBarParent;

    [Header("XP Reward (on kill)")]
    [Tooltip("Base XP awarded when the enemy is defeated. Scaled by enemy level.")]
    [SerializeField] private int xpPerEnemyLevel = 20;

    [Header("Transition Timing")]
    [Tooltip("How long the combat UI panels take to fade in at the start of an encounter.")]
    [SerializeField] private float uiFadeInDuration = 0.30f;
    [Tooltip("Seconds after victory before the killed flag is set (lets Victory label show first).")]
    [SerializeField] private float killFlagDelay = 1.5f;


    private Coroutine _enemyAttackRoutine;

    // ── Encounter runtime state ───────────────────────────────────────────────

    private NPCData _enemyData;
    private int     _enemyHP;
    private float   _playerCd, _enemyCd;
    private float   _playerTimer, _enemyTimer;
    private bool    _active;
    private bool    _killReported;

    private GameObject  _spawnedHealthBar;
    private RectTransform _enemyAnchor;   // prop rect set just before encounter starts

    // ── Events (subscribed to by UI components) ───────────────────────────────

    public event Action<float, float> OnTimersChanged;  // (playerNorm, enemyNorm) 0..1
    public event Action<int, int>     OnEnemyHPChanged; // (current, max)
    public event Action<int>          OnPlayerHit;      // damage dealt to player
    public event Action<int>          OnEnemyHit;       // damage dealt to enemy
    public event Action               OnWin;
    public event Action               OnLose;

    // ── Debug ─────────────────────────────────────────────────────────────────

    private float lastPlayerHit, lastPlayerCrit;
    private float lastEnemyHit,  lastEnemyCrit;
    public float PlayerHitChance  => lastPlayerHit;
    public float PlayerCritChance => lastPlayerCrit;
    public float EnemyHitChance   => lastEnemyHit;
    public float EnemyCritChance  => lastEnemyCrit;

    public string CurrentNPC { get; private set; }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // EventBus hookup
        EventBus.OnTrigger += OnBusEvent;

        // Hitmask interactor hookup
        var interactor = FindFirstObjectByType<ZoneHitmaskInteractor>();
        if (interactor != null)
        {
            interactor.OnInteractableClicked += HandleInteractableClicked;
        }
    }
    private void OnDisable() => EventBus.OnTrigger -= OnBusEvent;

    

    private void HandleInteractableClicked(string interactableId, byte hitId)
    {
        Debug.Log($"[CombatManager] Clicked NPC: {interactableId}");

        CurrentNPC = interactableId;
    }
    private void OnBusEvent(string trigger, object payload)
    {
        if (trigger == "StartCombat")
            StartEncounter(payload as string ?? "");
    }

    // ── Snapshot builders (public so CombatDebugUI etc. can use them) ─────────

    public static CombatSnapshot BuildPlayerSnapshot(PlayerStats ps)
    {
        float critMult = ps.critMultiplier > 1f ? ps.critMultiplier : 1.5f;
        return new CombatSnapshot
        {
            BaseDamage      = ps.WeaponDamage + ps.StrengthBonusDamage,
            HitChanceBonus  = ps.HitChanceBonus,
            CritChance      = ps.CritChanceFinal,
            CritMultiplier  = critMult,
            Armor           = ps.Armor
        };
    }

    public static CombatSnapshot BuildEnemySnapshot(NPCData e)
    {
        float critMult = e.critMultiplier > 1f ? e.critMultiplier : 1.5f;
        return new CombatSnapshot
        {
            BaseDamage      = Mathf.Max(1, e.damage),
            HitChanceBonus  = e.hitChance * 0.02f,
            CritChance      = e.critChance,
            CritMultiplier  = critMult,
            Armor           = Mathf.RoundToInt(e.block)
        };
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public bool IsActive => _active;

    /// <summary>
    /// Call this just before firing "StartCombat" to give the manager a reference
    /// to the enemy prop's RectTransform so the health bar can be positioned over it.
    /// </summary>
    public void SetEnemyAnchor(RectTransform anchor) => _enemyAnchor = anchor;

    public void StartEncounter(string enemyId)
    {
        var enemy = NPCData_Manager.Instance.GetNPCS(enemyId);
        if (enemy == null)
        {
            Debug.LogError($"[CombatManager] Enemy '{enemyId}' not found.");
            return;
        }

        var ps = PlayerStats.Instance;
        if (ps == null)
        {
            Debug.LogError("[CombatManager] PlayerStats not found.");
            return;
        }

        // Activate combat UI at alpha 0 so OnEnable fires and CombatUI subscribes
        // before the first event pushes below — then fade in visually.
        if (combatUIParent != null)
        {
            var cg = combatUIParent.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0f;
            combatUIParent.SetActive(true);
        }
        combatUI?.InitialSetup(enemyId);

        _enemyData    = enemy;
        _enemyHP      = Mathf.Max(1, _enemyData.maxHP);
        _killReported = false;

        _playerCd    = Mathf.Max(0.1f, ps.attackSpeed);
        _enemyCd     = Mathf.Max(0.1f, _enemyData.attackSpeed);
        _playerTimer = _playerCd;
        _enemyTimer  = _enemyCd;

        _active = true;

        // Default stance if none selected
        var stanceSys = PlayerStance.Instance;
        if (stanceSys != null && stanceSys.currentStance == StanceType.None)
            stanceSys.SetStance(StanceType.Defensive);

        // Spawn enemy health bar (starts invisible, fades in alongside combat UI)
        if (enemyHealthBarPrefab != null)
        {
            if (_spawnedHealthBar != null) Destroy(_spawnedHealthBar);
            _spawnedHealthBar = Instantiate(enemyHealthBarPrefab, enemyHealthBarParent);

            var prop = ZoneScenePropSpawner.Instance?.GetPropData(CurrentNPC);

            // Position the health bar above the enemy prop anchor
            if (_enemyAnchor != null)
            {
                var barRect = _spawnedHealthBar.GetComponent<RectTransform>();
                if (barRect != null && prop != null)
                {
                    barRect.anchoredPosition = new Vector2(prop.PosX, prop.PosY);
                }
            }

            // Start invisible for fade-in
            var barCG = _spawnedHealthBar.GetComponent<CanvasGroup>()
                     ?? _spawnedHealthBar.AddComponent<CanvasGroup>();
            barCG.alpha = 0f;
        }
        else
        {
            Debug.LogWarning("[CombatManager] enemyHealthBarPrefab is NULL — assign it in the Inspector.");
        }

        // Fade in both panels together
        StartCoroutine(FadeInCombatPanels());

        // Initial UI push
        OnEnemyHPChanged?.Invoke(_enemyHP, _enemyData.maxHP);
        PushTimerUpdate();

        // Build previews for debug/stats UI
        RefreshPreviewsForCurrentStance();

        Attributes_UI.Instance?.Refresh();
    }

    public void EndEncounter()
    {
        _active = false;

        if (_enemyAttackRoutine != null)
        {
            StopCoroutine(_enemyAttackRoutine);
            _enemyAttackRoutine = null;
        }

        // Award XP (also broadcasts "CombatXPEarned" for the victory toast)
        AwardKillXP();

        // Fade bars out, then fire CombatResult + delayed kill flag
        StartCoroutine(VictoryFadeOut());
    }

    /// <summary>
    /// Fades the combat UI panel and enemy health bar out together,
    /// then fires the CombatResult event and schedules the kill flag.
    /// </summary>
    private IEnumerator VictoryFadeOut()
    {
        var panelCG = combatUIParent    != null ? combatUIParent.GetComponent<CanvasGroup>()    : null;
        var barCG   = _spawnedHealthBar != null ? _spawnedHealthBar.GetComponent<CanvasGroup>() : null;

        float t = 0f;
        while (t < uiFadeInDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - t / uiFadeInDuration);
            if (panelCG != null) panelCG.alpha = alpha;
            if (barCG   != null) barCG.alpha   = alpha;
            yield return null;
        }

        // Destroy + deactivate now that they're invisible
        if (_spawnedHealthBar != null) { Destroy(_spawnedHealthBar); _spawnedHealthBar = null; }
        if (combatUIParent    != null) combatUIParent.SetActive(false);

        // Show the Victory overlay
        EventBus.Fire("CombatResult", true);

        // Delay prop disappear so it happens after the Victory label has been visible
        StartCoroutine(DelayedKillFlag());
    }

    private IEnumerator DelayedKillFlag()
    {
        yield return new WaitForSeconds(killFlagDelay);

        if (_enemyData == null) yield break;

        // Convention: <zoneId>.<npcId>.killed  e.g. "stableCourtyard.goblin1.killed"
        string zone = WorldState.CurrentZoneId;
        string flag = !string.IsNullOrEmpty(zone)
            ? $"{zone}.{_enemyData.npcID}.killed"
            : $"{_enemyData.npcID}.killed";

        WorldStateManager.Instance?.SetFlag(flag);
    }

    public void HandlePlayerDeath()
    {
        Debug.Log("[CombatManager] Player defeated.");

        if (_enemyAttackRoutine != null)
        {
            StopCoroutine(_enemyAttackRoutine);
            _enemyAttackRoutine = null;
        }

        _active = false;
        StopAllCoroutines();

        OnLose?.Invoke();

        if (_spawnedHealthBar != null)
        {
            Destroy(_spawnedHealthBar);
            _spawnedHealthBar = null;
        }

        if (combatUIParent != null) combatUIParent.SetActive(false);

        // Resume the Ink story at the CombatLost knot BEFORE the respawn sequence
        // so the dialog can show flavour text while the respawn screen runs.
        EventBus.Fire("CombatResult", false);

        if (RespawnUI.Instance != null)
            RespawnUI.Instance.BeginRespawnSequence();
        else
            Debug.LogError("[CombatManager] RespawnUI.Instance is null.");
    }

    public void RefreshPreviewsForCurrentStance()
    {
        if (_enemyData == null || PlayerStats.Instance == null) return;

        var ps         = PlayerStats.Instance;
        var playerSnap = BuildPlayerSnapshot(ps);
        var enemySnap  = BuildEnemySnapshot(_enemyData);
        var stanceNow  = PlayerStance.Instance ? PlayerStance.Instance.currentStance : StanceType.None;

        CombatCalculator.PreviewPlayerVsEnemy(
            playerSnap, enemySnap, stanceNow, _enemyData.evasion,
            out float pH, out float pC);
        lastPlayerHit  = pH;
        lastPlayerCrit = pC;

        CombatCalculator.PreviewEnemyVsPlayer(
            enemySnap, playerSnap, _enemyData.hitChance, ps.HitChanceBonus,
            out float eH, out float eC);
        lastEnemyHit  = eH;
        lastEnemyCrit = eC;

        Attributes_UI.Instance?.Refresh();
    }

    // ── Update loop ───────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_active || _enemyData == null) return;

        float dt = Time.deltaTime;
        _playerTimer -= dt;
        _enemyTimer  -= dt;

        if (_playerTimer <= 0f) { ResolvePlayerAttack(); _playerTimer += _playerCd; }
        if (_enemyTimer <= 0f)
        {
            StartEnemyAttack();
            _enemyTimer += _enemyCd;
        }

        PushTimerUpdate();
    }

    // ── Attack resolution ─────────────────────────────────────────────────────


    private void StartEnemyAttack()
    {
        if (!_active) return;

        if (_enemyAttackRoutine != null)
            StopCoroutine(_enemyAttackRoutine);

        _enemyAttackRoutine = StartCoroutine(EnemyAttackRoutine());
    }

    private System.Collections.IEnumerator EnemyAttackRoutine()
    {
        if (!_active) yield break;

        // Start the visual attack first
        CombatAnimator.Instance?.PlayEnemyAttack();

        // Wait until the impact point of the lunge
        yield return new WaitForSeconds(0.20f);

        // Now apply damage
        ResolveEnemyAttack();

        _enemyAttackRoutine = null;
    }

    private void ResolvePlayerAttack()
    {
        if (!_active) return;

        var ps = PlayerStats.Instance;
        if (ps == null) return;

        var playerSnap = new CombatSnapshot
        {
            BaseDamage     = ps.WeaponDamage + ps.StrengthBonusDamage,
            HitChanceBonus = ps.HitChanceBonus,
            CritChance     = ps.CritChanceFinal,
            CritMultiplier = ps.critMultiplier > 1f ? ps.critMultiplier : 1.5f,
            Armor          = ps.Armor
        };

        var enemySnap = new CombatSnapshot
        {
            BaseDamage     = _enemyData.damage,
            HitChanceBonus = _enemyData.hitChance,
            CritChance     = _enemyData.critChance,
            CritMultiplier = _enemyData.critMultiplier > 1f ? _enemyData.critMultiplier : 1.5f,
            Armor          = _enemyData.block
        };

        var stance = PlayerStance.Instance != null ? PlayerStance.Instance.currentStance : StanceType.None;
        var result = CombatCalculator.ResolvePlayerVsEnemy(playerSnap, enemySnap, stance, _enemyData.evasion);
        lastPlayerHit  = result.hitChanceShown;
        lastPlayerCrit = result.critChanceShown;

        if (!result.hitLanded)
        {
            GameLog_Manager.Instance.AddEntry($"You miss {_enemyData.displayName}.", "#AAAAAA");
            return;
        }

        if (result.wasBlocked || result.finalDamage <= 0)
        {
            GameLog_Manager.Instance.AddEntry($"{_enemyData.displayName} blocks your attack!", "#77BBFF");
            return;
        }

        int applied = Mathf.Clamp(result.finalDamage, 0, _enemyHP);
        _enemyHP -= applied;
        OnEnemyHit?.Invoke(applied);
        OnEnemyHPChanged?.Invoke(_enemyHP, _enemyData.maxHP);

        CombatDebugUI.Instance?.ShowChances(lastPlayerHit, lastPlayerCrit, lastEnemyHit, lastEnemyCrit);

        string[] verbs = { "slash", "pierce", "strike", "cleave", "smash", "stab" };
        string   verb  = verbs[UnityEngine.Random.Range(0, verbs.Length)];

        if (result.wasCrit)
            GameLog_Manager.Instance.AddEntry($"Critical! You {verb} {_enemyData.displayName} for {result.finalDamage}!", "#FFD633");
        else
            GameLog_Manager.Instance.AddEntry($"You {verb} {_enemyData.displayName} for {result.finalDamage}.", "#32CD32");

        if (_enemyHP <= 0)
        {
            if (!_killReported)
            {
                QuestManager.Instance?.ReportAction($"Action_Kill_{_enemyData.npcID}", 1);
                _killReported = true;
            }
            OnWin?.Invoke();
            EndEncounter();
        }
    }

    private void ResolveEnemyAttack()
    {
        if (!_active) return;

        var ps = PlayerStats.Instance;
        if (ps == null) return;

        var enemySnap = new CombatSnapshot
        {
            BaseDamage     = _enemyData.damage,
            HitChanceBonus = _enemyData.hitChance,
            CritChance     = _enemyData.critChance,
            CritMultiplier = _enemyData.critMultiplier > 1f ? _enemyData.critMultiplier : 1.5f,
            Armor          = _enemyData.block
        };

        var playerSnap = new CombatSnapshot
        {
            BaseDamage     = ps.WeaponDamage + ps.StrengthBonusDamage,
            HitChanceBonus = ps.HitChanceBonus,
            CritChance     = ps.CritChanceFinal,
            CritMultiplier = ps.critMultiplier > 1f ? ps.critMultiplier : 1.5f,
            Armor          = ps.Armor
        };

        var stance             = PlayerStance.Instance != null ? PlayerStance.Instance.currentStance : StanceType.None;
        float evasionFromSpeed = CombatCalculator.GetPlayerEvasionBonusFromFortitude(ps);

        var result = CombatCalculator.ResolveEnemyVsPlayer(
            enemySnap, playerSnap, _enemyData.hitChance, evasionFromSpeed, stance);

        lastEnemyHit  = result.hitChanceShown;
        lastEnemyCrit = result.critChanceShown;

        if (!result.hitLanded)
        {
            GameLog_Manager.Instance.AddEntry($"{_enemyData.displayName} misses you.", "#AAAAAA");
            return;
        }

        if (result.wasBlocked || result.finalDamage <= 0)
        {
            GameLog_Manager.Instance.AddEntry($"You block {_enemyData.displayName}'s attack!", "#66CCFF");
            CombatDebugUI.Instance?.ShowChances(lastPlayerHit, lastPlayerCrit, lastEnemyHit, lastEnemyCrit);
            return;
        }

        PlayerStats.Instance.ApplyDamage(result.finalDamage);
        OnPlayerHit?.Invoke(result.finalDamage);

        CombatDebugUI.Instance?.ShowChances(lastPlayerHit, lastPlayerCrit, lastEnemyHit, lastEnemyCrit);

        string[] verbs = { "swings at", "claws", "bites", "slashes", "smashes", "strikes" };
        string   verb  = verbs[UnityEngine.Random.Range(0, verbs.Length)];

        if (result.wasCrit)
            GameLog_Manager.Instance.AddEntry(
                $"{_enemyData.displayName} lands a critical hit and {verb} you for {result.finalDamage}!", "#FFAA33");
        else
            GameLog_Manager.Instance.AddEntry(
                $"{_enemyData.displayName} {verb} you for {result.finalDamage}.", "#FF5555");
    }

    // ── XP award on kill ──────────────────────────────────────────────────────

    private void AwardKillXP()
    {
        if (PlayerSkills.Instance == null || _enemyData == null) return;

        int xp = Mathf.Max(10, _enemyData.level * xpPerEnemyLevel);

        var stance = PlayerStance.Instance != null ? PlayerStance.Instance.currentStance : StanceType.None;
        Enum_Skills skill = stance switch
        {
            StanceType.Berserker  => Enum_Skills.Strength,
            StanceType.Defensive  => Enum_Skills.Speed,
            StanceType.Precision  => Enum_Skills.Perception,
            _                     => Enum_Skills.Strength
        };

        PlayerSkills.Instance.AddXP(skill, xp);

        // Broadcast icon + amount for the combat victory XP toast.
        // We intentionally skip XPToastSpawner here — combat has its own
        // overlay toast in CombatResultUI so we don't double-show.
        Sprite xpIcon = PlayerSkills.Instance != null
            ? PlayerSkills.Instance.GetIconForSkill(skill)
            : null;

        EventBus.Fire("CombatXPEarned", new CombatXPPayload
        {
            xp        = xp,
            icon      = xpIcon,
            skillName = skill.ToString()
        });

        Debug.Log($"[CombatManager] Awarded {xp} {skill} XP for killing {_enemyData.npcID}.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void PushTimerUpdate()
    {
        float p = Mathf.Clamp01(_playerTimer / _playerCd);
        float e = Mathf.Clamp01(_enemyTimer  / _enemyCd);
        OnTimersChanged?.Invoke(p, e);
    }

    /// <summary>
    /// Fades the combat UI parent and the spawned enemy health bar in together
    /// after the dialog panel has closed.
    /// </summary>
    private IEnumerator FadeInCombatPanels()
    {
        var panelCG = combatUIParent != null
            ? combatUIParent.GetComponent<CanvasGroup>()
            : null;

        var barCG = _spawnedHealthBar != null
            ? _spawnedHealthBar.GetComponent<CanvasGroup>()
            : null;

        float t = 0f;
        while (t < uiFadeInDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / uiFadeInDuration);
            if (panelCG != null) panelCG.alpha = alpha;
            if (barCG   != null) barCG.alpha   = alpha;
            yield return null;
        }

        if (panelCG != null) panelCG.alpha = 1f;
        if (barCG   != null) barCG.alpha   = 1f;
    }
}

/// <summary>
/// Payload sent on EventBus "CombatXPEarned" so CombatResultUI can show
/// the skill icon alongside the XP number without needing a string parse.
/// </summary>
public class CombatXPPayload
{
    public int    xp;
    public Sprite icon;
    public string skillName; // e.g. "Strength"
}
