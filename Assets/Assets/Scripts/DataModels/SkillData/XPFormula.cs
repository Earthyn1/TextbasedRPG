using System;
using UnityEngine;

public static class XPFormula
{
    public static readonly int[] XPForLevels;

    static XPFormula()
    {
        XPForLevels = new int[100]; // Index 0 unused, levels 1–99
        XPForLevels[0] = 0;

        double xp = 0;
        for (int level = 1; level <= 99; level++)
        {
            xp += Math.Floor(level + 300 * Math.Pow(2, level / 7.0));
            XPForLevels[level] = (int)Math.Floor(xp / 4);
        }
    }

    // Optional helper: get XP required for a specific level
    public static int GetXPForLevel(int level)
    {
        if (level < 1) return 0;
        if (level >= XPForLevels.Length) return XPForLevels[99];
        return XPForLevels[level];
    }
}
