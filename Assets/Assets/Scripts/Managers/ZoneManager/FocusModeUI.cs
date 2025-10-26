// FocusModeUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FocusModeUI : MonoBehaviour
{
    [SerializeField] Transform actionsParent;

    readonly List<GameObject> _hiddenSiblings = new();
    Transform _focusedParent;
    bool _active;

    public bool IsActive => _active;

    public void Enter(Button focusedButton)
    {
        if (_active || focusedButton == null || actionsParent == null) return;

        Transform focusedRoot = FindDirectChildContaining(actionsParent, focusedButton.transform);
        if (focusedRoot == null) return;

        _active = true;
        _hiddenSiblings.Clear();
        foreach (Transform child in actionsParent)
        {
            if (child == focusedRoot) continue;
            if (child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(false);
                _hiddenSiblings.Add(child.gameObject);
            }
        }
        if (actionsParent is RectTransform r) LayoutRebuilder.ForceRebuildLayoutImmediate(r);
        GameManager.Instance?.SetNavigationLocked(true);
    }

    public void Exit()
    {
        if (!_active) return;
        foreach (var go in _hiddenSiblings) if (go) go.SetActive(true);
        _hiddenSiblings.Clear();
        if (actionsParent is RectTransform r) LayoutRebuilder.ForceRebuildLayoutImmediate(r);
        GameManager.Instance?.SetNavigationLocked(false);
        _active = false;
    }

    static Transform FindDirectChildContaining(Transform root, Transform descendant)
    {
        Transform t = descendant; while (t != null && t.parent != root) t = t.parent;
        return (t != null && t.parent == root) ? t : null;
    }
}
