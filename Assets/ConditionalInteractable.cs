using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Attach to any GameObject alongside InteractionDetector.
// Press E when in range to attempt the interaction.
// Checks optional item and flag requirements, then fires the appropriate UnityEvent.
public class ConditionalInteractable : MonoBehaviour
{
    [Header("Item Requirements")]
    [Tooltip("Enable to require the player to have specific items.")]
    [SerializeField] private bool requiresItems = false;

    [Tooltip("All of these items must be in the player's inventory. Drag ItemData assets here.")]
    [SerializeField] private List<ItemData> requiredItems = new List<ItemData>();

    [Tooltip("If true, required items are removed from inventory when the interaction succeeds.")]
    [SerializeField] private bool consumeItemsOnSuccess = false;

    [Header("Flag Requirements")]
    [Tooltip("This GameManager flag must be set before interacting. Leave blank to skip.")]
    [SerializeField] private string requiredFlag;

    [Tooltip("Sets this flag on GameManager when interaction succeeds. Leave blank to skip.")]
    [SerializeField] private string setFlagOnSuccess;

    [Header("Dialogue Feedback")]
    [Tooltip("Shown when all conditions pass. Leave null for no dialogue.")]
    [SerializeField] private Dialogue successDialogue;

    [Tooltip("Shown when the item check fails. Leave null for auto-generated message.")]
    [SerializeField] private Dialogue missingItemDialogue;

    [Tooltip("Shown when the flag check fails.")]
    [SerializeField] private Dialogue missingFlagDialogue;

    [Header("Events")]
    [SerializeField] private UnityEvent onSuccess;
    [SerializeField] private UnityEvent onFail;

    private bool playerInRange = false;
    private bool hasSucceeded = false;
    private Inventory playerInventory;

    // Wire these to InteractionDetector's onPlayerEnter / onPlayerExit UnityEvents
    public void OnPlayerEnter(Collider2D col)
    {
        // Debug.Log($"[ConditionalInteractable] OnPlayerEnter called on {gameObject.name}");
        playerInventory = col.GetComponent<Inventory>();
        playerInRange = true;
    }

    public void OnPlayerExit()
    {
        playerInRange = false;
        playerInventory = null;
    }

    void Update()
    {
        if (!playerInRange) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsState(GameManager.GameState.Explore)) return;
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;

        TryInteract();
    }

    private void TryInteract()
    {
        // Debug.Log($"[ConditionalInteractable] TryInteract called. playerInRange={playerInRange}, hasSucceeded={hasSucceeded}");
        // Already succeeded — just replay the success dialogue, skip everything else
        if (hasSucceeded)
        {
            PlayDialogue(successDialogue);
            return;
        }

        // Flag check
        if (!string.IsNullOrEmpty(requiredFlag) && (GameManager.Instance == null || !GameManager.Instance.HasFlag(requiredFlag)))
        {
            PlayDialogue(missingFlagDialogue);
            onFail?.Invoke();
            return;
        }

        // Item check
        if (requiresItems && requiredItems.Count > 0)
        {
            foreach (var item in requiredItems)
            {
                if (playerInventory == null || !playerInventory.HasItem(item))
                {
                    PlayDialogue(missingItemDialogue != null ? missingItemDialogue : BuildMissingItemDialogue(item));
                    onFail?.Invoke();
                    return;
                }
            }
        }

        // All conditions met — consume items if needed
        if (requiresItems && consumeItemsOnSuccess && playerInventory != null)
        {
            foreach (var item in requiredItems)
                playerInventory.ConsumeItem(item);
        }

        if (!string.IsNullOrEmpty(setFlagOnSuccess))
            GameManager.Instance?.SetFlag(setFlagOnSuccess);

        hasSucceeded = true;
        PlayDialogue(successDialogue);
        onSuccess?.Invoke();
    }

    private void PlayDialogue(Dialogue d)
    {
        if (d == null || d.lines == null || d.lines.Length == 0) return;
        DialogueManager.Instance?.StartDialogue(d);
    }

    private Dialogue BuildMissingItemDialogue(ItemData item)
    {
        return new Dialogue
        {
            showDialogue = true,
            lines = new[] { new DialogueLine { characterName = "", text = $"You need {item.itemName} to do this." } }
        };
    }
}
