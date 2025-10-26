using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class ZoneLoader : MonoBehaviour
{
    [SerializeField] private TextAsset zonesJsonFile;

    public List<ZoneData> LoadZones()
    {
        if (zonesJsonFile == null)
        {
            Debug.LogError("Zones JSON file not assigned!");
            return null;
        }

        try
        {
            var zones = JsonConvert.DeserializeObject<List<ZoneData>>(zonesJsonFile.text);
           
            foreach (var z in zones)
            {
                if (z?.npcInteractables == null) continue;
                foreach (var npc in z.npcInteractables)
                {
                }
            }
            return zones;

           
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load zones JSON: " + e.Message);
            return null;
        }
    }

    // Simple confirmation logger
    public static void LogZonesBrief(List<ZoneData> zones)
    {
        if (zones == null) { Debug.LogError("Zones list is null"); return; }

        Debug.Log($"Loaded {zones.Count} zones.");
        foreach (var z in zones)
        {
            if (z == null) continue;
            z.EnsureDefaults(); // keeps counts safe
            Debug.Log($"- {z.id} ({z.displayName})  A:{z.actions.Count}  NPCs:{z.npcInteractables.Count}  World:{z.worldInteractables.Count}");
        }
    }

}
