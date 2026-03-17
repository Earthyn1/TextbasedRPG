using System.Collections.Generic;
using UnityEngine;

public class InkStateStore : MonoBehaviour
{
    private readonly Dictionary<string, string> _stateByKey = new();

    public bool TryGet(string key, out string json) => _stateByKey.TryGetValue(key, out json);

    public void Set(string key, string json) => _stateByKey[key] = json;

    public void Clear(string key) => _stateByKey.Remove(key);
}


[System.Serializable]
public class NpcInkMemory
{
    public bool askedRumors;
}