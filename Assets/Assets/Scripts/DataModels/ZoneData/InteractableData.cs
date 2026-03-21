using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class InteractableData
{
    public string id;
    public string displayName;
    public string portrait;
    public string autoDialog;
    public string hitColor;
}

[JsonConverter(typeof(StringEnumConverter))]
public enum NPCDisposition { Neutral, Friendly, Aggressive }

[Serializable]
public class NPCInteractableData
{
    public string id;
    public string displayName;
    public string portrait;
    public string autoDialog;

    [JsonConverter(typeof(StringEnumConverter))]
    public NPCDisposition disposition;

    public string hitColor;

    // Unity types shouldn't be (de)serialized from your JSON
    [JsonIgnore]
    public Sprite Image;
}
