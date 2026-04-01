using UnityEngine;

/// <summary>
/// Attach to the enemy health bar prefab.
/// Subscribes to CombatManager.OnEnemyHPChanged and keeps the ProgressBar in sync.
/// Sets its initial value immediately in Awake so the bar is correct from the
/// first frame it spawns — even if CombatManager fires OnEnemyHPChanged before
/// Start() would run.
/// </summary>
public class EnemyHealthBarUI : MonoBehaviour
{
    [SerializeField] private ProgressBar hpBar;

    private void Awake()
    {
        // Subscribe immediately so we never miss an event fired in the same frame
        // as Instantiate (CombatManager fires the initial push right after spawning us).
        var cm = CombatManager.Instance;
        if (cm != null)
            cm.OnEnemyHPChanged += HandleHPChanged;
    }

    private void OnDestroy()
    {
        var cm = CombatManager.Instance;
        if (cm != null)
            cm.OnEnemyHPChanged -= HandleHPChanged;
    }

    private void HandleHPChanged(int current, int max)
    {
        if (hpBar == null) return;
        float fill = max > 0 ? (float)current / max : 0f;
        hpBar.SetProgress(fill, $"{current}/{max}");
    }
}
