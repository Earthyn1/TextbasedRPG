using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Skills_Slot : MonoBehaviour
{

    public Enum_Skills skill_type;
    public TMP_Text skillName;
    public TMP_Text skillLevel;
    public Image progressBar;



    void Start()
    {
        if (skillName == null)
        {
            Debug.LogWarning("SkillName TMP_Text is not assigned!");
            return;
        }

        PlayerSkills.Instance.RegisterSkillUI(this);



        switch (skill_type)
        {
            case Enum_Skills.Strength:
                skillName.text = "Strength";
                break;
            case Enum_Skills.Speed:
                skillName.text = "Speed";
                break;
            case Enum_Skills.Perception:
                skillName.text = "Perception";
                break;
            case Enum_Skills.Aethur:
                skillName.text = "Aethur";
                break;
            default:
                skillName.text = "Unknown Skill";
                break;
        }
    }

    public void UpdateSkillDisplay(SkillData data)
    {
        if (data == null) return;
        if (skillName == null || skillLevel == null) return;

        skillLevel.text = $"{data.level}";

        if (progressBar != null)
            progressBar.fillAmount = PlayerSkills.Instance.GetLevelProgress01(skill_type);
    }

    public void OnClicked()
    {
       SkillData skillData = PlayerSkills.Instance.GetSkill(skill_type);
        GameLog_Manager.Instance.AddEntry($"{skillName.text} Level: {skillLevel.text} XP: {skillData.xp}");
    }
}



