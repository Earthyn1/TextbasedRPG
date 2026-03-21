using Newtonsoft.Json;
using System;
using System.Collections.Generic;

[Serializable]
public class ActionData
{
    [JsonProperty("name")] public string name;
    [JsonProperty("target")] public string target;
    [JsonProperty("type")] public ActionType type;
    [JsonProperty("icon")] public string icon;
    [JsonProperty("hitColor")] public string hitColor;

    // Gating
    [JsonProperty("visibleWhen")] public List<string> visibleWhen = new();
    [JsonProperty("enableWhen")] public List<string> enableWhen = new();


    // NEW - better semantics for skill/fail systems
    [JsonProperty("hideOnSuccess")] public bool hideOnSuccess = false;

    [JsonProperty("lockedMessage")] public string lockedMessage = "You need a brush!";
}
