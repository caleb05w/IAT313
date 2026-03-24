using UnityEngine;

// Place one of these in each scene to mark where the player spawns.
// The player will automatically move here on scene load.
public class SpawnPoint : MonoBehaviour
{
    void Start()
    {
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
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.5f);
    }
}
