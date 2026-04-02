using UnityEngine;

public class HealthBarSlashSpawner : MonoBehaviour
{
    [Header("Spawn Area")]
    [SerializeField] private RectTransform spawnArea;

    [Header("Slash Prefabs")]
    [SerializeField] private GameObject[] slashPrefabs;

    [Header("Randomization")]
    [SerializeField] private Vector2 rotationRange = new Vector2(-15f, 15f);
    [SerializeField] private Vector2 scaleRange = new Vector2(0.9f, 1.1f);
    [SerializeField] private bool randomizeX = true;
    [SerializeField] private bool randomizeY = true;

    [Header("Optional")]
    [SerializeField] private Transform spawnParentOverride;

    public void SpawnSlash()
    {
        if (spawnArea == null)
        {
            Debug.LogWarning("[HealthBarSlashSpawner] No spawnArea assigned.", this);
            return;
        }

        if (slashPrefabs == null || slashPrefabs.Length == 0)
        {
            Debug.LogWarning("[HealthBarSlashSpawner] No slash prefabs assigned.", this);
            return;
        }

        GameObject prefab = slashPrefabs[Random.Range(0, slashPrefabs.Length)];
        if (prefab == null)
        {
            Debug.LogWarning("[HealthBarSlashSpawner] Selected slash prefab is null.", this);
            return;
        }

        Transform parent = spawnParentOverride != null ? spawnParentOverride : spawnArea;

        GameObject slashInstance = Instantiate(prefab, parent);

        RectTransform slashRect = slashInstance.GetComponent<RectTransform>();
        if (slashRect == null)
        {
            Debug.LogWarning("[HealthBarSlashSpawner] Slash prefab needs a RectTransform.", slashInstance);
            return;
        }

        // Make sure it behaves like a UI/world-space child correctly.
        slashRect.anchorMin = new Vector2(0.5f, 0.5f);
        slashRect.anchorMax = new Vector2(0.5f, 0.5f);
        slashRect.pivot = new Vector2(0.5f, 0.5f);

        Rect rect = spawnArea.rect;

        float x = randomizeX ? Random.Range(rect.xMin, rect.xMax) : rect.center.x;
        float y = randomizeY ? Random.Range(rect.yMin, rect.yMax) : rect.center.y;

        slashRect.anchoredPosition = new Vector2(x, y);

        float rot = Random.Range(rotationRange.x, rotationRange.y);
        slashRect.localRotation = Quaternion.Euler(0f, 0f, rot);

        float scale = Random.Range(scaleRange.x, scaleRange.y);
        slashRect.localScale = new Vector3(scale, scale, 1f);

        // Ensure it appears on top of other slash children if needed.
        slashRect.SetAsLastSibling();
    }

    public void SpawnSlashes(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnSlash();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (spawnArea == null)
            return;

        Vector3[] corners = new Vector3[4];
        spawnArea.GetWorldCorners(corners);

        Gizmos.color = Color.red;
        for (int i = 0; i < 4; i++)
        {
            Vector3 a = corners[i];
            Vector3 b = corners[(i + 1) % 4];
            Gizmos.DrawLine(a, b);
        }
    }
#endif
}