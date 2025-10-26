using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BookProgress
{
    public string BookID;
    private Dictionary<string, WordProgress> words = new Dictionary<string, WordProgress>();

    public BookProgress(BookData book)
    {
        BookID = book.BookID;
        foreach (var word in book.Words)
        {
            words[word.WordID] = new WordProgress(word.WordID, word.UnlockXP);
        }
    }

    public bool AddXP(string wordID, float xp)
    {
        if (words.TryGetValue(wordID, out var progress))
        {
            bool wasUnlocked = progress.IsUnlocked;
            progress.AddXP(xp);
            return !wasUnlocked && progress.IsUnlocked; // true if just unlocked
        }
        return false;
    }

    public bool IsWordUnlocked(string wordID)
    {
        return words.ContainsKey(wordID) && words[wordID].IsUnlocked;
    }

    public bool IsBookComplete()
    {
        foreach (var wp in words.Values)
        {
            if (!wp.IsUnlocked) return false;
        }
        return true;
    }

    public Dictionary<string, WordProgress> GetAllWordProgress()
    {
        return new Dictionary<string, WordProgress>(words);
    }
}