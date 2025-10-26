// DialogUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] TMP_Text zoneNameText;
    [SerializeField] TMP_Text zoneDescriptionText;
    [SerializeField] Image npcPortraitImage;
    [SerializeField] Image npcPortraitImage2;
    [SerializeField] Sprite defaultNarratorPortrait;
    [SerializeField] Transform actionsParent;
    [SerializeField] GameObject actionButtonPrefab;

    readonly HashSet<string> _granted = new();

    public void SetPortraitToNarrator()
    {
        if (!defaultNarratorPortrait) return;
        if (npcPortraitImage) npcPortraitImage.sprite = defaultNarratorPortrait;
        if (npcPortraitImage2) npcPortraitImage2.sprite = defaultNarratorPortrait;
    }

    public void SetPortraitFromId(string portraitId)
    {
        Sprite sprite = null;
        if (!string.IsNullOrEmpty(portraitId))
            sprite = Resources.Load<Sprite>($"Portraits/{portraitId}");
        if (sprite == null) sprite = defaultNarratorPortrait;
        if (npcPortraitImage) npcPortraitImage.sprite = sprite;
        if (npcPortraitImage2) npcPortraitImage2.sprite = sprite;
    }

    public void ApplyEnterEffects(DialogNode node)
    {
        if (node == null) return;
        if (_granted.Contains(node.dialogId)) return;

        if (node.onEnterTakeItems != null)
            foreach (var t in node.onEnterTakeItems)
                if (t != null && !string.IsNullOrWhiteSpace(t.id) && t.amount > 0)
                {
                    Inventory_Manager.Instance?.RemoveItem(t.id, t.amount);
                    GameLog_Manager.Instance?.AddEntry($"-{t.amount} {t.id}");
                }

        if (node.onEnterGiveItems != null)
            foreach (var g in node.onEnterGiveItems)
                if (g != null && !string.IsNullOrWhiteSpace(g.id) && g.amount > 0)
                {
                    Inventory_Manager.Instance?.AddItem(g.id, g.amount);
                    GameLog_Manager.Instance?.AddEntry($"+{g.amount} {g.id}");
                }

        _granted.Add(node.dialogId);
    }

    public void RenderNode(DialogNode node)
    {
        if (node == null) return;

        zoneNameText.text = node.npcName;
        zoneDescriptionText.text = $"<b>{node.npcName}:</b> {node.npcText}";
        SetPortraitFromId(node.npcPortrait);

        ApplyEnterEffects(node);

        foreach (Transform c in actionsParent) Destroy(c.gameObject);

        foreach (var option in node.playerOptions)
        {
            if (!RequirementEvaluator.EvaluateAll(option.requirement)) continue;

            var go = GameObject.Instantiate(actionButtonPrefab, actionsParent);
            var btn = go.GetComponent<Action_Button>();
            btn.SetupDialogButton(option);
            btn.actionName = node.npcName;
            btn.actionRequirement = option.requirement;
        }
    }
}
