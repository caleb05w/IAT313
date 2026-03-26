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
    [Header("Same-Scene Destination")]
    [Tooltip("Move the player to this Transform within the current scene. Leave empty if loading a new scene.")]
    [SerializeField] private Transform destination;

    [Header("Cross-Scene")]
    [Tooltip("Name of the scene to load. Leave empty to teleport within the same scene.")]
    [SerializeField] private string targetScene = "";

    [Tooltip("Name of the SpawnPoint in the target scene. Leave empty to use the default spawn.")]
    [SerializeField] private string targetSpawnPointName = "";

    [Header("Feel")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float holdDuration = 0.2f;
    [Tooltip("Seconds before this pad can trigger again. Prevents instant re-trigger on same-scene teleports.")]
    [SerializeField] private float cooldown = 1f;
    [Tooltip("How far in front of the spawn point the player lands, in the direction they were travelling.")]
    [SerializeField] private float spawnOffset = 3f;

    private bool onCooldown;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (onCooldown || !other.CompareTag("Player")) return;

        // Capture approach direction now — velocity may be zeroed during the fade
        var rb = other.GetComponent<Rigidbody2D>();
        Vector2 approachDir = rb != null && rb.linearVelocity.sqrMagnitude > 0.01f
            ? rb.linearVelocity.normalized
            : other.GetComponent<playerMovement>()?.FacingDirection ?? Vector2.down;

        StartCoroutine(TeleportWithFade(other.gameObject, approachDir));
    }

    private IEnumerator TeleportWithFade(GameObject player, Vector2 approachDir)
    {
        onCooldown = true;
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
            else if (destination != null)
            {
                var rb = player.GetComponent<Rigidbody2D>();
                Vector2 spawnPos = (Vector2)destination.position + approachDir * spawnOffset;
                if (rb != null)
                    rb.position = spawnPos;
                else
                    player.transform.position = spawnPos;
            }
            else
            {
                Debug.LogWarning($"TeleportPad '{name}': no Destination or Target Scene set.", this);
            }
        });

        // Only reached for same-scene teleports (cross-scene destroys this object)
        GameManager.Instance?.SetState(GameManager.GameState.Explore);
        yield return new WaitForSeconds(cooldown);
        onCooldown = false;
    }
}
