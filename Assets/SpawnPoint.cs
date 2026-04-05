using UnityEngine;

// Place one or more of these in each scene to mark where the player can spawn.
// Set spawnPointName to match the TeleportPad's targetSpawnPointName in the previous scene.
// Leave spawnPointName empty to make this the default spawn for the scene.
public class SpawnPoint : MonoBehaviour
{
    public enum SpawnDirection { UseApproachDirection, Up, Down, Left, Right }

    [Tooltip("Must match the TeleportPad's Target Spawn Point Name in the source scene. Leave empty for the default spawn.")]
    [SerializeField] private string spawnPointName = "";

    [Tooltip("Which direction the player spawns from. UseApproachDirection mirrors the direction they were travelling.")]
    [SerializeField] private SpawnDirection spawnDirection = SpawnDirection.UseApproachDirection;

    [Tooltip("How far from the spawn point the player lands.")]
    [SerializeField] private float spawnOffset = 0.5f;

    void Start()
    {
        string target = GameManager.Instance != null ? GameManager.Instance.targetSpawnPointName : "";

        bool isMatch = string.IsNullOrEmpty(target)
            ? string.IsNullOrEmpty(spawnPointName)
            : spawnPointName == target;

        if (!isMatch) return;

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

        Vector2 spawnPos = fromTeleport
            ? (Vector2)transform.position + ResolveDirection(approachDir) * spawnOffset
            : (Vector2)transform.position;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.position = spawnPos;
        else
            player.transform.position = spawnPos;
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
        Gizmos.color = string.IsNullOrEmpty(spawnPointName) ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        // Draw an arrow showing the resolved spawn direction
        Vector2 dir = ResolveDirection(Vector2.down);
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(dir * spawnOffset));
        Gizmos.DrawWireSphere(transform.position + (Vector3)(dir * spawnOffset), 0.15f);
    }
}
