using UnityEditor.EditorTools;
using UnityEngine;

public class ReadingSession : MonoBehaviour
{
    public string CurrentBookID;
    public string CurrentWordID;

    public ProgressBar ReadingProgressBar;
    public ProgressBar WordXPProgressBar;

    private float loopDuration = 2f;
    private float loopTimer = 0f;

    public void StartSession(string bookID, string wordID)
    {
        CurrentBookID = bookID;
        CurrentWordID = wordID;
        loopTimer = 0f;
        WordXPProgressBar.SetProgress(0, "0%" );
        ReadingProgressBar.SetProgress(0, "0s");
    }


    public int GetReadingXp()
    {
        SkillData litskill = PlayerSkills.Instance.GetSkill(Enum_Skills.Aethur);
        
        // Mean XP scales linearly to hit your examples exactly
        float mean = 7.105263f + 0.894737f * litskill.level;   // ~8 at L1, ~25 at L20

        // Half-range grows from ~2 at L1 to ~5 at L20, capped so it doesn't blow up
        float half = Mathf.Clamp(1.842105f + 0.157895f * litskill.level, 2f, 6f);

        float xp = Random.Range(mean - half, mean + half);
        return Mathf.RoundToInt(Mathf.Clamp(xp, 1f, 999f));
    }


    private void Update()
    {
        if (string.IsNullOrEmpty(CurrentBookID) || string.IsNullOrEmpty(CurrentWordID))
            return;

        // Advance loop timer
        loopTimer += Time.deltaTime;

        // Update loop progress bar (optional, shows current cycle)
        if (ReadingProgressBar != null)
            ReadingProgressBar.SetProgress(
                Mathf.Clamp01(loopTimer / loopDuration),
                (loopDuration - loopTimer).ToString("F1") + "s"
            );
        // When loop finishes, add actual XP
        if (loopTimer >= loopDuration)
        {
            loopTimer = 0f;

           BookData book = GameManager.Instance.book_Manager.GetBook(CurrentBookID);
          

            // Add XP to the focused word
            GameManager.Instance.book_Manager.AddWordXP(CurrentBookID, CurrentWordID, book.ReadingXP);
            Debug.Log(book.ReadingXP);

            // Update the word XP progress bar to reflect the new total
            var bookProgressDict = GameManager.Instance.book_Manager.GetBookWordProgress(CurrentBookID);
            if (bookProgressDict.TryGetValue(CurrentWordID, out var wordProgress))
            {
                float totalProgress = wordProgress.CurrentXP / wordProgress.UnlockXP;
                if (WordXPProgressBar != null)
                    WordXPProgressBar.SetProgress(totalProgress, (totalProgress * 100f).ToString("F1") + "%");
            }
        }
    }


}
