[System.Serializable]

public class NPCData
{
    public string npcID;
    public string displayName;
    public string description;
    public string lootTable;

    public int level;
    public int maxHP;

    // Simplified combat model
    public int damage;              // replaces attack
    public float attackSpeed;
    public int block;               // replaces blockPower
    public float hitChance;         // replaces precision (0.0–1.0)
    public float evasion;           // replaces defenceRating (0.0–1.0)
    public float critChance;        // 0.05 = 5%
    public float critMultiplier;    // 1.5 = 150%
}

