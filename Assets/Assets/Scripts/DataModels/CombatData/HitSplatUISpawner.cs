using UnityEngine;

public class HitSplatUISpawner : MonoBehaviour
{
    [SerializeField] private HitSplatUI floatingTextPrefab;
    [SerializeField] private RectTransform spawnAnchor;
    [SerializeField] private float edgePadding = 10f; // optional margin from edges

    /// <summary>
    /// Spawns a hit splat with custom settings at a random position within the anchor box.
    /// </summary>
    public void ShowHitSplat(string message, Color textColor, Color bgColor, float scale)
    {
        if (floatingTextPrefab == null || spawnAnchor == null)
            return;

        var ft = Instantiate(floatingTextPrefab, spawnAnchor);
        var rect = ft.GetComponent<RectTransform>();

        // get random position within spawnAnchor rect
        Vector2 halfSize = spawnAnchor.rect.size * 0.5f;
        float x = Random.Range(-halfSize.x + edgePadding, halfSize.x - edgePadding);
        float y = Random.Range(-halfSize.y + edgePadding, halfSize.y - edgePadding);
        rect.anchoredPosition = new Vector2(x, y);

        ft.Init(message, textColor, bgColor, scale);
    }

    // --- Convenience helpers for readability ---

    public void ShowNormalHit(int dmg)
    {
        ShowHitSplat($"-{dmg}", Color.white, Color.red, 0.6f);
    }

    public void ShowCritHit(int dmg)
    {
        ShowHitSplat($"-{dmg}!", new Color(1f, 0.8f, 0.2f), Color.red, 0.8f);
    }

    public void ShowBlock()
    {
        ShowHitSplat("0", Color.white, new Color(0.3f, 0.6f, 1f), 0.6f);
    }
}
