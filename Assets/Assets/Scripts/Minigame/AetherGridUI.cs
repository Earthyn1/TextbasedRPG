using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Aether minigame — trace a safe path through a grid of nodes left-to-right.
///
/// Rules:
///   - All nodes are visible from the start. Corrupted = red.
///   - Player picks one node per column, must be adjacent (±1 row) to previous pick.
///   - Clicking a corrupted node = instant fail.
///   - Reaching the last column safely = win.
///   - Timer bar drains the whole time. Runs out = fail.
///
/// Inspector setup:
///   canvasGroup  — root CanvasGroup (fade in/out)
///   nodeGrid     — GridLayoutGroup that nodes are spawned into
///   nodePrefab   — prefab with AetherNode + Button + Image
///   timerBar     — Image (Fill Horizontal) for the countdown
///   promptText   — TMP_Text for "Trace the path..." / result
/// </summary>
public class AetherGridUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private CanvasGroup      canvasGroup;
    [SerializeField] private GridLayoutGroup  nodeGrid;
    [SerializeField] private GameObject       nodePrefab;
    [SerializeField] private Image            timerBar;
    [SerializeField] private TMP_Text         promptText;

    [Header("Intro")]
    [SerializeField] private float introFadeDuration = 0.30f;
    [SerializeField] private float introHoldDuration = 1.50f;

    [Header("Defaults (overridden by MinigameManager)")]
    [SerializeField] private int   cols          = 4;
    [SerializeField] private int   rows          = 3;
    [SerializeField] private float corruptChance = 0.25f;
    [SerializeField] private float timeLimit     = 12f;

    [Header("Result")]
    [SerializeField] private float resultHoldTime   = 1.0f;
    [SerializeField] private float exitFadeDuration = 0.4f;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private AetherNode[,] _nodes;
    private int   _currentCol   = 0;
    private int   _lastRow      = -1;   // -1 = first column, any row valid
    private float _timeRemaining;
    private bool  _active;

    private Action<bool> _onComplete;
    private Action<bool> _onInstantResult;

    // ── Entry point ───────────────────────────────────────────────────────────

    public void Play(int colCount, int rowCount, float corruption, float time,
                     Action<bool> onComplete, Action<bool> onInstantResult = null)
    {
        cols          = colCount;
        rows          = rowCount;
        corruptChance = corruption;
        timeLimit     = time;
        _onComplete      = onComplete;
        _onInstantResult = onInstantResult;

        _timeRemaining = timeLimit;
        _active        = false;

        if (timerBar   != null) timerBar.fillAmount = 1f;
        if (promptText != null) { promptText.text = "Trace the path..."; promptText.color = Color.white; }
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        BuildGrid();
        StartCoroutine(IntroRoutine());
    }

    // ── Grid generation ───────────────────────────────────────────────────────

    private void BuildGrid()
    {
        // Clear any old nodes
        for (int i = nodeGrid.transform.childCount - 1; i >= 0; i--)
            Destroy(nodeGrid.transform.GetChild(i).gameObject);

        // GridLayoutGroup: FixedColumnCount = cols, fills row by row
        nodeGrid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        nodeGrid.constraintCount = cols;

        _nodes = new AetherNode[cols, rows];

        // Guarantee at least one valid path through the grid
        int[] safePath = GenerateSafePath();

        // Instantiate row-by-row so GridLayoutGroup positions correctly
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                bool isCorrupted = (safePath[col] != row) &&
                                   (UnityEngine.Random.value < corruptChance);

                var go   = Instantiate(nodePrefab, nodeGrid.transform);
                var node = go.GetComponent<AetherNode>();

                if (node == null)
                {
                    Debug.LogError("[AetherGridUI] nodePrefab is missing AetherNode component!");
                    continue;
                }

                node.Setup(col, row, isCorrupted, OnNodeClicked);
                _nodes[col, row] = node;
            }
        }
    }

    /// <summary>
    /// Generates one guaranteed safe path (not necessarily the only one).
    /// Each step moves to an adjacent row (±1) in the next column.
    /// </summary>
    private int[] GenerateSafePath()
    {
        int[] path = new int[cols];
        path[0] = UnityEngine.Random.Range(0, rows);

        for (int col = 1; col < cols; col++)
        {
            int prev = path[col - 1];
            var options = new List<int>();

            for (int r = Mathf.Max(0, prev - 1); r <= Mathf.Min(rows - 1, prev + 1); r++)
                options.Add(r);

            path[col] = options[UnityEngine.Random.Range(0, options.Count)];
        }

        return path;
    }

    // ── Intro ─────────────────────────────────────────────────────────────────

    private IEnumerator IntroRoutine()
    {
        // Fade in — grid is visible so player can plan during the hold
        float t = 0f;
        while (t < introFadeDuration)
        {
            t += Time.deltaTime;
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(t / introFadeDuration);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(introHoldDuration);

        // Activate first column and start the timer
        ActivateColumn(0);
        _active = true;
    }

    // ── Column activation ─────────────────────────────────────────────────────

    private void ActivateColumn(int col)
    {
        _currentCol = col;

        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                var node = _nodes[c, r];
                if (node == null) continue;

                if (c == col)
                {
                    // Only adjacent rows to last pick are clickable
                    bool adjacent = (_lastRow == -1) || (Mathf.Abs(r - _lastRow) <= 1);
                    node.SetState(adjacent ? AetherNode.NodeState.Clickable
                                           : AetherNode.NodeState.Locked);
                }
                else if (c > col)
                {
                    // Future columns — visible but inactive
                    node.SetState(AetherNode.NodeState.Inactive);
                }
                // Past columns stay as Selected/Locked — don't touch them
            }
        }
    }

    // ── Timer ─────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_active) return;

        _timeRemaining -= Time.deltaTime;
        if (timerBar != null)
            timerBar.fillAmount = Mathf.Clamp01(_timeRemaining / timeLimit);

        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            Resolve(false);
        }
    }

    // ── Node click ────────────────────────────────────────────────────────────

    private void OnNodeClicked(AetherNode node)
    {
        if (!_active || node.Column != _currentCol) return;

        if (node.IsCorrupted)
        {
            // Touched a corrupted node — flash it and fail
            node.SetState(AetherNode.NodeState.Locked);
            Resolve(false);
            return;
        }

        // Valid pick — mark as selected
        node.SetState(AetherNode.NodeState.Selected);
        _lastRow = node.Row;

        // Lock out other nodes in this column
        for (int r = 0; r < rows; r++)
        {
            if (r == node.Row) continue;
            var sibling = _nodes[_currentCol, r];
            if (sibling != null) sibling.SetState(AetherNode.NodeState.Locked);
        }

        // Win check
        if (_currentCol == cols - 1)
        {
            Resolve(true);
            return;
        }

        // Advance
        ActivateColumn(_currentCol + 1);
    }

    // ── Resolve ───────────────────────────────────────────────────────────────

    private void Resolve(bool success)
    {
        if (!_active) return;
        _active = false;

        // Lock everything down
        for (int c = 0; c < cols; c++)
            for (int r = 0; r < rows; r++)
                if (_nodes[c, r] != null && !(_nodes[c, r].GetComponent<AetherNode>().IsCorrupted))
                    _nodes[c, r].SetState(AetherNode.NodeState.Locked);

        if (promptText != null)
        {
            promptText.text  = success ? "SUCCESS!" : "FAIL";
            promptText.color = success
                ? new Color(0.35f, 0.85f, 0.35f)
                : new Color(0.85f, 0.30f, 0.30f);
        }

        _onInstantResult?.Invoke(success);
        StartCoroutine(ResolveRoutine(success));
    }

    private IEnumerator ResolveRoutine(bool success)
    {
        yield return new WaitForSeconds(resultHoldTime);

        float t = 0f;
        while (t < exitFadeDuration)
        {
            t += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / exitFadeDuration);
            yield return null;
        }

        _onComplete?.Invoke(success);
        Destroy(gameObject);
    }
}
