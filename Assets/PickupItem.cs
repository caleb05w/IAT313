using UnityEngine;
using UnityEngine.InputSystem;

// Attach to a pickup GameObject alongside InteractionDetector.
// Wire OnPlayerEnter() to onPlayerEnter and OnPlayerExit() to onPlayerExit in InteractionDetector.
public class PickupItem : MonoBehaviour
{
    [SerializeField] private ItemData itemData;

    private bool playerInRange = false;
    private Inventory inventory;

    // Wire to InteractionDetector's onPlayerEnter
    public void OnPlayerEnter()
    {
        playerInRange = true;
        var player = GameObject.FindWithTag("Player");
        inventory = player != null ? player.GetComponent<Inventory>() : null;
    }

    // Wire to InteractionDetector's onPlayerExit
    public void OnPlayerExit()
    {
        playerInRange = false;
        inventory = null;
    }

    void Update()
    {
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;
        if (!playerInRange || inventory == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsState(GameManager.GameState.Explore)) return;

        if (inventory.AddItem(itemData))
            Destroy(gameObject);
    }
}
