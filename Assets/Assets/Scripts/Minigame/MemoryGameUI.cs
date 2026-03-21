using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Memory minigame: slots stagger in one by one, player studies them,
/// then they fade out and a question asks "Where was the [item]?"
///
/// Prefab layout:
///   Root           — CanvasGroup (whole prefab fade)
///     MessageText  — single TMP_Text + CanvasGroup (text fades separately)
///     SlotsParent  — Horizontal Layout Group
///       [slots spawned here — each gets a CanvasGroup for stagger fade-in]
///
/// Each slot is spawned from slotPrefab which has a MemorySlot component.
/// Item sprites are loaded from Resources/Icons/<spriteName>
/// </summary>
public class MemoryGameUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private CanvasGroup canvasGroup;        // whole prefab
    [SerializeField] private TMP_Text    messageText;        // single label — changes each phase
    [SerializeField] private CanvasGroup messageCanvasGroup; // fades the text independently
    [SerializeField] private Transform   slotsParent;
    [SerializeField] private GameObject  slotPrefab;

    [Header("Timing")]
    [SerializeField] private float slotFadeDuration  = 0.18f; // how long each slot fades in
    [SerializeField] private float slotStaggerDelay  = 0.09f; // gap between each slot starting
    [SerializeField] private float crossfadeDuration = 0.3f;  // text swap crossfade
    [SerializeField] private float resultHoldTime    = 1.0f;
    [SerializeField] private float exitFadeDuration  = 0.4f;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private MemorySlot[]   _slots;
    private CanvasGroup[]  _slotGroups;
    private Action<bool>   _onComplete;
    private Action<bool>   _onInstantResult; // fires immediately when result is known, before animations
    private bool           _canClick;
    private int            _correctIndex;
    private string         _targetName;

    // ── Entry point ───────────────────────────────────────────────────────────

    public void Play(MemoryGameConfig config, float showDuration, int itemCount, Action<bool> onComplete, Action<bool> onInstantResult = null)
    {
        _onComplete = onComplete;
        _onInstantResult = onInstantResult;

        var items = SelectItems(config.targetItemId, itemCount);
        if (items == null) { onComplete?.Invoke(false); Destroy(gameObject); return; }

        _correctIndex = items.targetIndex;
        _targetName   = items.targetItem.itemName;

        BuildSlots(items.selected);
        StartCoroutine(GameRoutine(showDuration));
    }

    // ── Item selection ────────────────────────────────────────────────────────

    private class Selection
    {
        public List<Item_Data> selected;
        public int             targetIndex;
        public Item_Data       targetItem;
    }

    /// <summary>
    /// Picks `count` items from the full item database, always including the target.
    /// Shuffles so the target position is random each play.
    /// </summary>
    private static Selection SelectItems(string targetItemId, int count)
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("[MemoryGameUI] InventoryManager not available.");
            return null;
        }

        Item_Data target = InventoryManager.Instance.GetItemDefinition(targetItemId);
        if (target == null)
        {
            Debug.LogError($"[MemoryGameUI] Target item '{targetItemId}' not found in database.");
            return null;
        }

        // Build distractor pool — all items except the target
        var pool = new List<Item_Data>();
        foreach (var item in InventoryManager.Instance.GetAllItemDefinitions())
            if (item.itemID != targetItemId && item.texture != null)
                pool.Add(item);

        Shuffle(pool);

        // Combine target + up to (count-1) distractors, then shuffle
        var selected = new List<Item_Data> { target };
        for (int i = 0; i < count - 1 && i < pool.Count; i++)
            selected.Add(pool[i]);

        Shuffle(selected);

        return new Selection
        {
            selected    = selected,
            targetIndex = selected.IndexOf(target),
            targetItem  = target
        };
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ── Slot setup ────────────────────────────────────────────────────────────

    private void BuildSlots(List<Item_Data> items)
    {
        int count   = items.Count;
        _slots      = new MemorySlot[count];
        _slotGroups = new CanvasGroup[count];

        for (int i = 0; i < count; i++)
        {
            var slotObj = Instantiate(slotPrefab, slotsParent);

            var cg = slotObj.GetComponent<CanvasGroup>() ?? slotObj.AddComponent<CanvasGroup>();
            cg.alpha       = 0f;
            _slotGroups[i] = cg;

            var slot = slotObj.GetComponent<MemorySlot>();
            if (slot == null)
            {
                Debug.LogError("[MemoryGameUI] slotPrefab is missing a MemorySlot component!");
                continue;
            }

            int capturedIndex = i;
            slot.Setup(items[i].texture, () => OnSlotClicked(capturedIndex));
            _slots[i] = slot;
        }
    }

    // ── Game phases ───────────────────────────────────────────────────────────

    private IEnumerator GameRoutine(float showDuration)
    {
        _canClick = false;

        // --- Phase 1: Show "Focus your mind..." and stagger slots in ---
        if (messageText) messageText.text = "Focus your mind...";
        if (messageCanvasGroup) messageCanvasGroup.alpha = 1f;

        for (int i = 0; i < _slotGroups.Length; i++)
            StartCoroutine(FadeCanvasGroup(_slotGroups[i], 0f, 1f, slotFadeDuration));

        // Wait for all slots to finish appearing, then hold for showDuration
        float staggerTotal = (_slotGroups.Length - 1) * slotStaggerDelay + slotFadeDuration;
        yield return new WaitForSeconds(staggerTotal + showDuration);

        // --- Phase 2: Fade out slots and text simultaneously ---
        foreach (var cg in _slotGroups)
            StartCoroutine(FadeCanvasGroup(cg, 1f, 0f, crossfadeDuration));

        yield return StartCoroutine(FadeCanvasGroup(messageCanvasGroup, 1f, 0f, crossfadeDuration));

        // Disable slot buttons while question is shown
        foreach (var slot in _slots)
            if (slot != null) slot.HideItem();

        // --- Phase 3: Fade in the question ---
        if (messageText) messageText.text = $"Where was the <b>{_targetName}</b>?";
        yield return StartCoroutine(FadeCanvasGroup(messageCanvasGroup, 0f, 1f, crossfadeDuration));

        // Fade slots back in as blank clickable boxes
        for (int i = 0; i < _slotGroups.Length; i++)
            StartCoroutine(FadeCanvasGroup(_slotGroups[i], 0f, 1f, slotFadeDuration, i * slotStaggerDelay));

        float questionStagger = (_slotGroups.Length - 1) * slotStaggerDelay + slotFadeDuration;
        yield return new WaitForSeconds(questionStagger);

        _canClick = true;
    }

    // ── Click handling ────────────────────────────────────────────────────────

    private void OnSlotClicked(int index)
    {
        if (!_canClick) return;
        _canClick = false;

        bool success = index == _correctIndex;
        _onInstantResult?.Invoke(success); // fire immediately — XP toast etc.
        StartCoroutine(ResolveRoutine(success));
    }

    private IEnumerator ResolveRoutine(bool success)
    {
        // Reveal slot results
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null) continue;
            if (i == _correctIndex)
                _slots[i].ShowResult(MemorySlot.ResultType.Correct);
            else
                _slots[i].ShowResult(MemorySlot.ResultType.Wrong);
        }

        // Swap message text to result
        if (messageText != null)
        {
            messageText.text  = success ? "SUCCESS!" : "FAIL";
            messageText.color = success
                ? new Color(0.35f, 0.85f, 0.35f) // green
                : new Color(0.85f, 0.3f,  0.3f);  // red
        }

        if (messageCanvasGroup != null) messageCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(resultHoldTime);

        // Reset text colour before fade so it doesn't linger
        if (messageText != null) messageText.color = Color.white;

        // Fade out entire prefab
        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, exitFadeDuration));

        _onComplete?.Invoke(success);
        Destroy(gameObject);
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration, float delay = 0f)
    {
        if (cg == null) yield break;

        if (delay > 0f) yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        cg.alpha = from;
        while (elapsed < duration)
        {
            elapsed  += Time.unscaledDeltaTime;
            cg.alpha  = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        cg.alpha = to;
    }
}
