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

        // Clear the target so returning to this scene uses the default next time
        if (GameManager.Instance != null)
            GameManager.Instance.targetSpawnPointName = "";

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.position = transform.position;
        else
            player.transform.position = transform.position;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = string.IsNullOrEmpty(spawnPointName) ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.5f);
    }
}
