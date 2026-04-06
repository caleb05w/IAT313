using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;

// Attach to any GameObject that should persist across scene loads.
// Assign cameraTarget to have the Cinemachine vcam follow that transform in every scene.
public class PersistOnLoad : MonoBehaviour
{
    [Tooltip("The transform the Cinemachine vcam should follow. Assign the Player here. Leave empty to skip camera wiring.")]
    [SerializeField] private Transform cameraTarget;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // OnSceneLoaded doesn't fire for the initial scene, so wire the vcam here
        WireCamera();
        DisableDuplicateAudioListeners();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        WireCamera();
        DisableDuplicateAudioListeners();
    }

    private void DisableDuplicateAudioListeners()
    {
        bool foundFirst = false;
        foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!foundFirst) { foundFirst = true; }
            else             { listener.enabled = false; }
        }
    }

    private void WireCamera()
    {
        if (cameraTarget == null) return;

        var vcam = FindFirstObjectByType<CinemachineVirtualCamera>();
        if (vcam == null) return;

        vcam.Follow = cameraTarget;
    }
}
