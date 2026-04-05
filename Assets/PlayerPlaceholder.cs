using UnityEngine;

// Attach to a dummy player object in scenes 2+ as a visual reference in the Editor.
// At runtime, destroys itself if a real persistent player already exists.
// Safe to leave in every scene — it never interferes with the real player.
public class PlayerPlaceholder : MonoBehaviour
{
    void Awake()
    {
        foreach (var p in FindObjectsByType<playerMovement>(FindObjectsSortMode.None))
        {
            if (p.gameObject != gameObject)
            {
                Destroy(gameObject);
                return;
            }
        }
    }
}
