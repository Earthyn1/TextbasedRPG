using TMPro;
using UnityEngine;

public class CombatDebugUI : MonoBehaviour
{
    [SerializeField] private TMP_Text debugText;

    public static CombatDebugUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Updates the panel with the latest chances.
    /// </summary>
    public void ShowChances(float playerHit, float playerCrit, float enemyHit, float enemyCrit)
    {
        debugText.text =
            $"<b>Combat Debug</b>\n" +
            $"Player → Enemy:\n" +
            $"  HitChance: {(playerHit * 100f):F1}%\n" +
            $"  CritChance: {(playerCrit * 100f):F1}%\n\n" +
            $"Enemy → Player:\n" +
            $"  HitChance: {(enemyHit * 100f):F1}%\n" +
            $"  CritChance: {(enemyCrit * 100f):F1}%";
    }
}
