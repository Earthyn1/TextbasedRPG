using UnityEngine;

/// <summary>
/// Listens to InventoryManager.OnItemDelta and spawns a floating item icon
/// that flies toward the bag/inventory icon whenever an item is picked up.
///
/// Setup in Inspector:
///   toastPrefab     — a UI prefab with ItemPickupToast + Image + CanvasGroup
///   toastParent     — a full-screen RectTransform overlay to spawn toasts in
///   spawnAnchor     — where toasts appear (e.g. centre of the dialog panel)
///   bagIconTarget   — the inventory bag icon RectTransform to fly toward
/// </summary>
public class ItemPickupToastSpawner : MonoBehaviour
{
    public static ItemPickupToastSpawner Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject toastPrefab;
    [SerializeField] private RectTransform toastParent;   // full-screen canvas overlay
    [SerializeField] private RectTransform spawnAnchor;   // where icon appears (dialog area)
    [SerializeField] private RectTransform bagIconTarget; // inventory bag icon

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnItemDelta += HandleItemDelta;
        else
            StartCoroutine(SubscribeWhenReady());
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnItemDelta -= HandleItemDelta;
    }

    private System.Collections.IEnumerator SubscribeWhenReady()
    {
        while (InventoryManager.Instance == null)
            yield return null;

        InventoryManager.Instance.OnItemDelta += HandleItemDelta;
    }

    private void HandleItemDelta(string itemId, int delta)
    {
        if (delta <= 0) return; // only show on gain, not removal

        var item = InventoryManager.Instance.GetItemDefinition(itemId);
        if (item == null || item.texture == null) return;

        SpawnToast(item.texture);
    }

    private void SpawnToast(Sprite sprite)
    {
        if (toastPrefab == null || toastParent == null || spawnAnchor == null || bagIconTarget == null)
        {
            Debug.LogWarning("[ItemPickupToastSpawner] Missing references — check Inspector.");
            return;
        }

        var go = Instantiate(toastPrefab, toastParent);
        var toast = go.GetComponent<ItemPickupToast>();
        if (toast == null)
        {
            Debug.LogError("[ItemPickupToastSpawner] toastPrefab is missing ItemPickupToast component.");
            Destroy(go);
            return;
        }

        Vector2 spawnPos = WorldToCanvasPos(spawnAnchor.position);
        Vector2 targetPos = WorldToCanvasPos(bagIconTarget.position);

        toast.Init(sprite, spawnPos, targetPos);
    }

    /// Converts a world/screen position into toastParent's local anchored space.
    private Vector2 WorldToCanvasPos(Vector3 worldPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            toastParent,
            RectTransformUtility.WorldToScreenPoint(null, worldPos),
            null,
            out Vector2 localPoint
        );
        return localPoint;
    }
}
