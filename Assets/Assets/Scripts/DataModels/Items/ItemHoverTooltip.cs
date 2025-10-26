using UnityEngine;
using UnityEngine.EventSystems;

public class ItemHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [SerializeField] private Inventory_Slot invSlot;   // optional
    [SerializeField] private Equip_Slot equipSlot;     // optional

    private RectTransform rt;
    private void Awake() => rt = transform as RectTransform;
    private Item_Data CurrentItem()
    {
        if (invSlot && invSlot.inventoryData != null) return invSlot.inventoryData;
        if (equipSlot) return EquipmentManager.Instance?.GetEquipped(equipSlot.slot);
        return null;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        var it = CurrentItem();
        if (it != null && it.quantity > 0)
            ItemTooltipManager.Instance?.ShowAt(rt, it);
    }

    public void OnPointerMove(PointerEventData e)
    {
        // no-op: ItemTooltipManager follows mouse in Update()
    }

    public void OnPointerExit(PointerEventData e)
    {
        ItemTooltipManager.Instance?.Hide();
    }
}
