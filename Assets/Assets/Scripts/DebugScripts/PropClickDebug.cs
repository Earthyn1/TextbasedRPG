using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIRaycastDebug : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("====== UI CLICK DEBUG ======");

        // What Unity says you clicked
        Debug.Log($"Top Hit: {eventData.pointerCurrentRaycast.gameObject?.name}");

        // Full raycast stack (VERY IMPORTANT)
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        Debug.Log($"Raycast stack ({results.Count} hits):");

        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];

            Debug.Log(
                $"[{i}] {r.gameObject.name} | " +
                $"depth={r.depth} | " +
                $"sortingLayer={r.sortingLayer} | " +
                $"sortingOrder={r.sortingOrder}"
            );
        }

        Debug.Log("============================");
    }
}