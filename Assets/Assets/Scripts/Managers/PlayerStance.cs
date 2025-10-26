using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum StanceType
{
    None,
    Berserker,
    Defensive,
    Precision
}

public class PlayerStance : MonoBehaviour
{
    public static PlayerStance Instance;

    [Header("UI Buttons")]
    [SerializeField] private Button berserkerButton;
    [SerializeField] private Button defensiveButton;
    [SerializeField] private Button precisionButton;


    [Header("Colors")]
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color defaultColor = Color.white;

    [Header("Current Stance")]
    public StanceType currentStance = StanceType.None;

    public static System.Action OnStanceChanged;


    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        // Hook up button listeners
        berserkerButton.onClick.AddListener(() => SetStance(StanceType.Berserker));
        defensiveButton.onClick.AddListener(() => SetStance(StanceType.Defensive));
        precisionButton.onClick.AddListener(() => SetStance(StanceType.Precision));
    }

    public void SetStance(StanceType newStance)
    {
        currentStance = newStance;
        Debug.Log($"Player stance changed to: {currentStance}");

        ResetButtonColors();

        if (CombatManager.Instance != null)
            CombatManager.Instance.RefreshPreviewsForCurrentStance();

        OnStanceChanged?.Invoke();


        // Get stance modifiers from your combat calc
        CombatCalculator.GetStanceMods(newStance, out float dmgMul, out float accBonusPct, out float critBonusPct);

        string stanceEffects = BuildStanceEffectString(dmgMul, accBonusPct, critBonusPct);

        switch (newStance)
        {
            case StanceType.Berserker:
                berserkerButton.image.color = selectedColor;
                GameLog_Manager.Instance.AddEntry(
                    $"Player stance changed to: {currentStance} {stanceEffects}", "#FF4444"); // red
                break;

            case StanceType.Defensive:
                defensiveButton.image.color = selectedColor;
                GameLog_Manager.Instance.AddEntry(
                    $"Player stance changed to: {currentStance} {stanceEffects}", "#3399FF"); // blue
                break;

            case StanceType.Precision:
                precisionButton.image.color = selectedColor;
                GameLog_Manager.Instance.AddEntry(
                    $"Player stance changed to: {currentStance} {stanceEffects}", "#FFD633"); // yellow
                break;
        }

        OnStanceChanged?.Invoke();

    }

    // Helper to format stance bonuses into readable text
    private string BuildStanceEffectString(float dmgMul, float accBonusPct, float critBonusPct)
    {
        List<string> parts = new List<string>();

        if (Mathf.Abs(dmgMul - 1f) > 0.01f)
        {
            float pct = (dmgMul - 1f) * 100f;
            parts.Add($"{(pct >= 0 ? "+" : "")}{pct:F0}% dmg");
        }

        if (Mathf.Abs(accBonusPct) > 0.001f)
            parts.Add($"{(accBonusPct >= 0 ? "+" : "")}{accBonusPct * 100f:F0}% accuracy");

        if (Mathf.Abs(critBonusPct) > 0.001f)
            parts.Add($"{(critBonusPct >= 0 ? "+" : "")}{critBonusPct * 100f:F0}% crit");

        return parts.Count > 0 ? $"({string.Join(", ", parts)})" : "";
    }


    private void ResetButtonColors()
    {
        berserkerButton.image.color = defaultColor;
        defensiveButton.image.color = defaultColor;
        precisionButton.image.color = defaultColor;
    }
}
