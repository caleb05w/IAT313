using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Dynamically renders inventory slots — one per item in the inventory.
// Attach to a Canvas. Assign a slot prefab (an Image GameObject) and the Inventory.
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    // A prefab with an Image component — instantiated per item
    [SerializeField] private Image slotPrefab;

    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    private List<Image> slots = new List<Image>();

    void Start()
    {
        inventory.OnChanged += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (inventory != null) inventory.OnChanged -= Refresh;
    }

    void Refresh()
    {
        // Destroy existing slot images
        foreach (var slot in slots)
            Destroy(slot.gameObject);
        slots.Clear();

        // Spawn one slot per item
        for (int i = 0; i < inventory.Count; i++)
        {
            var item = inventory.GetItem(i);
            Image slot = Instantiate(slotPrefab, transform);
            slot.sprite = item.icon;
            slot.color = i == inventory.SelectedIndex ? selectedColor : normalColor;
            slots.Add(slot);
        }
    }
}
