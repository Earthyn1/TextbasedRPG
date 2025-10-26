using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class Inventory_Slot : MonoBehaviour
{
    public TMP_Text itemQty;
    public Item_Data inventoryData;
    public Inventory_Manager inventoryManager;
    public Image itemImage;

    public Image cooldownRadial;

    private void Update()
    {
        UpdateCooldownUI();
    }

    public void Initialize(Inventory_Manager manager)
    {
        inventoryManager = manager;
    }

    public void SetSlot(Item_Data data)
    {
        inventoryData = data;

        if (data == null || data.IsEmpty())
        {
            SetEmpty();
        }
        else
        {
            itemImage.sprite = inventoryData.texture;
            itemImage.color = new Color(1, 1, 1, 1f);
            itemQty.text = inventoryData.quantity.ToString();

            // inventoryText.text = $"{data.itemName} (x{data.quantity})";
            // inventoryText.color = Color.black;
        }
    }

    public void SetEmpty()
    {
        inventoryData = Item_Data.Empty;
      
        itemQty.text = "";
        itemImage.color = new Color(0, 0, 0, 0.0f);

        
    }

    public void SlotClicked()
    {
        string addition = "";
        if (inventoryData.itemType == ItemType.Literacy)
        {
           addition = GetBookProgress(inventoryData.itemID);
        }

        GameLog_Manager.Instance.AddEntry(inventoryData.description + " " + addition);

    }

    public string GetBookProgress(string bookid)
    {
        // Get the progress dictionary for this book
        var wordProgress = Book_Manager.Instance.GetBookWordProgress(bookid);

        // Get total words in the book
        var book = Book_Manager.Instance.GetBook(bookid);
        int totalWords = book != null ? book.Words.Count : 0;

        // Count unlocked words
        int unlockedCount = wordProgress != null ? wordProgress.Values.Count(w => w.IsUnlocked) : 0;

        // Return display text
        return $" ({unlockedCount}/{totalWords})";
    }

    private void UpdateCooldownUI()
    {
        // no slot data? nothing to show
        if (inventoryData == null || inventoryData.IsEmpty())
        {
            if (cooldownRadial != null)
            {
                cooldownRadial.fillAmount = 0f;
                cooldownRadial.enabled = false;
            }
            return;
        }

        // not a consumable? no cooldown UI
        if (!inventoryData.IsConsumable() || ConsumableCooldowns.Instance == null)
        {
            if (cooldownRadial != null)
            {
                cooldownRadial.fillAmount = 0f;
                cooldownRadial.enabled = false;
            }
            return;
        }

        var cds = ConsumableCooldowns.Instance;

        // is it actually cooling down?
        if (!cds.IsOnCooldown(inventoryData))
        {
            // cooldown over: hide overlay entirely
            if (cooldownRadial != null)
            {
                cooldownRadial.fillAmount = 0f;
                cooldownRadial.enabled = false;
            }
            return;
        }

        // still cooling → compute fill
        float remain = cds.GetRemaining(inventoryData);                        // seconds left
        float total = inventoryData.consumable.cooldownSeconds;               // total cooldown
        float fillNorm = Mathf.Clamp01(remain / Mathf.Max(total, 0.0001f));   // 1 → full dark, 0 → ready

        if (cooldownRadial != null)
        {
            cooldownRadial.enabled = true;
            cooldownRadial.fillAmount = fillNorm;
        }
    }

}
