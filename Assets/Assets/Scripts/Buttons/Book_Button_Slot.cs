using TMPro;
using UnityEngine;

public class Book_Button_Slot : MonoBehaviour
{

    public TMP_Text Text;
    public RectTransform literacy_Panel;
    public RectTransform RightPanel;
    public string bookID;


    public void SlotClicked()
    {
        SkillData literacyskill = PlayerSkills.Instance.GetSkill(Enum_Skills.Aethur);
        var book = Book_Manager.Instance.GetBook(bookID);


        if (book.RequiredLvl <= literacyskill.level)
        {
            GameManager.Instance.clearMiddlePanels();
            literacy_Panel.gameObject.SetActive(true);
            RightPanel.gameObject.SetActive(true);
            Debug.Log(bookID + "here");
            GameManager.Instance.book_Manager.SetupLiteracyPanel(bookID);
        }
        else
        {
            GameLog_Manager.Instance.AddEntry("You simply cannot understand this book!");
        }


       


    }
}
