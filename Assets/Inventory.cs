using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Manages the player's inventory — dynamic size, one of each item type.
// Attach to the Player GameObject.
// Left/Right arrow keys cycle the selected slot.
public class Inventory : MonoBehaviour
{
    private List<ItemData> items = new List<ItemData>();

    public int SelectedIndex { get; private set; } = 0;

    public int Count => items.Count;

    // InventoryUI subscribes to this to know when to redraw
    public event System.Action OnChanged;

    // Fired when an item is being consumed — InventoryUI animates it, then calls RemoveItem
    public event System.Action<ItemData> OnItemConsumed;

    // Returns the index of an item (-1 if not found)
    public int IndexOf(ItemData item) => items.IndexOf(item);

    // Triggers the consume animation via InventoryUI; actual removal happens after animation
    public void ConsumeItem(ItemData item)
    {
        if (!items.Contains(item)) return;
        OnItemConsumed?.Invoke(item);
    }

    // Returns the item at a given slot
    public ItemData GetItem(int index) => (index >= 0 && index < items.Count) ? items[index] : null;

    // Returns the currently selected item (null if inventory is empty)
    public ItemData GetSelected() => items.Count > 0 ? items[SelectedIndex] : null;

    // Returns true if item is already in inventory
    public bool HasItem(ItemData item) => items.Contains(item);

    // Selects the given item if it exists in the inventory
    public void SelectItem(ItemData item)
    {
        int idx = items.IndexOf(item);
        if (idx < 0) return;
        SelectedIndex = idx;
        OnChanged?.Invoke();
    }

    // Adds an item only if it doesn't already exist — returns false if duplicate
    public bool AddItem(ItemData item)
    {
        if (items.Contains(item)) return false;
        items.Add(item);
        string itemList = string.Join(", ", items.ConvertAll(i => i.itemName));
        Debug.Log($"Picked up: {item.itemName} | Inventory: [{itemList}]");
        OnChanged?.Invoke();
        return true;
    }

    // Removes an item by reference
    public bool RemoveItem(ItemData item)
    {
        if (!items.Remove(item)) return false;
        if (SelectedIndex >= items.Count) SelectedIndex = Mathf.Max(0, items.Count - 1);
        OnChanged?.Invoke();
        return true;
    }

    void Start()
    {
        // Restore saved inventory when a new scene's player spawns
        if (GameManager.Instance != null && GameManager.Instance.savedInventory.Count > 0)
        {
            items = new List<ItemData>(GameManager.Instance.savedInventory);
            OnChanged?.Invoke();
        }
    }

    // Call this before loading a new scene so data isn't lost when player is destroyed
    public void SaveToGameManager()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.savedInventory = new List<ItemData>(items);
    }

    void Update()
    {
        if (items.Count == 0) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsState(GameManager.GameState.Explore)) return;

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            SelectedIndex = (SelectedIndex - 1 + items.Count) % items.Count;
            OnChanged?.Invoke();
        }

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            SelectedIndex = (SelectedIndex + 1) % items.Count;
            OnChanged?.Invoke();
        }
    }
}
