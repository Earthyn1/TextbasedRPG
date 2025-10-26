using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class NPCDataLoader : MonoBehaviour
{
    [SerializeField] private TextAsset enemiesJsonFile;

    // All loaded enemies
    public List<NPCData> NPCDatabase { get; private set; } = new List<NPCData>();

    private void Awake()
    {
        LoadEnemies();
    }

    private void Start()
    {
        // Example: pass enemies to a central manager if you want
        if (NPCData_Manager.Instance != null)
        {
            NPCData_Manager.Instance.SetAllNPCS(NPCDatabase);
        }
        else
        {
            Debug.LogWarning("Enemy_Manager instance not found!");
        }
    }

    private void LoadEnemies()
    {
        if (enemiesJsonFile == null)
        {
            Debug.LogError("Enemies JSON file not assigned!");
            NPCDatabase = new List<NPCData>();
            return;
        }

        try
        {
            // Deserialize JSON into a list of enemies
            NPCDatabase = JsonConvert.DeserializeObject<List<NPCData>>(enemiesJsonFile.text);
            Debug.Log($"✅ Loaded {NPCDatabase.Count} enemies into database.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Failed to load enemies JSON: " + e.Message);
            NPCDatabase = new List<NPCData>();
        }
    }


}
