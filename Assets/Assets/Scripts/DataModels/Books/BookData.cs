using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BookData
{
    public string BookID;
    public string BookName;
    public int RequiredLvl;
    public int ReadingXP;
    public Enum_Skills SkillReward;
    public int XPReward;
    public int LiteracyXP;
    public List<WordData> Words;
}
