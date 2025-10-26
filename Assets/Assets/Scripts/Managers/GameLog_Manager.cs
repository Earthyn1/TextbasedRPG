using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameLog_Manager : MonoBehaviour
{
    public static GameLog_Manager Instance { get; private set; }

    [Header("UI")]
    public TMP_Text logText; // Assign your TMP_Text UI element
    public int maxEntries = 100; // Max number of log entries to keep
    public ScrollRect scrollRect;         // Assign the ScrollRect containing the TMP_Text


    private List<string> entries = new List<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Add a new entry to the game log
    /// </summary>
    public void AddEntry(string entry, string colorHex = null)
    {
        if (string.IsNullOrEmpty(entry)) return;

        string final = entry;
        if (!string.IsNullOrEmpty(colorHex))
            final = $"<color={colorHex}>{entry}</color>";

        entries.Add(final);

        // Trim if too many
        if (entries.Count > maxEntries)
            entries.RemoveAt(0);

        UpdateUI();
        ScrollToBottom();
    }


    /// <summary>
    /// Refresh the UI text
    /// </summary>
    private void UpdateUI()
    {
        if (logText != null)
        {
            logText.text = string.Join("\n", entries);
        }
    }

    private void ScrollToBottom()
    {
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();  // Ensure layout is updated
            scrollRect.verticalNormalizedPosition = 0f;  // Scroll to bottom
        }
    }

    /// <summary>
    /// Clear the log
    /// </summary>
    public void ClearLog()
    {
        entries.Clear();
        UpdateUI();
    }
}
