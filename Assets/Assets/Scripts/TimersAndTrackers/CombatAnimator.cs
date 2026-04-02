using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives purely code-based RectTransform animations for combat hit reactions.
///
/// Enemy flinch  — triggered by CombatManager.OnEnemyHit
///   The enemy portrait jolts sideways then snaps back.
///
/// Enemy attack lunge — triggered by CombatManager.OnPlayerHit
///   The enemy pulls back then whooshes forward toward the player, then returns.
///
/// Wire up in Inspector:
///   enemyRect  — RectTransform of the enemy portrait / sprite
///   playerRect — RectTransform of the player portrait (optional, reserved for future)
/// </summary>
public class CombatAnimator : MonoBehaviour
{
    public static CombatAnimator Instance { get; private set; }

    [Header("Targets (set automatically at combat start, or assign manually)")]
    [SerializeField] private RectTransform enemyRect;
    [SerializeField] private RectTransform playerRect; // reserved — not animated yet
    private Image _enemyImage;

    [Header("Enemy Flinch (on hit)")]
    [SerializeField] private float flinchDistance  = 24f;   // px right
    [SerializeField] private float flinchOutTime   = 0.05f;
    [SerializeField] private float flinchReturnTime = 0.10f;

    [Header("Enemy Attack Lunge (on player hit)")]
    [SerializeField] private float pullbackDistance = 40f;  // px away from player
    [SerializeField] private float lungeDistance    = 70f;  // px toward player (past origin)
    [SerializeField] private float pullbackTime     = 0.12f;
    [SerializeField] private float lungeTime        = 0.08f;
    [SerializeField] private float returnTime       = 0.18f;

    [Header("HealthBarSlashScriptRef")]

    [SerializeField] private HealthBarSlashSpawner slashSpawner; 



    // ── Runtime ───────────────────────────────────────────────────────────────

    private Vector2 _enemyOrigin;
    private Coroutine _enemyAnim;

    private void Awake()
    {
        Instance = this;
        if (enemyRect != null)
            _enemyOrigin = enemyRect.anchoredPosition;

        
    }

    /// <summary>
    /// Called just before combat starts to aim the animator at the actual prop that was clicked.
    /// Snaps any running animation, records the new origin, and re-subscribes to CombatManager
    /// events in case OnEnable fired before CombatManager.Instance was available.
    /// </summary>
    public void SetEnemyTarget(RectTransform rt)
    {
        if (_enemyAnim != null) StopCoroutine(_enemyAnim);

        enemyRect = rt;
        _enemyOrigin = rt != null ? rt.anchoredPosition : Vector2.zero;

        // Grab image from first child
        _enemyImage = null;
        if (enemyRect != null && enemyRect.childCount > 0)
        {
            _enemyImage = enemyRect.GetChild(0).GetComponent<Image>();
        }

        EnsureSubscribed();
    }

    private IEnumerator FlashRed(Image img, float duration = 0.15f)
    {
        if (img == null) yield break;

        Color original = img.color;
        Color hitColor = Color.red;

        float half = duration * 0.5f;
        float t = 0f;

        // Fade to red
        while (t < half)
        {
            t += Time.deltaTime;
            img.color = Color.Lerp(original, hitColor, t / half);
            yield return null;
        }

        t = 0f;

        // Fade back to original
        while (t < half)
        {
            t += Time.deltaTime;
            img.color = Color.Lerp(hitColor, original, t / half);
            yield return null;
        }

        img.color = original;
    }

    private void OnEnable()  => EnsureSubscribed();

    private void OnDisable()
    {
        var cm = CombatManager.Instance;
        if (cm == null) return;
        cm.OnEnemyHit  -= HandleEnemyHit;
        cm.OnPlayerHit -= HandlePlayerHit;
    }

    /// <summary>
    /// Subscribes to CombatManager events, unsubscribing first to avoid double-registration.
    /// Safe to call multiple times.
    /// </summary>
    private void EnsureSubscribed()
    {
        var cm = CombatManager.Instance;
        if (cm == null) return;
        cm.OnEnemyHit  -= HandleEnemyHit;
        cm.OnPlayerHit -= HandlePlayerHit;
        cm.OnEnemyHit  += HandleEnemyHit;
        cm.OnPlayerHit += HandlePlayerHit;
    }


    public void PlayEnemyAttack()
    {
        if (enemyRect == null) return;
        RestartAnim(EnemyAttackLunge());
    }




    // ── Handlers ──────────────────────────────────────────────────────────────

    private void HandleEnemyHit(int damage)
    {
        if (enemyRect == null) return;
        RestartAnim(EnemyFlinch());
       
    }

    private void HandlePlayerHit(int damage)
    {
        // 💥 Spawn slash AFTER animation completes
        if (slashSpawner != null)
        {
            slashSpawner.SpawnSlash();
        }
    }

    // ── Sequences ─────────────────────────────────────────────────────────────

    private IEnumerator EnemyFlinch()
    {
        // 🔴 Start flash (parallel)
        if (_enemyImage != null)
            StartCoroutine(FlashRed(_enemyImage));

        // Jolt right
        yield return MoveEnemy(_enemyOrigin, _enemyOrigin + Vector2.right * flinchDistance, flinchOutTime, Easing.EaseOut);

        // Snap back
        yield return MoveEnemy(enemyRect.anchoredPosition, _enemyOrigin, flinchReturnTime, Easing.EaseIn);

        
    }

    private IEnumerator EnemyAttackLunge()
    {
        Vector2 pullbackPos = _enemyOrigin + Vector2.right * pullbackDistance;  // pulls right (back)
        Vector2 lungePos    = _enemyOrigin + Vector2.left  * lungeDistance;     // lunges left (toward player)

        // Pull back
        yield return MoveEnemy(_enemyOrigin, pullbackPos, pullbackTime, Easing.EaseOut);
        // Whoosh forward
        yield return MoveEnemy(pullbackPos, lungePos, lungeTime, Easing.Linear);
        // Return to rest
        yield return MoveEnemy(lungePos, _enemyOrigin, returnTime, Easing.EaseOut);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void RestartAnim(IEnumerator next)
    {
        if (_enemyAnim != null) StopCoroutine(_enemyAnim);
        // Snap back to origin before starting so interrupted anims don't drift
        if (enemyRect != null) enemyRect.anchoredPosition = _enemyOrigin;
        _enemyAnim = StartCoroutine(next);
    }

    private IEnumerator MoveEnemy(Vector2 from, Vector2 to, float duration, System.Func<float, float> ease)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            enemyRect.anchoredPosition = Vector2.LerpUnclamped(from, to, ease(Mathf.Clamp01(t / duration)));
            yield return null;
        }
        enemyRect.anchoredPosition = to;
    }

    // ── Easing ────────────────────────────────────────────────────────────────

    private static class Easing
    {
        public static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
        public static float EaseIn(float t)  => t * t;
        public static float Linear(float t)  => t;
    }
}
