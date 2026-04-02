using UnityEngine;

public class SlashAutoCleanup : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.45f;

    private void OnEnable()
    {
        CancelInvoke(nameof(DestroySelf));
        Invoke(nameof(DestroySelf), lifetime);
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }
}