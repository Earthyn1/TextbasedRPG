using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static EquippableData;

[RequireComponent(typeof(Image))]
public class Equip_Slot : MonoBehaviour, IPointerClickHandler
{
    [Header("Which slot is this?")]
    public EquipSlot slot = EquipSlot.Body;

    [Header("Icon target (defaults to this Image)")]
    public Image icon;

    [Header("Show placeholder when empty")]
    public Sprite emptySprite;
    public bool hideIconWhenEmpty = false;

    private bool subscribed;

    private void Reset()
    {
        icon = GetComponent<Image>();
    }

    private void Awake()
    {
        if (icon == null) icon = GetComponent<Image>();
    }

    private void Update()
    {
        // If manager spawned after this slot, latch on as soon as it exists
        if (!subscribed) TrySubscribe();
    }

    private void OnEnable()
    {
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquippedChanged += HandleEquippedChanged;
            // Initialize current state
            var equipped = EquipmentManager.Instance.GetEquipped(slot);
            ApplyIcon(equipped);
        }
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquippedChanged -= HandleEquippedChanged;

        if (subscribed && EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquippedChanged -= HandleEquippedChanged;
        subscribed = false;
    }

    private void TrySubscribe()
    {
        var mgr = EquipmentManager.Instance;
        if (mgr == null || subscribed) return;

        mgr.OnEquippedChanged += HandleEquippedChanged;
        subscribed = true;

        // Initialize current state
        ApplyIcon(mgr.GetEquipped(slot));
    }
    private void HandleEquippedChanged(EquipSlot changedSlot, Item_Data item)
    {
        if (changedSlot != slot) return;
        ApplyIcon(item);

        // If tooltip is open for this slot, hide it
        if (ItemTooltipManager.Instance != null)
            ItemTooltipManager.Instance.Hide();
    }

    private void ApplyIcon(Item_Data item)
    {
        if (icon == null) return;

        if (item != null && item.texture != null)
        {
            icon.enabled = true;
            icon.sprite = item.texture;
        }
        else
        {
            if (hideIconWhenEmpty)
            {
                icon.sprite = null;
                icon.enabled = false;
            }
            else
            {
                icon.enabled = true;
                icon.sprite = emptySprite; // may be null (that’s fine)
            }
        }
    }

    // Right-click to unequip this slot
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        if (EquipmentManager.Instance == null) return;

        var cur = EquipmentManager.Instance.GetEquipped(slot);
        if (cur != null)
        {
            EquipmentManager.Instance.Unequip(slot);
            // Optional: play SFX or toast here

            // Hide tooltip on manual unequip too
            if (ItemTooltipManager.Instance != null)
                ItemTooltipManager.Instance.Hide();
        }
    }

    public bool TryEquipHere(string itemID)
    {
        if (EquipmentManager.Instance == null) return false;

        if (!EquipmentManager.Instance.TryGetEquipRow(itemID, out var row))
            return false; // not equippable or unknown

        // Armor must match this exact slot. Hands can be routed by the manager.
        if (row.allowedSlot != EquippableData.EquipSlot.MainHand &&
            row.allowedSlot != EquippableData.EquipSlot.OffHand &&
            row.allowedSlot != slot)
        {
            Debug.LogWarning($"[{name}] {itemID} is not allowed in {slot}.");
            return false;
        }

        bool preferOff = (slot == EquippableData.EquipSlot.OffHand);
        return EquipmentManager.Instance.EquipByItemID(itemID, preferOff);
    }


}
