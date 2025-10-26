using System;
using UnityEngine;

public static class EventBus
{
    public static event Action<string, object> OnTrigger;

    public static void Fire(string trigger, object payload = null)
    {
        OnTrigger?.Invoke(trigger, payload);
    }

    public static void Trigger(string trigger, object payload = null)
    {
        OnTrigger?.Invoke(trigger, payload);
    }
}
