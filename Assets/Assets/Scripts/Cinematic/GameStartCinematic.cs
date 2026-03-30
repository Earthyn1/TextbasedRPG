using UnityEngine;

/// <summary>
/// Plays an Ink-driven cinematic after the scene fades in.
///
/// Setup:
///   1. Create an .ink file (e.g. opening.ink) with your opening paragraphs — no choices, just text.
///   2. Compile it to JSON in Inky, import into Unity.
///   3. Drag the compiled TextAsset into the "Ink Json" slot on this component.
///   4. Optionally set "Start Knot" to jump to a specific section (leave blank for the start).
///
/// Example opening.ink:
///   Three weeks on the road. You counted every sunrise.
///
///   Dunhaven. The adventuring capital of the known world — if you believed the pamphlets.
///
///   The truth was simpler. You needed coin.
/// </summary>
public class GameStartCinematic : MonoBehaviour
{
    [Header("Opening Cinematic")]
    [SerializeField] private TextAsset inkJson;
    [SerializeField] private string startKnot = ""; // leave blank to start from the top

    private void OnEnable()
    {
        SceneFader.OnFadeInComplete += OnSceneFadedIn;
    }

    private void OnDisable()
    {
        SceneFader.OnFadeInComplete -= OnSceneFadedIn;
    }

    private void OnSceneFadedIn()
    {
        SceneFader.OnFadeInComplete -= OnSceneFadedIn;

        if (inkJson == null) return;

        if (CinematicManager.Instance != null)
            CinematicManager.Instance.Play(inkJson, startKnot);
        else
            Debug.LogWarning("[GameStartCinematic] CinematicManager not found in scene.");
    }
}
