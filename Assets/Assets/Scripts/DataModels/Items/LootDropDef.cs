using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LootDropDef
{
    public string itemId;
    public int minQty;
    public int maxQty;
    public float dropChance;
}

[System.Serializable]
public class LootTableDef
{
    public string id;
    public List<LootDropDef> drops;

    public int xpOnKill;
    public int goldMin;
    public int goldMax;

    public bool shouldFallback;
    public string fallbackItemId;
    public int fallbackQty;
}

