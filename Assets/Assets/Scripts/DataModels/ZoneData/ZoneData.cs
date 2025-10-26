using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[Serializable]
public class ZoneData
{
    public string id;
    public string displayName;
    public string description;
    public List<ActionData> actions;
    public string examine;
    public List<NPCInteractableData> npcInteractables;
    public List<InteractableData> worldInteractables;
    public string portrait;
    public string autoDialog;
    public string bgImage;


    // Ensure lists are non-null after deserialization
    public void EnsureDefaults()
    {
        actions ??= new List<ActionData>();
        npcInteractables ??= new List<NPCInteractableData>();
        worldInteractables ??= new List<InteractableData>();
    }
}
