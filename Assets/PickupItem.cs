using UnityEngine;
using UnityEngine.InputSystem;

// Attach to a pickup GameObject alongside InteractionDetector.
// Wire OnPlayerEnter() to onPlayerEnter and OnPlayerExit() to onPlayerExit in InteractionDetector.
public class PickupItem : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private AudioSource pickupSound;
    [SerializeField] private string flagToSet;

    private bool playerInRange = false;
    private Inventory inventory;

    void Start()
    {
        if (itemData != null)
            GetComponentInChildren<LabelController>()?.SetDefaultText(itemData.itemName);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        inventory = other.GetComponent<Inventory>();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        inventory = null;
    }

    void Update()
    {
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;
        if (!playerInRange || inventory == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsState(GameManager.GameState.Explore)) return;

        if (inventory.AddItem(itemData))
        {
            inventory.SelectItem(itemData);
            FindFirstObjectByType<InventoryUI>()?.Open();
            var source = pickupSound != null ? pickupSound : GetComponent<AudioSource>();
            if (source != null && source.clip != null)
                AudioSource.PlayClipAtPoint(source.clip, transform.position, source.volume);
            if (!string.IsNullOrEmpty(flagToSet))
                GameManager.Instance?.SetFlag(flagToSet);
            Destroy(gameObject);
        }
    }
}
