using UnityEngine;
using UnityEngine.Events;

// Handles trigger-based interactions (e.g. NPCs, pickups, doors, zones).
// Attach to any GameObject. Add any Collider2D shape you want — this script will use it.
public class InteractionDetector : MonoBehaviour
{
    // If useBoxCollider is false, a CircleCollider2D trigger zone is used with this radius
    [SerializeField] private float radius = 1f;
    // If true, uses the existing BoxCollider2D as the trigger instead of adding a CircleCollider2D
    [SerializeField] private bool useBoxCollider = false;
    // If true, touching this object triggers Combat state instead of Explore
    [SerializeField] private bool isHostile = false;

    // Wire any public method here — called when the player enters the zone
    [SerializeField] private UnityEvent onPlayerEnter;
    // Called when the player exits the zone
    [SerializeField] private UnityEvent onPlayerExit;

    private LabelController label;

    void Awake()
    {
        label = GetComponentInChildren<LabelController>();

        if (useBoxCollider)
        {
            var box = GetComponent<BoxCollider2D>();
            if (box != null) box.isTrigger = true;
            else Debug.LogWarning("InteractionDetector: useBoxCollider is true but no BoxCollider2D found on " + gameObject.name);
        }
        else
        {
            var circle = GetComponent<CircleCollider2D>();
            if (circle == null) circle = gameObject.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = radius;
        }
    }

    // Fires once when another collider enters the interaction zone
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (label != null) label.ShowMessage("Press E to interact");

        if (isHostile)
            GameManager.Instance.SetState(GameManager.GameState.Combat);

        onPlayerEnter?.Invoke();
    }

    void OnTriggerStay2D(Collider2D other) { }

    // Fires once when another collider leaves the interaction zone
    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (label != null) label.FadeOut();

        if (GameManager.Instance != null)
        {
            if (isHostile)
                GameManager.Instance.SetState(GameManager.GameState.Explore);

            onPlayerExit?.Invoke();
        }
    }
}
