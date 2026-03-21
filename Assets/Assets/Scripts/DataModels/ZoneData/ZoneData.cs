using System;
using System.Collections.Generic;

[Serializable]
public class ZoneData
{
    public string id;
    public string type;            // "Zone" | "NPC" | "World"
    public string displayName;
    public string description;

    public List<ActionData> actions;

    // Zone membership
    public List<NPCInteractableData> npcs;
    public List<InteractableData> worldObjects;

    // NEW: visual props placed in the scene (only meaningful when type == "Zone")
    public List<ScenePropData> sceneProps;

    // Common presentation fields (mostly used by Zone/NPC/World entries)
    public string portrait;
    public string autoDialog;
    public string bgImage;

    // OPTIONAL: if you add it to Places later
    public string hitmask;

    public void EnsureDefaults()
    {
        actions ??= new List<ActionData>();
        npcs ??= new List<NPCInteractableData>();
        worldObjects ??= new List<InteractableData>();
        sceneProps ??= new List<ScenePropData>();
    }
}

[Serializable]
public class ScenePropData
{
    public string id;
    public string sprite;

    public List<ScenePropStateData> states;

    // Matches your JSON: "pos": [x,y], "size": [w,h]
    public float[] pos;   // length 2
    public float[] size;  // length 2

    public string hideWhenFlag;
    public int hitId;
    public bool spawnHidden;

    // Helpers so your gameplay code is clean/safe
    public float PosX => (pos != null && pos.Length > 0) ? pos[0] : 0f;
    public float PosY => (pos != null && pos.Length > 1) ? pos[1] : 0f;
    public float SizeX => (size != null && size.Length > 0) ? size[0] : 0f;
    public float SizeY => (size != null && size.Length > 1) ? size[1] : 0f;
}

[Serializable]
public class ScenePropStateData
{
    public string whenFlag;   // if this flag is true, use this state
    public string sprite;     // sprite to display
}