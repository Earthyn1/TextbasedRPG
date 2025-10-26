using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class DialogLoader : MonoBehaviour
{
    [SerializeField] private TextAsset dialogJsonFile;

    public List<DialogNode> LoadDialogs()
    {
        if (dialogJsonFile == null)
        {
            Debug.LogError("Dialog JSON file not assigned!");
            return null;
        }

        try
        {
            var dialogs = JsonConvert.DeserializeObject<List<DialogNode>>(dialogJsonFile.text);
            Debug.Log($"Loaded {dialogs.Count} dialog nodes.");
            return dialogs;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load dialog JSON: " + e.Message);
            return null;
        }
    }
}
