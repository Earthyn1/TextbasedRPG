using UnityEngine;
using UnityEngine.EventSystems;

public class Inventory_Slot_RightClickAction : MonoBehaviour, IPointerClickHandler
{
    public Inventory_Slot slot; // assign in prefab / inspector

    private void Reset()
    {
        if (slot == null) slot = GetComponent<Inventory_Slot>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        if (slot == null || slot.inventoryData == null || slot.inventoryData.IsEmpty()) return;

        var item = slot.inventoryData;

        Debug.Log($"[RC] itemID={item.itemID} " +
                  $"type={item.itemType} " +
                  $"IsConsumable()={item.IsConsumable()} " +
                  $"hasBlock={(item.consumable != null)}");

        if (item.IsConsumable())
        {
            // prevent spam if it's cooling
            if (ConsumableCooldowns.Instance != null &&
                ConsumableCooldowns.Instance.IsOnCooldown(item))
            {
                // optional: give feedback in the log
                GameLog_Manager.Instance?.AddEntry(
                    $"{item.itemName} is recharging...",
                    "#AAAAAA"
                );
                return;
            }

            if (FoodUseSystem.Instance == null)
            {
                Debug.LogError("[RC] Tried to consume, but FoodUseSystem.Instance is NULL in scene.");
                return;
            }

            string resultMsg = FoodUseSystem.Instance.TryConsume(item);

            bool inCombat = CombatManager.Instance != null && CombatManager.Instance.IsActive;
            if (!inCombat)
            {
                GameLog_Manager.Instance?.AddEntry(resultMsg, "#32CD32");
            }

            // item count may have changed
            slot.inventoryManager.UpdateSlots();
            return;
        }


        if (item.IsEquippable())
        {
            Debug.Log("[RC] Equip path triggered!");
            EquipmentManager.Instance?.Equip(item, preferOffhandIfPossible: false);
            return;
        }

        Debug.Log("[RC] No valid action for this item.");
        GameLog_Manager.Instance?.AddEntry("You can't use that.");
    }

}
