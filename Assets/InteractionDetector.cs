using UnityEngine;

// Handles trigger-based interactions (e.g. NPCs, pickups, doors, zones).
// Attach to any GameObject that should detect when another object enters its interaction zone.
// Requires a CircleCollider2D — one will be auto-created if missing.
public class InteractionDetector : MonoBehaviour
{
    // Radius of the interaction zone — adjust per object in the Inspector
    [SerializeField] private float interactionRadius = 1f;
    // Toggle to show a red circle in the Scene view representing the interaction zone
    [SerializeField] private bool showHitbox = false;

    private CircleCollider2D interactionCollider;
    private LabelController label;

    void Awake()
    {
        label = GetComponent<LabelController>();

        // Grab existing CircleCollider2D or create one if not present
        interactionCollider = GetComponent<CircleCollider2D>();
        if (interactionCollider == null)
            interactionCollider = gameObject.AddComponent<CircleCollider2D>();

        // Must be a trigger so objects pass through instead of physically colliding
        interactionCollider.isTrigger = true;
        interactionCollider.radius = interactionRadius;
    }

    void OnValidate()
    {
        // Updates radius live in the editor when you change the value
        var col = GetComponent<CircleCollider2D>();
        if (col != null) col.radius = interactionRadius;
    }

    void OnDrawGizmos()
    {
        // Only draw if showHitbox is enabled in the Inspector
        if (!showHitbox) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }

    // Fires once when another collider enters the interaction zone
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Interaction Enter: " + other.gameObject.name);
        //fadeIn can take in args string which will appear. Keep in mind this can also be set with seralized field in game ui.
        if (label != null) label.FadeIn();
    }

    // Fires every frame while another collider remains in the interaction zone
    void OnTriggerStay2D(Collider2D other)
    {
        Debug.Log("Interaction Stay: " + other.gameObject.name);
    }

    // Fires once when another collider leaves the interaction zone
    void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log("Interaction Exit: " + other.gameObject.name);
        if (label != null) label.FadeOut();
    }
}
