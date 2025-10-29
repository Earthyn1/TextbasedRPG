using UnityEngine;
using UnityEngine.UI;

public class LootBox_UI : MonoBehaviour
{
    public Image Loot;
    public string itemID;
    void Start()
    {
        
    }

    public void Setup(string ItemID)
    {

        Loot.gameObject.SetActive(true);
        itemID = ItemID;

        Item_Data data = Inventory_Manager.Instance.GetItemDefinition(itemID);

        if (data == null)
        {
            Debug.LogWarning($"[LootBox_UI] Item '{itemID}' not found in Inventory Manager database!");
            if (Loot) Loot.sprite = null;
            return;
        }

        if (Loot != null)
        {
            Loot.sprite = data.texture;
            Loot.color = Color.white; // just ensure visible
        }
    }

    public void ClearImage()
    {
        Loot.gameObject.SetActive(false);

    }


}
