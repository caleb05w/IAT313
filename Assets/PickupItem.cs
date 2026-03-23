using UnityEngine;
using UnityEngine.InputSystem;

// Attach to a pickup GameObject alongside InteractionDetector.
// Wire OnPlayerEnter() to onPlayerEnter and OnPlayerExit() to onPlayerExit in InteractionDetector.
public class PickupItem : MonoBehaviour
{
    [SerializeField] private ItemData itemData;

    private bool playerInRange = false;

    // Wire to InteractionDetector's onPlayerEnter
    public void OnPlayerEnter() => playerInRange = true;

    // Wire to InteractionDetector's onPlayerExit
    public void OnPlayerExit() => playerInRange = false;

    void Update()
    {
        if (!playerInRange) return;
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        var inventory = player.GetComponent<Inventory>();
        if (inventory == null) return;

        if (inventory.AddItem(itemData))
            Destroy(gameObject);
    }
}
