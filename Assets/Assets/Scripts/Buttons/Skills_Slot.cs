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
            case Enum_Skills.Defence:
                skillName.text = "Defence";
                break;
            case Enum_Skills.Fortitude:
                skillName.text = "Fortitude";
                 break;
            case Enum_Skills.Precision:
                skillName.text = "Precision";
                break;
          
            case Enum_Skills.Aethur:
                skillName.text = "Aethur";
                break;
            case Enum_Skills.Speed:
                skillName.text = "Speed";
                break;

            default:
                skillName.text = "Unknown Skill";
                break;
        }
    }

    public void UpdateSkillDisplay(SkillData data)
    {
        skillName.text = data.skillType.ToString();
        skillLevel.text = $"{data.level}";

        SkillData sd = PlayerSkills.Instance.GetSkill(skill_type);
        float percent = PlayerSkills.Instance.GetLevelProgress01(skill_type);

        // directly set fill
        progressBar.fillAmount = percent;



    }

    public void OnClicked()
    {
       SkillData skillData = PlayerSkills.Instance.GetSkill(skill_type);
        GameLog_Manager.Instance.AddEntry($"{skillName.text} Level: {skillLevel.text} XP: {skillData.xp}");
    }
}



