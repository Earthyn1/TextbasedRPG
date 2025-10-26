using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class BookLoader : MonoBehaviour
{
    [SerializeField] private TextAsset booksJsonFile;

    // All loaded books
    public List<BookData> bookDatabase { get; private set; } = new List<BookData>();

    private void Awake()
    {
        LoadBooks();
    }

    private void Start()
    {
        // Pass all loaded books to the manager
        if (Book_Manager.Instance != null)
        {
            Book_Manager.Instance.SetAllBooks(bookDatabase);
        }
        else
        {
            Debug.LogWarning("Book_Manager instance not found!");
        }
    }

    private void LoadBooks()
    {
        if (booksJsonFile == null)
        {
            Debug.LogError("Books JSON file not assigned!");
            bookDatabase = new List<BookData>();
            return;
        }

        try
        {
            // Deserialize JSON into a list of books
            bookDatabase = JsonConvert.DeserializeObject<List<BookData>>(booksJsonFile.text);
            Debug.Log($"Loaded {bookDatabase.Count} books into database.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load books JSON: " + e.Message);
            bookDatabase = new List<BookData>();
        }
    }

    // Optional helper: get a book by its ID
    public BookData GetBookByID(string bookID)
    {
        return bookDatabase.Find(b => b.BookID == bookID);
    }
}
