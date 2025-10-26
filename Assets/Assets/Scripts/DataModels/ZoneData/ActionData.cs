using Newtonsoft.Json;
using System;
using System.Collections.Generic;

[Serializable]
public class ActionData
{
    [JsonProperty("name")] public string name;
    [JsonProperty("zone")] public string zone;
    [JsonProperty("type")] public ActionType type;

    // Gating
    [JsonProperty("visibleWhen")] public List<string> visibleWhen = new();
    [JsonProperty("enableWhen")] public List<string> enableWhen = new();

    // OLD
    // [JsonProperty("hideWhenDone")] public bool hideWhenDone = false;

    // NEW - better semantics for skill/fail systems
    [JsonProperty("hideOnSuccess")] public bool hideOnSuccess = false;

    [JsonProperty("lockedMessage")] public string lockedMessage = "You need a brush!";
}
