using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ZoneHitmaskInteractor : MonoBehaviour
{
    [SerializeField] private InteractionUI interactionUI;

    public Image backgroundImage;
    public Image visualOverlayImage;

    public Texture2D hitmask;
    public Material hoverMaterial;

    [Header("Tuning")]
    public bool flipV = false;
    public byte edgeTolerance = 1;
    public float fadeSpeed = 10f;
    public float clearDelay = 0.08f;

    [Header("Click")]
    public float clickCooldown = 0.15f;
    private float lastClickTime = -999f;

    // ✅ Map hitmask byte -> interactable string id (zone-scoped)
    // Fill this from your zone data when zone changes.
    public Dictionary<byte, string> hitIdToInteractable = new();

    public event Action<string, byte> OnInteractableClicked; // (interactableId, hitId)

    private byte hoveredId = 0;
    private byte pendingId = 0;
    private float revealAmount = 0f;
    private float lastNonZeroTime = -999f;

    void Update()
    {
        if (interactionUI != null && interactionUI.IsBusy)
        {
            // Optional: ensure hover visuals are cleared while UI is open
            ClearHover();
            return;
        }

        byte id = 0;
        Color32 sampled = default;

        if (HitmaskPicker.TryGetIdUnderMouse_UIImage(
            backgroundImage, hitmask, out id, out sampled,
            flipV: flipV, edgeTolerance: edgeTolerance))
        {
            // Debug
            // Debug.Log($"RAW R: {sampled.r}   ID(after snap): {id}");

            if (id != 0)
            {
                pendingId = id;
                lastNonZeroTime = Time.unscaledTime;
            }
        }

        hoveredId = (Time.unscaledTime - lastNonZeroTime <= clearDelay) ? pendingId : (byte)0;

        // ✅ Hover visuals
        if (hoverMaterial != null)
        {
            hoverMaterial.SetFloat("_HoveredId", hoveredId);

            float target = (hoveredId == 0) ? 0f : 1f;
            revealAmount = Mathf.MoveTowards(revealAmount, target, Time.unscaledDeltaTime * fadeSpeed);
            hoverMaterial.SetFloat("_Reveal", revealAmount);
        }

        // ✅ Click -> interact
        if (hoveredId != 0 && Input.GetMouseButtonDown(0))
        {
            if (Time.unscaledTime - lastClickTime < clickCooldown) return;
            lastClickTime = Time.unscaledTime;

            if (hitIdToInteractable != null && hitIdToInteractable.TryGetValue(hoveredId, out var interactableId))
            {
                OnInteractableClicked?.Invoke(interactableId, hoveredId);
            }
            else
            {
                Debug.Log($"[HitmaskClick] Clicked id={hoveredId} but no mapping. mapCount={(hitIdToInteractable?.Count ?? -1)}");
            }
        }
    }


    public void ClearHover()
    {
        hoveredId = 0;
        pendingId = 0;
        revealAmount = 0f;
        lastNonZeroTime = -999f;

        if (hoverMaterial != null)
        {
            hoverMaterial.SetFloat("_HoveredId", 0f);
            hoverMaterial.SetFloat("_Reveal", 0f);
        }
    }
}