using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public static class EquippableData
{
    public enum ItemKind { Weapon, Armor, Consumable, Resource, Misc }
    public enum EquipSlot { Head, Body, Legs, MainHand, OffHand, Boots, None }
    public enum WeaponHanded { OneHanded, TwoHanded }

    [Serializable]
    public class EquippableRow
    {
        public string itemID;

        // equip rules
        [JsonConverter(typeof(StringEnumConverter))]
        public EquipSlot allowedSlot = EquipSlot.None;

        [JsonConverter(typeof(StringEnumConverter))]
        public WeaponHanded handedness = WeaponHanded.OneHanded;

        public bool canGoInOffhand = false;

        // requirements
        public int levelRequirement = 0;        // e.g. 15
        public string levelRequiredSkill = "";  // e.g. "fortitude", "strength", "precision"

        // primary stats from gear (flat on the row to match JSON)
        public int strength = 0;
        public int defence = 0;
        public int fortitude = 0;
        public int precision = 0;
        public int aether = 0;

        // combat-impact stats
        public int damage = 0;           // weapon/base damage contribution
        public float attackSpeed = 0f;   // additive to base attack speed (seconds per swing delta)
        public int block = 0;            // flat DR / block this item provides
        public int elementalAether = 0;  // elemental / magic hook

        public float critChance = 0f;       // additive crit chance (0.05 = +5%)
        public float critMultiplier = 0f;   // additive crit damage bonus (0.25 = +25%)

        public float dodgeChance = 0f;      // additive avoidance bonus (0.05 = +5%)
    }
}
