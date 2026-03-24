using UnityEngine;

// Defines a single item type — create via Assets > Create > Inventory > Item
[CreateAssetMenu(fileName = "Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
}
