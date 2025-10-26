using UnityEngine;
using TMPro;
using UnityEngine.UI;


public class XPNumbers : MonoBehaviour
{

    public TMP_Text _Text;
    public Image _Image;

    [SerializeField] private Animator Animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Animator?.SetTrigger("Play");

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DestroySelf()
    {
        Destroy(transform.parent.gameObject);
    }

}
