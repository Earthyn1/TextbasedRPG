using TMPro;
using UnityEngine;

public class Word_Slot : MonoBehaviour
{

    public TMP_Text Text;
    public string wordID;
    public string wordLore;
    public bool isUnlocked = false;

    public void SlotClicked()
    {
        if (isUnlocked)
        {
            GameLog_Manager.Instance.AddEntry(wordLore);
        }
        else
        {
            GameLog_Manager.Instance.AddEntry("Word not yet unlocked");
        }
           
    }

}
