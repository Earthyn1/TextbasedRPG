using System.Collections.Generic;
using UnityEngine;

public class ConsumableCooldowns : MonoBehaviour
{
    public static ConsumableCooldowns Instance { get; private set; }

    private Dictionary<string, float> lastUseTime = new Dictionary<string, float>();

    private void Awake()
    {
        Instance = this;
    }

    public bool IsOnCooldown(Item_Data item)
    {
        if (item?.consumable == null) return false;
        float cd = item.consumable.cooldownSeconds;
        if (cd <= 0f) return false;

        if (!lastUseTime.TryGetValue(item.itemID, out float tLast))
            return false;

        return (Time.time - tLast) < cd;
    }

    public float GetRemaining(Item_Data item)
    {
        if (item?.consumable == null) return 0f;
        float cd = item.consumable.cooldownSeconds;
        if (cd <= 0f) return 0f;

        if (!lastUseTime.TryGetValue(item.itemID, out float tLast))
            return 0f;

        float remain = cd - (Time.time - tLast);
        return remain < 0f ? 0f : remain;
    }

    public void MarkUsed(Item_Data item)
    {
        if (item == null) return;
        lastUseTime[item.itemID] = Time.time;
    }
}
