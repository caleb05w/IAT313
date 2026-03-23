using UnityEngine;

// Teleports the player to a target destination on contact.
// Attach to a teleport pad GameObject alongside InteractionDetector.
// Wire this object's Teleport() method to InteractionDetector's onPlayerEnter event in the Inspector.
public class TeleportPad : MonoBehaviour
{
    // Drag the destination GameObject (or an empty Transform marker) here in the Inspector
    [SerializeField] private Transform destination;

    // Called via InteractionDetector's onPlayerEnter UnityEvent
    public void Teleport()
    {
        Debug.Log("TeleportPad.Teleport() called on " + gameObject.name);
        if (destination == null)
        {
            Debug.LogWarning("TeleportPad: no destination set on " + gameObject.name);
            return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        Debug.Log("TeleportPad: player found = " + (player != null ? player.name : "NULL"));
        if (player == null) return;

        // Move via Rigidbody2D so physics state stays consistent
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.position = destination.position;
        else
            player.transform.position = destination.position;
    }
}
