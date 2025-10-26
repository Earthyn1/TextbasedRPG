using System;
using System.Collections.Generic;

[Serializable]
public class ConditionBlock
{
    public List<string> allOf = new();
    public List<string> anyOf = new();
    public List<string> noneOf = new();
}

[Serializable]
public class GameEventDef
{
    public string id;
    public string scope = "global";   // or "zone"
    public string zoneId;
    public List<string> triggers = new();
    public int priority = 0;
    public bool once = true;
    public ConditionBlock conditions = new();
    public List<string> actions = new();
    public bool blocking = false;
}
