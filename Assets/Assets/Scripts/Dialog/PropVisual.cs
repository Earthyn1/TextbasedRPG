using UnityEngine;
using UnityEngine.UI;

public class PropVisual : MonoBehaviour
{
    public Image imageA;
    public Image imageB;

    private bool usingA = true;

    public Image ActiveImage => usingA ? imageA : imageB;
    public Image InactiveImage => usingA ? imageB : imageA;

    public void Swap()
    {
        usingA = !usingA;
    }
}