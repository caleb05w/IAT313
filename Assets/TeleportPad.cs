using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Teleports the player to a destination or loads a new scene when they walk in.
// Requires a Collider2D (Box or Circle) on this GameObject — no Inspector wiring needed.
// For cross-scene: set Target Scene + Target Spawn Point Name.
// For same-scene: set Destination only.
[RequireComponent(typeof(Collider2D))]
public class TeleportPad : MonoBehaviour
{
    public enum SpawnDirection { UseApproachDirection, Up, Down, Left, Right }

    [Header("Same-Scene Destination")]
    [Tooltip("The TeleportPad to send the player to. The destination pad controls the spawn position and direction.")]
    [SerializeField] private TeleportPad destination;

    [Header("Cross-Scene")]
    [Tooltip("Name of the scene to load. Leave empty to teleport within the same scene.")]
    [SerializeField] private string targetScene = "";

    [Tooltip("Name of the SpawnPoint in the target scene. Leave empty to use the default spawn.")]
    [SerializeField] private string targetSpawnPointName = "";

    [Header("Flag Requirements")]
    [Tooltip("This flag must be set on GameManager before the player can use this pad. Leave empty to skip.")]
    [SerializeField] private string requiredFlag = "";
    [Tooltip("Sets this flag on GameManager when the player successfully teleports. Leave empty to skip.")]
    [SerializeField] private string setFlagOnTeleport = "";
    [Tooltip("Dialogue shown when the required flag is not set.")]
    [SerializeField] private Dialogue missingFlagDialogue;

    [Header("Item Requirement")]
    [Tooltip("Item the player must have to use this pad. Leave empty for no requirement.")]
    [SerializeField] private ItemData requiredItem;
    [Tooltip("Dialogue shown when the player is missing the required item.")]
    [SerializeField] private Dialogue blockedDialogue;

    [Header("Spawn")]
    [Tooltip("Direction the player faces when they arrive. UseApproachDirection mirrors the direction they were travelling.")]
    [SerializeField] private SpawnDirection spawnDirection = SpawnDirection.UseApproachDirection;
    [Tooltip("How far from the destination the player lands.")]
    [SerializeField] private float spawnOffset = 0.5f;

    [Header("Feel")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float holdDuration = 0.2f;
    [Tooltip("Seconds before this pad can trigger again. Prevents instant re-trigger on same-scene teleports.")]
    [SerializeField] private float cooldown = 1f;
    private AudioSource audioSource;

    private bool onCooldown;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        audioSource = GetComponent<AudioSource>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (onCooldown || !other.CompareTag("Player")) return;

        if (!string.IsNullOrEmpty(requiredFlag) && (GameManager.Instance == null || !GameManager.Instance.HasFlag(requiredFlag)))
        {
            var dialogue = missingFlagDialogue != null && missingFlagDialogue.lines?.Length > 0
                ? missingFlagDialogue
                : new Dialogue { showDialogue = true, lines = new[] { new DialogueLine { characterName = "???", text = "You can't go there yet." } } };
            DialogueManager.Instance?.StartDialogue(dialogue);
            return;
        }

        if (requiredItem != null)
        {
            var inventory = other.GetComponent<Inventory>();
            if (inventory == null || !inventory.HasItem(requiredItem))
            {
                var dialogue = blockedDialogue != null && blockedDialogue.lines?.Length > 0
                    ? blockedDialogue
                    : new Dialogue
                    {
                        showDialogue = true,
                        lines = new[] { new DialogueLine { characterName = "???", text = $"You need the {requiredItem.itemName} to pass." } }
                    };

                DialogueManager.Instance?.StartDialogue(dialogue);
                return;
            }
        }

        var rb = other.GetComponent<Rigidbody2D>();
        Vector2 approachDir = rb != null && rb.linearVelocity.sqrMagnitude > 0.01f
            ? rb.linearVelocity.normalized
            : other.GetComponent<playerMovement>()?.FacingDirection ?? Vector2.down;

        if (audioSource) audioSource.Play();
        StartCoroutine(TeleportWithFade(other.gameObject, approachDir));
    }

    private IEnumerator TeleportWithFade(GameObject player, Vector2 approachDir)
    {
        onCooldown = true;

        bool hasDestination = !string.IsNullOrEmpty(targetScene) || destination != null;

        if (!hasDestination)
        {
            // Debug.LogWarning($"TeleportPad '{name}': no Destination or Target Scene set.", this);
            onCooldown = false;
            yield break;
        }

        GameManager.Instance?.SetState(GameManager.GameState.Transition);

        yield return ScreenFade.Get().FadeOutAndIn(fadeDuration, holdDuration, () =>
        {
            if (!string.IsNullOrEmpty(targetScene))
            {
                player.GetComponent<Inventory>()?.SaveToGameManager();
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.targetSpawnPointName = targetSpawnPointName;
                    GameManager.Instance.savedApproachDirection = approachDir;
                    GameManager.Instance.pendingTeleportSpawn = true;
                }
                SceneManager.LoadScene(targetScene);
            }
            else
            {
                var rb = player.GetComponent<Rigidbody2D>();
                Vector2 spawnPos = destination.GetSpawnPosition();
                if (rb != null)
                    rb.position = spawnPos;
                else
                    player.transform.position = spawnPos;
            }
        });

        if (!string.IsNullOrEmpty(setFlagOnTeleport))
            GameManager.Instance?.SetFlag(setFlagOnTeleport);

        GameManager.Instance?.SetState(GameManager.GameState.Explore);
        yield return new WaitForSeconds(cooldown);
        onCooldown = false;
    }

    // Returns where the player will land when arriving at this pad
    public Vector2 GetSpawnPosition()
    {
        Vector2 dir = spawnDirection == SpawnDirection.UseApproachDirection
            ? Vector2.down
            : ResolveDirection(Vector2.zero);
        return (Vector2)transform.position + dir * spawnOffset;
    }

    private Vector2 ResolveDirection(Vector2 approachDir)
    {
        switch (spawnDirection)
        {
            case SpawnDirection.Up:    return Vector2.up;
            case SpawnDirection.Down:  return Vector2.down;
            case SpawnDirection.Left:  return Vector2.left;
            case SpawnDirection.Right: return Vector2.right;
            default:                   return approachDir;
        }
    }

    void OnDrawGizmos()
    {
        if (spawnDirection == SpawnDirection.UseApproachDirection) return;

        Vector2 dir = ResolveDirection(Vector2.zero);
        Vector3 landingPos = (Vector2)transform.position + dir * spawnOffset;

        // Line from this pad to landing point
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, landingPos);

        // Landing position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(landingPos, 0.2f);

        // This pad center
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.15f);
    }
}
