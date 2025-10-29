using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SkillData
{
    public Enum_Skills skillType;
    public int level;
    public float xp;


    public SkillData(Enum_Skills type)
    {
        skillType = type;
        level = 1;
        xp = 0f;
    }
}

public class PlayerSkills : MonoBehaviour
{
    public static PlayerSkills Instance { get; private set; }

    public Dictionary<Enum_Skills, SkillData> skills = new Dictionary<Enum_Skills, SkillData>();
    private List<Skills_Slot> registeredSkillUIs = new List<Skills_Slot>();

    // icons per skill
    [Header("XP Icons")]
    public Sprite StrengthIcon;
    public Sprite SpeedIcon;
    public Sprite DefenceIcon;
    public Sprite PrecisionIcon;
    public Sprite FortitudeIcon;
    public Sprite AetherIcon;
    public Sprite GenericXP;

    public Sprite GetIconForSkill(Enum_Skills skill)
    {
        switch (skill)
        {
            case Enum_Skills.Strength: return StrengthIcon;
            case Enum_Skills.Defence: return DefenceIcon;
            case Enum_Skills.Precision: return SpeedIcon;      // using Speed icon for Precision stance
            case Enum_Skills.Speed: return SpeedIcon;
            case Enum_Skills.Fortitude: return FortitudeIcon;
            case Enum_Skills.Aethur: return AetherIcon;
            default: return GenericXP;
        }
    }

    // This is what CombatManager calls after a successful player hit
    public void AwardCombatXPFromHit(int damageDealt, StanceType stanceUsed)
    {
        Enum_Skills skillToTrain;
        int xpAmount;

        switch (stanceUsed)
        {
            case StanceType.Berserker:
                skillToTrain = Enum_Skills.Strength;
                xpAmount = damageDealt * 4;
                break;

            case StanceType.Defensive:
                skillToTrain = Enum_Skills.Defence;
                xpAmount = damageDealt * 4;
                break;

            case StanceType.Precision:
                skillToTrain = Enum_Skills.Precision;
                xpAmount = damageDealt * 4;
                break;

            case StanceType.None:
            default:
                return; // no xp in neutral stance
        }

        // 1. Actually add XP. This updates skills dict,
        //    logs to GameLog, handles level ups, and refreshes UIs.
        AddXP(skillToTrain, xpAmount);

        // 2. Pop the floating "+12 xp" toast
        if (XPToastSpawner.Instance != null)
        {
            string xpText = xpAmount + " xp";
            Sprite icon = GetIconForSkill(skillToTrain);
            XPToastSpawner.Instance.ShowXPToast(xpText, icon);
        }
    }
    public void RegisterSkillUI(Skills_Slot ui)
    {
        if (!registeredSkillUIs.Contains(ui))
        {
            registeredSkillUIs.Add(ui);
            // Initialize display immediately
            ui.UpdateSkillDisplay(GetSkill(ui.skill_type));
        }
    }

    public event Action<Enum_Skills, int> OnSkillLevelChanged; // (skill, newLevel)


    private void UpdateAllSkillUIs(Enum_Skills skill)
    {
        SkillData data = GetSkill(skill);
        foreach (var ui in registeredSkillUIs)
        {
            if (ui.skill_type == skill)
                ui.UpdateSkillDisplay(data);
        }
    }

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize all skills
            foreach (Enum_Skills skill in Enum.GetValues(typeof(Enum_Skills)))
            {
                skills[skill] = new SkillData(skill);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Add XP to a skill
    public void AddXP(Enum_Skills skill, float amount)
    {
        if (!skills.ContainsKey(skill)) return;

        SkillData data = skills[skill];
        data.xp += amount;

        GameLog_Manager.Instance.AddEntry($"Gained {amount} XP in {skill}", "#AB47BC");

        // Level-up loop
        while (data.level < 99 && data.xp >= XPFormula.XPForLevels[data.level])
        {
            data.level++;
            Debug.Log($"{skill} leveled up to {data.level}!");

            GameLog_Manager.Instance.AddEntry($"{skill} level {data.level}!", "#AB47BC"); // purple
            OnSkillLevelChanged?.Invoke(skill, data.level);   // << notify listeners


        }

        UpdateAllSkillUIs(skill);

    }

    public float GetLevelProgress01(Enum_Skills skill)
    {
        SkillData d = GetSkill(skill);
        // Max level: treat as full bar
        if (d.level >= 99) return 1f;

        // Total XP thresholds
        float prevLevelXP = (d.level > 1) ? XPFormula.XPForLevels[d.level - 1] : 0f;
        float nextLevelXP = XPFormula.XPForLevels[d.level];

        // XP earned inside this level
        float gainedThisLevel = d.xp - prevLevelXP;
        float spanThisLevel = nextLevelXP - prevLevelXP;

        // 0..1 progress
        return Mathf.Clamp01(gainedThisLevel / spanThisLevel);
    }




    // Optional: get XP remaining for next level
    public float GetXPToNextLevel(Enum_Skills skill)
    {
        SkillData data = GetSkill(skill);
        if (data.level >= 99) return 0;
        return XPFormula.XPForLevels[data.level] - data.xp;
    }

    // Optional: get skill info
    public SkillData GetSkill(Enum_Skills skill)
    {
        skills.TryGetValue(skill, out SkillData data);
        return data;
    }
}
