using UnityEngine;
using UnityEngine.Events;

// Handles trigger-based interactions (e.g. NPCs, pickups, doors, zones).
// Attach to any GameObject. Add any Collider2D shape you want — this script will use it.
public class InteractionDetector : MonoBehaviour
{
    // If a CircleCollider2D is present, this controls its radius
    [SerializeField] private float radius = 1f;
    // If true, touching this object triggers Combat state instead of Explore
    [SerializeField] private bool isHostile = false;

    // Wire any public method here — called when the player enters the zone
    [SerializeField] private UnityEvent onPlayerEnter;
    // Called when the player exits the zone
    [SerializeField] private UnityEvent onPlayerExit;

    private LabelController label;

    void Awake()
    {
        label = GetComponent<LabelController>();

        // Use whatever Collider2D is on this GameObject — set isTrigger automatically
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            if (col is CircleCollider2D circle)
                circle.radius = radius;
        }
        else
            Debug.LogWarning("InteractionDetector on " + gameObject.name + " has no Collider2D.");
    }

    // Fires once when another collider enters the interaction zone
    void OnTriggerEnter2D(Collider2D other)
    {
        if (label != null) label.ShowMessage("Press E to interact");

        if (!other.CompareTag("Player")) return;

        if (isHostile)
            GameManager.Instance.SetState(GameManager.GameState.Combat);

        onPlayerEnter?.Invoke();
    }

    void OnTriggerStay2D(Collider2D other) { }

    // Fires once when another collider leaves the interaction zone
    void OnTriggerExit2D(Collider2D other)
    {
        if (label != null) label.FadeOut();

        if (other.CompareTag("Player") && GameManager.Instance != null)
        {
            if (isHostile)
                GameManager.Instance.SetState(GameManager.GameState.Explore);

            onPlayerExit?.Invoke();
        }
    }
}
