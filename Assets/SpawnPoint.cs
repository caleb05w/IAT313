using UnityEngine;

// Place one or more of these in each scene to mark where the player can spawn.
// Set spawnPointName to match the TeleportPad's targetSpawnPointName in the previous scene.
// Leave spawnPointName empty to make this the default spawn for the scene.
public class SpawnPoint : MonoBehaviour
{
    [Tooltip("Must match the TeleportPad's Target Spawn Point Name in the source scene. Leave empty for the default spawn.")]
    [SerializeField] private string spawnPointName = "";

    void Start()
    {
        string target = GameManager.Instance != null ? GameManager.Instance.targetSpawnPointName : "";

        // This spawn point is active if it matches the requested target,
        // or if no target is set and this is the default (unnamed) spawn.
        bool isMatch = string.IsNullOrEmpty(target)
            ? string.IsNullOrEmpty(spawnPointName)
            : spawnPointName == target;

        if (!isMatch) return;

        // Read and clear transient teleport data
        bool fromTeleport = GameManager.Instance != null && GameManager.Instance.pendingTeleportSpawn;
        Vector2 approachDir = Vector2.down;
        if (GameManager.Instance != null)
        {
            approachDir = GameManager.Instance.savedApproachDirection;
            GameManager.Instance.targetSpawnPointName = "";
            GameManager.Instance.savedApproachDirection = Vector2.down;
            GameManager.Instance.pendingTeleportSpawn = false;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        // Only offset position when arriving via teleport — fresh starts use the exact spawn position
        Vector2 spawnPos = fromTeleport
            ? (Vector2)transform.position + approachDir * 3f
            : (Vector2)transform.position;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.position = spawnPos;
        else
            player.transform.position = spawnPos;

    }

    void OnDrawGizmos()
    {
        Gizmos.color = string.IsNullOrEmpty(spawnPointName) ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.5f);
    }
}
