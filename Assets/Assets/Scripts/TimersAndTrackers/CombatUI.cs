using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CombatUI : MonoBehaviour
{

    public Image PlayerImage;
    public Image EnemyImage;

    public TMP_Text PlayerName;
    public TMP_Text EnemyName;

    public ProgressBar PlayerHPBar;
    public ProgressBar PlayerMPBar;
    public ProgressBar PlayerSpeedBar;

    public ProgressBar EnemyHPBar;
    public ProgressBar EnemySpeedBar;

    private NPCData _enemyData;


    private void OnEnable()
    {
        var cm = CombatManager.Instance;
        if (cm != null)
        {
            cm.OnTimersChanged += HandleTimers;
            cm.OnEnemyHPChanged += HandleEnemyHP;
            cm.OnEnemyHit += ShowEnemyDamage;
            cm.OnPlayerHit += ShowPlayerDamage;
            cm.OnWin += ShowWin;
            cm.OnLose += ShowLose;
        }

        var ps = PlayerStats.Instance;
        if (ps != null)
        {
            ps.OnVitalsChanged += UpdatePlayerBars;
            ps.OnStatsChanged += UpdatePlayerBars;
        }
    }

    private void OnDisable()
    {
        var cm = CombatManager.Instance;
        if (cm != null)
        {
            cm.OnTimersChanged -= HandleTimers;
            cm.OnEnemyHPChanged -= HandleEnemyHP;
            cm.OnEnemyHit -= ShowEnemyDamage;
            cm.OnPlayerHit -= ShowPlayerDamage;
            cm.OnWin -= ShowWin;
            cm.OnLose -= ShowLose;
        }

        var ps = PlayerStats.Instance;
        if (ps != null)
        {
            ps.OnVitalsChanged -= UpdatePlayerBars;
            ps.OnStatsChanged -= UpdatePlayerBars;
        }
    }

    public void InitialSetup(string enemyID)
    {
        _enemyData = NPCData_Manager.Instance.GetNPCS(enemyID);
        if (_enemyData == null)
        {
            Debug.LogError($"InitialSetup failed: enemy '{enemyID}' not found.");
            return;
        }

        PlayerName.text = "Kolf";
        EnemyName.text = _enemyData.displayName;

        EnemyHPBar.SetProgress(1.0f, _enemyData.maxHP + "/" + _enemyData.maxHP);

        var ps = PlayerStats.Instance;
        PlayerHPBar.SetProgress((float)ps.CurrentHP / ps.MaxHP, $"{ps.CurrentHP}/{ps.MaxHP}");
        PlayerMPBar.SetProgress((float)ps.CurrentMana / ps.MaxMana, $"{ps.CurrentMana}/{ps.MaxMana}");

        // Subscribe once to keep UI updated automatically
        ps.OnVitalsChanged += () =>
        {
            PlayerHPBar.SetProgress((float)ps.CurrentHP / ps.MaxHP, $"{ps.CurrentHP}/{ps.MaxHP}");
            PlayerMPBar.SetProgress((float)ps.CurrentMana / ps.MaxMana, $"{ps.CurrentMana}/{ps.MaxMana}");
        };

        // assuming enemyImage = "Skeleton"
        Sprite sprite = Resources.Load<Sprite>($"Portraits/{_enemyData.npcID}");
        if (sprite != null)
        {
            EnemyImage.sprite = sprite;
        }
        else
        {
            Debug.LogError($"❌ Could not find sprite at Resources/Portraits/{_enemyData.npcID}");
        }

    }

    public void TriggerNextBattle()
    {
        CombatManager.Instance.TriggerNextFightAuto();
    }
    // === Event Handlers ===
    private void HandleTimers(float playerNorm, float enemyNorm)
    {
        PlayerSpeedBar.SetProgress(1f - playerNorm, "");
        EnemySpeedBar.SetProgress(1f - enemyNorm, "");
    }

    private void HandleEnemyHP(int cur, int max)
    {
        EnemyHPBar.SetProgress(max > 0 ? (float)cur / max : 0f, $"{cur}/{max}");
    }

    private void UpdatePlayerBars()
    {
        var ps = PlayerStats.Instance;
        if (ps == null) return;

        PlayerHPBar.SetProgress((float)ps.CurrentHP / ps.MaxHP, $"{ps.CurrentHP}/{ps.MaxHP}");
        PlayerMPBar.SetProgress((float)ps.CurrentMana / ps.MaxMana, $"{ps.CurrentMana}/{ps.MaxMana}");
    }

    private void ShowEnemyDamage(int dmg)
    {
        Debug.Log($"Enemy took {dmg} damage!");
        // TODO: spawn floating damage text, play sfx
    }

    private void ShowPlayerDamage(int dmg)
    {
        Debug.Log($"Player took {dmg} damage!");
        // TODO: flash screen, animate HP bar
    }

    private void ShowWin()
    {
        Debug.Log("🎉 Player won!");
        // TODO: victory UI popup
    }

    private void ShowLose()
    {
        Debug.Log("💀 Player lost!");
        // TODO: death screen popup
    }
}




