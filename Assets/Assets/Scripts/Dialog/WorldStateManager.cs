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

        flags.Add(key);   // ✅ store first

        OnFlagChanged?.Invoke(key);   // ✅ invoke after storage
    }

    public void ClearFlag(string key)
    {
        if (flags.Remove(key))
            OnFlagChanged?.Invoke(key);
    }
}