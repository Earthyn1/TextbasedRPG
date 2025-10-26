using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using static UnityEngine.Rendering.DebugUI;
using UnityEngine.UI;

public class Book_Manager : MonoBehaviour
{
    public static Book_Manager Instance;

    // Dictionary for fast lookup by BookID
    private Dictionary<string, BookData> allBooks = new Dictionary<string, BookData>();

    // Player's progress per book: BookID -> BookProgress
    private Dictionary<string, BookProgress> playerBookProgress = new Dictionary<string, BookProgress>();

    public RectTransform BooksList;
    public RectTransform BookPanel;
    public RectTransform RightPanel;
    public RectTransform literacyPanel;
    public RectTransform BookCompletedBox;
    public RectTransform ProgressBarsHolder;

    public TMP_Text RightPanelName;
    public TMP_Text MiddlePanelName;
    public Book_Button_Slot slotPrefab;
    public Word_Slot wordSlotPrefab;
    public RectTransform wordSlotPanel;
    public Action_Button returnButton;
    public Action_Button returnButton2;

    public TMP_Text BookProgress;


    public XPNumbers XPNumbersPrefab;
    public RectTransform xpSpawnLocation;
    public RectTransform xpSpawnLocation2;


    public ReadingSession ReadingSession;



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region Book Initialization
    public void SetAllBooks(List<BookData> books)
    {
        allBooks.Clear();
        foreach (var book in books)
        {
            if (!allBooks.ContainsKey(book.BookID))
                allBooks[book.BookID] = book;
            else
                Debug.LogWarning($"Duplicate BookID found: {book.BookID}");
        }

        Debug.Log($"Book_Manager initialized with {allBooks.Count} books.");
    }
    #endregion

    public void PopulateBookList(string Returnzone)
    {
        GameManager.Instance.clearMiddlePanels();
        GameManager.Instance.clearRightPanels();

        BookPanel.gameObject.SetActive(true);
        returnButton.nextAction = Returnzone;
        returnButton2.nextAction = Returnzone;

        RightPanelName.text = "Book Info";

        foreach (Transform child in BooksList)
        {
            child.gameObject.SetActive(false);
        }

        foreach (Inventory_Slot slot in Inventory_Manager.Instance.slots)
        {
            if (slot.inventoryData.itemType == ItemType.Literacy)
            {
                Book_Button_Slot newSlot = Instantiate(slotPrefab, BooksList);
                newSlot.name = $"Slot_{BooksList.name}";
                newSlot.literacy_Panel = literacyPanel;
                newSlot.RightPanel = RightPanel;
               

                // Get book ID from inventoryData.itemID
                string bookID = slot.inventoryData.itemID;
                newSlot.bookID = bookID;

               // Get the progress dictionary for this book
               var wordProgress = Book_Manager.Instance.GetBookWordProgress(bookID);

                // Get total words in the book
                var book = Book_Manager.Instance.GetBook(bookID);
                int totalWords = book != null ? book.Words.Count : 0;

                SkillData literacyskill = PlayerSkills.Instance.GetSkill(Enum_Skills.Aethur);

                if ( book.RequiredLvl <= literacyskill.level)
                {
                     // Count unlocked words
                    int unlockedCount = wordProgress != null ? wordProgress.Values.Count(w => w.IsUnlocked) : 0;
                    // Set the display text
                    newSlot.Text.text = $"{slot.inventoryData.itemName} ({unlockedCount}/{totalWords})";
                }
                else
                {
                    newSlot.Text.text = $"{slot.inventoryData.itemName} lvl:{book.RequiredLvl} required";
                    newSlot.Text.color = Color.red ;
                }




                    Debug.Log(newSlot.Text.text);
            }
        }
    }

    public void SetupLiteracyPanel(string bookID)
    {
        foreach (Transform child in wordSlotPanel)
        {
            Destroy(child.gameObject);
        }

        var book = GetBook(bookID);
        if (book == null) return;

        MiddlePanelName.text = book.BookName;

        var wordProgressDict = GetBookWordProgress(bookID);

        WordData focusedWord = null; // The word we'll focus on

        BookCompletedBox.gameObject.SetActive(false);
        ProgressBarsHolder.gameObject.SetActive(true);

        // First, check if any word is already in progress
        foreach (var word in book.Words)
        {
            bool isUnlocked = IsWordUnlocked(bookID, word.WordID);
            wordProgressDict.TryGetValue(word.WordID, out var wp);
            float cur = wp?.CurrentXP ?? 0f;
            float req = wp?.UnlockXP ?? word.UnlockXP;
            bool inProgress = !isUnlocked && cur > 0f && cur < req;

            if (inProgress)
            {
                focusedWord = word;
                break; // pick the first in-progress word
            }
        }

        // If none in progress, pick a random word that is not yet unlocked
        if (focusedWord == null)
        {
            var lockedWords = book.Words.Where(w => !IsWordUnlocked(bookID, w.WordID)).ToList();
            if (lockedWords.Count > 0)
                focusedWord = lockedWords[Random.Range(0, lockedWords.Count)];
        }

        // Build UI slots
        foreach (var word in book.Words)
        {
            bool isUnlocked = IsWordUnlocked(bookID, word.WordID);
            wordProgressDict.TryGetValue(word.WordID, out var wp);
            float cur = wp?.CurrentXP ?? 0f;
            float req = wp?.UnlockXP ?? word.UnlockXP;
            bool inProgress = !isUnlocked && cur > 0f && cur < req;

            Word_Slot newSlot = Instantiate(wordSlotPrefab, wordSlotPanel);
            newSlot.wordID = word.WordID;

            if (isUnlocked)
            {
                newSlot.Text.text = word.WordText;
                newSlot.isUnlocked = true;
                newSlot.GetComponent<Image>().color = Color.green;
                Debug.Log($"{word.WordText} (unlocked)");
            }
            else if (inProgress)
            {
                newSlot.Text.text = "?!!!?"; // partially revealed
                Debug.Log($"{word.WordText}: {cur}/{req} XP (in progress)");
            }
            else
            {
                newSlot.Text.text = "????"; // not started
                Debug.Log("????");
            }
        }

        // Start the reading session with the focused word
        if (focusedWord != null)
            ReadingSession.StartSession(book.BookID, focusedWord.WordID);



        // Get total words in the book
        int totalWords = book != null ? book.Words.Count : 0;

        // Get the progress dictionary for this book
        var wordProgress = Book_Manager.Instance.GetBookWordProgress(bookID);
        // Count unlocked words
        int unlockedCount = wordProgress != null ? wordProgress.Values.Count(w => w.IsUnlocked) : 0;

        // Set the display text
        BookProgress.text = $"Book Progress: {unlockedCount}/{totalWords}";
    }


    public void RevealUnlockedWord(string bookID, string wordID)
    {
        var book = GetBook(bookID);
        if (book == null) return;

        // Find the actual WordData for this WordID
        var wordData = book.Words.FirstOrDefault(w => w.WordID == wordID);
        if (wordData == null) return;

        PlayerSkills.Instance.AddXP(Enum_Skills.Aethur, wordData.UnlockXP);
 
        GameLog_Manager.Instance.AddEntry("New word learnt! " + "'" + wordData.WordText + "'");

        // Get total words in the book
        int totalWords = book != null ? book.Words.Count : 0;

        // Get the progress dictionary for this book
        var wordProgress = Book_Manager.Instance.GetBookWordProgress(bookID);
        // Count unlocked words
        int unlockedCount = wordProgress != null ? wordProgress.Values.Count(w => w.IsUnlocked) : 0;

        // Set the display text
        BookProgress.text = $"Book Progress: {unlockedCount}/{totalWords}";




        // Iterate through the UI slots
        foreach (Transform child in wordSlotPanel)
        {
            Word_Slot slot = child.GetComponent<Word_Slot>();
            if (slot == null) continue;

            // Match slot by WordID
            if (slot.wordID == wordID)
            {
                slot.Text.text = wordData.WordText; // reveal actual word
                slot.isUnlocked = true;
                slot.GetComponent<Image>().color = Color.green;
                Debug.Log($"Revealed word: {wordData.WordText}");
                break;
            }
        }
    }


    #region Getters
    public BookData GetBook(string bookID)
    {
        if (allBooks.TryGetValue(bookID, out var book))
            return book;

        Debug.LogWarning($"BookID '{bookID}' not found.");
        return null;
    }

    public List<BookData> GetAllBooks()
    {
        return new List<BookData>(allBooks.Values);
    }
    #endregion

    #region Word Tracking
    // Start tracking a book for the player
    public void InitializeBookProgress(string bookID)
    {
        if (!playerBookProgress.ContainsKey(bookID) && allBooks.ContainsKey(bookID))
        {
            var book = allBooks[bookID];
            playerBookProgress[bookID] = new BookProgress(book);
        }
    }

    // Add XP towards a word
    public void AddWordXP(string bookID, string wordID, float xpAmount)
    {
        InitializeBookProgress(bookID);

        if (!playerBookProgress.TryGetValue(bookID, out var bookProgress))
            return;

        // Add XP to the current word
        bool wasUnlocked = bookProgress.IsWordUnlocked(wordID); // before adding
        bookProgress.AddXP(wordID, xpAmount);

        // Literacy skill XP
        PlayerSkills.Instance.AddXP(Enum_Skills.Aethur, xpAmount);

        // Check if this word just unlocked
        bool justUnlocked = !wasUnlocked && bookProgress.IsWordUnlocked(wordID);

        if (justUnlocked)
        {
            Debug.Log($"Word {wordID} completed!");
            RevealUnlockedWord(bookID, wordID);

            // 🔑 Check if the entire book is now complete
            if (bookProgress.IsBookComplete())
            {
                Debug.Log($"Book {bookID} is fully complete!");
                OnBookComplete(bookID); // trigger reward/event/etc.
            }
            else
            {
                // Pick a new word to focus on
                string newWordID = PickNextWord(bookID);
                if (!string.IsNullOrEmpty(newWordID))
                {
                    ReadingSession.StartSession(bookID, newWordID);
                }
            }
        }
    }

    // Called when a whole book is finished
    private void OnBookComplete(string bookID)
    {
        BookCompletedBox.gameObject.SetActive(true);
        ProgressBarsHolder.gameObject.SetActive(false);
        // Example: grant bonus XP, unlock a new book, or show UI popup
        GameLog_Manager.Instance.AddEntry($"You finished {allBooks[bookID].BookName}!");
        Inventory_Manager.Instance.RemoveItem(bookID, 1);
        BookData bookData = GetBook(bookID);
        PlayerSkills.Instance.AddXP(Enum_Skills.Aethur, bookData.LiteracyXP); // reward bonus XP
        PlayerSkills.Instance.AddXP(bookData.SkillReward, bookData.XPReward); // reward bonus XP
        ReadingSession.CurrentBookID = "";
        ReadingSession.CurrentWordID = "";


        // TODO: maybe show a “Book Complete!” popup here
    }


    private string PickNextWord(string bookID)
    {
        if (!allBooks.TryGetValue(bookID, out var book))
            return null;

        var bookProgressDict = GetBookWordProgress(bookID);

        // First, try to find a word in progress
        foreach (var word in book.Words)
        {
            if (!IsWordUnlocked(bookID, word.WordID) &&
                bookProgressDict.TryGetValue(word.WordID, out var wp) &&
                wp.CurrentXP > 0f && wp.CurrentXP < wp.UnlockXP)
            {
                return word.WordID;
            }
        }

        // Otherwise, pick a random locked word
        var lockedWords = new List<string>();
        foreach (var word in book.Words)
        {
            if (!IsWordUnlocked(bookID, word.WordID))
                lockedWords.Add(word.WordID);
        }

        if (lockedWords.Count > 0)
            return lockedWords[Random.Range(0, lockedWords.Count)];

        // No words left
        return null;
    }





    // Check if a word is unlocked
    public bool IsWordUnlocked(string bookID, string wordID)
    {
        return playerBookProgress.ContainsKey(bookID) &&
               playerBookProgress[bookID].IsWordUnlocked(wordID);
    }

    // Check if entire book is complete
    public bool IsBookComplete(string bookID)
    {
        return playerBookProgress.ContainsKey(bookID) &&
               playerBookProgress[bookID].IsBookComplete();
    }

    // Get all unlocked words in a book
    public Dictionary<string, WordProgress> GetBookWordProgress(string bookID)
    {
        if (playerBookProgress.TryGetValue(bookID, out var progress))
            return progress.GetAllWordProgress();

        return new Dictionary<string, WordProgress>();
    }
    #endregion
}
