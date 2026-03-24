using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Teleports the player to a destination, optionally loading a new scene.
// Attach to a teleport pad GameObject alongside InteractionDetector.
// Wire this object's Teleport() method to InteractionDetector's onPlayerEnter event in the Inspector.
public class TeleportPad : MonoBehaviour
{
    [SerializeField] private Transform destination;

    [Tooltip("Leave empty to teleport within the same scene.")]
    [SerializeField] private string targetScene = "";

    [SerializeField] private float fadeDuration = 0f;
    [SerializeField] private float holdDuration = 0f;

    public void Teleport()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        StartCoroutine(TeleportWithFade(player));
    }

    private IEnumerator TeleportWithFade(GameObject player)
    {
        yield return ScreenFade.Get().FadeOutAndIn(fadeDuration, holdDuration, () =>
        {
            if (!string.IsNullOrEmpty(targetScene))
            {
                SceneManager.LoadScene(targetScene);
            }
            else if (destination != null)
            {
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.position = destination.position;
                else
                    player.transform.position = destination.position;
            }
        });
    }
}
