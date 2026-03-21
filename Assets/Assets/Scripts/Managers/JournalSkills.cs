using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Populates the Skills tab inside the Journal with live skill levels and XP progress.
/// Attach to the Skills panel GameObject.
/// Wire up the four TMP_Text level labels and optional fill bars in the Inspector.
/// </summary>
public class JournalSkills : MonoBehaviour
{
    [Header("Strength")]
    [SerializeField] private TMP_Text strengthLevel;
    [SerializeField] private Image    strengthBar;

    [Header("Perception")]
    [SerializeField] private TMP_Text perceptionLevel;
    [SerializeField] private Image    perceptionBar;

    [Header("Speed")]
    [SerializeField] private TMP_Text speedLevel;
    [SerializeField] private Image    speedBar;

    [Header("Aether")]
    [SerializeField] private TMP_Text aetherLevel;
    [SerializeField] private Image    aetherBar;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        RefreshAll();

        if (PlayerSkills.Instance != null)
            PlayerSkills.Instance.OnSkillLevelChanged += OnSkillLevelChanged;
    }

    private void OnDisable()
    {
        if (PlayerSkills.Instance != null)
            PlayerSkills.Instance.OnSkillLevelChanged -= OnSkillLevelChanged;
    }

    // ── Event callback ────────────────────────────────────────────────────────

    private void OnSkillLevelChanged(Enum_Skills skill, int newLevel)
    {
        RefreshSkill(skill);
    }

    // ── Refresh helpers ───────────────────────────────────────────────────────

    private void RefreshAll()
    {
        if (PlayerSkills.Instance == null) return;

        foreach (Enum_Skills skill in System.Enum.GetValues(typeof(Enum_Skills)))
            RefreshSkill(skill);
    }

    private void RefreshSkill(Enum_Skills skill)
    {
        if (PlayerSkills.Instance == null) return;

        SkillData data     = PlayerSkills.Instance.GetSkill(skill);
        float     progress = PlayerSkills.Instance.GetLevelProgress01(skill);

        if (data == null) return;

        switch (skill)
        {
            case Enum_Skills.Strength:
                SetRow(strengthLevel,   strengthBar,   "Strength",   data.level, progress);
                break;
            case Enum_Skills.Perception:
                SetRow(perceptionLevel, perceptionBar, "Perception", data.level, progress);
                break;
            case Enum_Skills.Speed:
                SetRow(speedLevel,      speedBar,      "Speed",      data.level, progress);
                break;
            case Enum_Skills.Aethur:
                SetRow(aetherLevel,     aetherBar,     "Aether",     data.level, progress);
                break;
        }
    }

    private void SetRow(TMP_Text label, Image bar, string skillName, int level, float progress)
    {
        if (label != null) label.text = $"{skillName} - {level}";
        if (bar   != null) bar.fillAmount = progress;
    }
}
