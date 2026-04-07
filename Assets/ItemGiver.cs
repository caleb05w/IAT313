using UnityEngine;

// Wire to InteractionDetector's On Player Enter or ConditionalInteractable's onSuccess.
// Gives the player an item and optionally hides a visual GameObject.
public class ItemGiver : MonoBehaviour
{
    [SerializeField] private ItemData item;
    [SerializeField] private GameObject visualToHide;
    [SerializeField] private string flagToSet;
    [SerializeField] private bool oneShot = true;
    [SerializeField] private AudioSource audioSource;

    private bool hasGiven = false;

    void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    public void Give()
    {
        if (oneShot && hasGiven) return;
        if (item == null) return;

        var player = GameObject.FindWithTag("Player");
        if (player == null) return;

        var inventory = player.GetComponent<Inventory>();
        if (inventory == null) return;

        inventory.AddItem(item);
        inventory.SelectItem(item);
        FindFirstObjectByType<InventoryUI>()?.Open();
        audioSource?.Play();
        hasGiven = true;

        if (visualToHide != null)
            visualToHide.SetActive(false);

        if (!string.IsNullOrEmpty(flagToSet))
        {
            Debug.Log($"[ItemGiver] Setting flag: '{flagToSet}', GameManager: {(GameManager.Instance != null ? "exists" : "NULL")}");
            GameManager.Instance?.SetFlag(flagToSet);
        }
        else
        {
            Debug.Log("[ItemGiver] No flag to set — flagToSet is empty.");
        }
    }
}
