using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

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
    [Tooltip("If true, events fire when the player presses E in range instead of on enter.")]
    [SerializeField] private bool fireOnInteract = false;
    [SerializeField] private string interactLabel;
    [SerializeField] private string flagOnEnter;
    [SerializeField] private string flagOnExit;
    [SerializeField] private bool oneShot = false;

    private bool hasFired = false;
    private bool playerInRange = false;

    // Wire any public method here — called when the player enters the zone (or presses E if fireOnInteract)
    [SerializeField] private UnityEvent onPlayerEnter;
    // Called when the player exits the zone
    [SerializeField] private UnityEvent onPlayerExit;
    // Called with the player's Collider2D — wire to ConditionalInteractable.OnPlayerEnter
    [SerializeField] private UnityEngine.Events.UnityEvent<Collider2D> onPlayerEnterWithCollider;

    private LabelController label;
    private Collider2D cachedPlayerCollider;

    void Awake()
    {
        label = GetComponentInChildren<LabelController>();

        if (useBoxCollider)
        {
            var box = GetComponent<BoxCollider2D>();
            if (box != null) box.isTrigger = true;
        }
        else
        {
            var circle = GetComponent<CircleCollider2D>();
            if (circle == null) circle = gameObject.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = radius;
        }
    }

    void Update()
    {
        if (!fireOnInteract || !playerInRange) return;
        if (oneShot && hasFired) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsState(GameManager.GameState.Explore)) return;
        if (!Keyboard.current.eKey.wasPressedThisFrame) return;

        Fire(cachedPlayerCollider);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        cachedPlayerCollider = other;

        if (label != null) label.ShowMessage(string.IsNullOrEmpty(interactLabel) ? "Press E to interact" : interactLabel);

        if (fireOnInteract) return;

        if (oneShot && hasFired) return;

        if (isHostile)
            GameManager.Instance.SetState(GameManager.GameState.Combat);

        Fire(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        cachedPlayerCollider = null;

        if (label != null) label.FadeOut();

        if (GameManager.Instance != null)
        {
            if (isHostile)
                GameManager.Instance.SetState(GameManager.GameState.Explore);

            if (!string.IsNullOrEmpty(flagOnExit)) GameManager.Instance.SetFlag(flagOnExit);
            onPlayerExit?.Invoke();
        }
    }

    private void Fire(Collider2D other)
    {
        hasFired = true;
        if (!string.IsNullOrEmpty(flagOnEnter)) GameManager.Instance?.SetFlag(flagOnEnter);
        onPlayerEnter?.Invoke();
        if (other != null) onPlayerEnterWithCollider?.Invoke(other);
    }
}
