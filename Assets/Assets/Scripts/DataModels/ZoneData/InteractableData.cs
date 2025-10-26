using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class InteractableData
{
    public string id;

}

[JsonConverter(typeof(StringEnumConverter))]
public enum NPCType { Neutral, Friendly, Aggressive }

[Serializable]
public class NPCInteractableData
{
    public string id;

    [JsonConverter(typeof(StringEnumConverter))]
    public NPCType type;

    // Unity types shouldn't be (de)serialized from your JSON
    [JsonIgnore]
    public Sprite Image;
}
