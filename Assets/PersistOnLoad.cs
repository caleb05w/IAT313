using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;

// Attach to any GameObject that should persist across scene loads.
public class PersistOnLoad : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reassign the Cinemachine camera target to the persistent player
        var vcam = FindObjectOfType<CinemachineVirtualCamera>();
        var player = GameObject.FindWithTag("Player");
        if (vcam != null && player != null)
            vcam.Follow = player.transform;
    }
}
