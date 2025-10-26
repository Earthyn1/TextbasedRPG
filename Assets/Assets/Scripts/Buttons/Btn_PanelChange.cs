using UnityEngine;
using UnityEngine.UI;

public class Btn_PanelChange : MonoBehaviour
{

    public GameObject Current_Panel;
   
    public Btn_PanelChange Other_Button;
    public Button myButton;
    private bool isSelected = true;
    public bool UseOnStart;

    private void Start()
    {
        if (UseOnStart == true)
        {
            OnClicked();
        } 
    }
    public void OnClicked()
    {
        Current_Panel.SetActive(true);
        Other_Button.Current_Panel.SetActive(false);
        myButton.interactable = false;
        Other_Button.GetComponent<Button>().interactable = true; // enable click

    }
}
