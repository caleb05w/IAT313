using UnityEngine;
using UnityEngine.InputSystem;

// Starts dialogue when the player is in range and presses the interact button.
// Attach to an NPC or interactable object alongside InteractionDetector.
// Wire OnInteract() to the "Interact" input action in the Player Input component.
// Call OnPlayerEnter/OnPlayerExit from InteractionDetector when the player enters/exits range.
public class DialogueTrigger : MonoBehaviour
{
    // Lines of dialogue to display — set in the Inspector per NPC
    [SerializeField] private string[] lines;

    // True while the player is inside the interaction zone
    private bool playerInRange = false;

    // Subscribe to state changes when this object becomes active
    void OnEnable()  => GameManager.Instance.OnStateChanged += OnStateChanged;

    // Unsubscribe when disabled — null check guards against scene unload order
    void OnDisable() { if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= OnStateChanged; }

    void OnStateChanged(GameManager.GameState state)
    {
        // Reset range flag if something else (cutscene, combat) exits dialogue externally
        if (state == GameManager.GameState.Explore)
            playerInRange = false;
    }

    // Called by the Input System when the Interact action fires (e.g. E key)
    // Wire this in the Player Input component under "Interact" → "OnInteract"
    public void OnInteract(InputAction.CallbackContext context)
    {
        // Only respond to the press, not the hold or release
        if (!context.performed) return;
        // Only trigger if player is in range
        if (!playerInRange) return;
        // Only allow interaction during Explore state — not during pause or combat
        if (!GameManager.Instance.IsState(GameManager.GameState.Explore)) return;

        GameManager.Instance.SetState(GameManager.GameState.Dialogue);

        // TODO: pass `lines` to your DialogueController here to display text
        Debug.Log("Dialogue started with: " + gameObject.name);
    }

    // Call this from InteractionDetector.OnTriggerEnter2D (or via UnityEvents)
    public void OnPlayerEnter() => playerInRange = true;

    // Call this from InteractionDetector.OnTriggerExit2D (or via UnityEvents)
    public void OnPlayerExit()  => playerInRange = false;
}
