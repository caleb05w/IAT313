using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Attach to the persistent player. Swaps the Animator controller per scene.
// Add an entry for each scene that needs a different look — scenes not listed keep the current controller.
public class PlayerAppearance : MonoBehaviour
{
    [System.Serializable]
    public class SceneAppearance
    {
        public string sceneName;
        public RuntimeAnimatorController animatorController;
    }

    [SerializeField] private List<SceneAppearance> appearances = new List<SceneAppearance>();

    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyForScene(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyForScene(scene.name);
    }

    private void ApplyForScene(string sceneName)
    {
        foreach (var entry in appearances)
        {
            if (entry.sceneName == sceneName && entry.animatorController != null)
            {
                animator.runtimeAnimatorController = entry.animatorController;
                return;
            }
        }
    }
}
