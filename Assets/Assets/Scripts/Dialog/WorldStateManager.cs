using System;
using System.Collections.Generic;
using UnityEngine;

public class WorldStateManager : MonoBehaviour
{
    public static WorldStateManager Instance;

    public event Action<string> OnFlagChanged; // key


    private HashSet<string> flags = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // optional but recommended
    }

    public bool HasFlag(string key)
    {
        return flags.Contains(key);
    }

    public void SetFlag(string key)
    {
        flags.Add(key);              // ✅ store in WorldStateManager
        WorldState.SetFlag(key, true); // ✅ keep WorldState in sync (used by RequirementEvaluator)

        OnFlagChanged?.Invoke(key);
    }

    public void ClearFlag(string key)
    {
        if (flags.Remove(key))
        {
            WorldState.SetFlag(key, false);
            OnFlagChanged?.Invoke(key);
        }
    }
}