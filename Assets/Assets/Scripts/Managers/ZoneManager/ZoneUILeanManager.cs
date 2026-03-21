using TMPro;
using UnityEngine;

public class ZoneUILeanManager : MonoBehaviour
{
    public static ZoneUILeanManager Instance { get; private set; }

    [SerializeField] TMP_Text zoneNameText;
    [SerializeField] TMP_Text zoneDescriptionText;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        GameManager.OnZoneWillChange += OnZoneChanged;
    }

    void OnDisable()
    {
        GameManager.OnZoneWillChange -= OnZoneChanged;
    }

    private void OnZoneChanged(ZoneData zone)
    {
        if (zone == null) return;

        if (zoneNameText != null)
            zoneNameText.text = zone.displayName ?? "";

        if (zoneDescriptionText != null)
            zoneDescriptionText.text = !string.IsNullOrEmpty(zone.description)
                ? $"<i>{zone.description}</i>"
                : "";
    }
}
