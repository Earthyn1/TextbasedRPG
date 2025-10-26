using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WordProgress
{
    public string WordID;
    public float CurrentXP;
    public bool IsUnlocked => CurrentXP >= UnlockXP;
    public float UnlockXP;

    public WordProgress(string wordID, float unlockXP)
    {
        WordID = wordID;
        UnlockXP = unlockXP;
        CurrentXP = 0f;
    }

    public void AddXP(float xp)
    {
        CurrentXP += xp;
        if (CurrentXP > UnlockXP)
            CurrentXP = UnlockXP;
    }
}