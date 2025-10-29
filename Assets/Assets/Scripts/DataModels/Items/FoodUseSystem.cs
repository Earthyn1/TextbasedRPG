using UnityEngine;

public class FoodUseSystem : MonoBehaviour
{
    public static FoodUseSystem Instance { get; private set; }

    [SerializeField] private Inventory_Manager inventoryManager;
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private ConsumableCooldowns cooldowns;

    private void Awake()
    {
        // singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (combatManager == null) combatManager = CombatManager.Instance;
        if (playerStats == null) playerStats = PlayerStats.Instance;
        if (cooldowns == null) cooldowns = ConsumableCooldowns.Instance;
        if (inventoryManager == null) inventoryManager = Inventory_Manager.Instance;
    }


    /// <summary>
    /// Try to consume the given item stack from inventory.
    /// Returns a message string for UI/log.
    /// </summary>
    public string TryConsume(Item_Data itemData)
    {
        if (itemData == null || itemData.IsEmpty())
            return "There's nothing to use.";

        if (!itemData.IsConsumable())
            return $"{itemData.itemName} can't be used like that.";

        var c = itemData.consumable;
        if (c == null)
            return $"{itemData.itemName} is missing consumable data.";

        bool inCombat = CombatManager.Instance != null && CombatManager.Instance.IsActive;

        if (inCombat && !c.usableInCombat)
        {
            return $"{itemData.itemName} can't be used during combat.";
        }

        if (cooldowns != null && cooldowns.IsOnCooldown(itemData))
        {
            float remain = Mathf.Ceil(cooldowns.GetRemaining(itemData));
            return $"{itemData.itemName} is still on cooldown ({remain}s).";
        }

        // figure out how much we’re gonna heal
        bool healsHP = (c.healHPFlat > 0 || c.healHPPercent > 0f);
        bool healsMP = (c.healMPFlat > 0 || c.healMPPercent > 0f);

        if (healsHP && playerStats.CurrentHP >= playerStats.MaxHP &&
            (!healsMP || playerStats.CurrentMana >= playerStats.MaxMana))
        {
            return "You don't need that right now.";
        }

        int hpFromFlat = c.healHPFlat;
        int hpFromPct = Mathf.RoundToInt(playerStats.MaxHP * c.healHPPercent);
        int totalHP = hpFromFlat + hpFromPct;

        int mpFromFlat = c.healMPFlat;
        int mpFromPct = Mathf.RoundToInt(playerStats.MaxMana * c.healMPPercent);
        int totalMP = mpFromFlat + mpFromPct;

        int healedHP = 0;
        if (totalHP > 0)
        {
            healedHP = playerStats.ApplyHealing(totalHP); // also fires OnVitalsChanged
        }

        int healedMP = 0;
        if (totalMP > 0)
        {
            healedMP = playerStats.ApplyMana(totalMP); // also fires OnVitalsChanged
        }

        // spend 1 item from inventory
        inventoryManager.RemoveItem(itemData.itemID, 1);

        // mark cooldown
        cooldowns?.MarkUsed(itemData);

        // build the log line that describes what happened
        string msg;
        if (healedHP > 0 && healedMP > 0)
        {
            msg = $"You use {itemData.itemName}, restoring {healedHP} HP and {healedMP} MP.";
        }
        else if (healedHP > 0)
        {
            msg = $"You use {itemData.itemName}, restoring {healedHP} HP.";
        }
        else if (healedMP > 0)
        {
            msg = $"You drink {itemData.itemName}, restoring {healedMP} MP.";
        }
        else
        {
            msg = $"You use {itemData.itemName}.";
        }

        // ✅ log to the combat log / gamelog directly (green good text)
        GameLog_Manager.Instance?.AddEntry(msg, "#32CD32");

        // and also return it to caller in case UI wants to surface it somewhere else
        return msg;
    }


}
