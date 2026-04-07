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
        if (animator == null) return;

        foreach (var entry in appearances)
        {
            if (entry.sceneName == sceneName && entry.animatorController != null)
            {
                // Only read params if a controller is already loaded — avoids NullRef on first load
                bool  isWalking  = false;
                float inputX     = 0f;
                float inputY     = 0f;
                float lastInputX = 0f;
                float lastInputY = -1f; // default idle faces down

                if (animator.runtimeAnimatorController != null)
                {
                    isWalking  = animator.GetBool("isWalking");
                    inputX     = animator.GetFloat("InputX");
                    inputY     = animator.GetFloat("InputY");
                    lastInputX = animator.GetFloat("LastInputX");
                    lastInputY = animator.GetFloat("LastInputY");
                }

                animator.runtimeAnimatorController = entry.animatorController;

                animator.SetBool("isWalking",   isWalking);
                animator.SetFloat("InputX",     inputX);
                animator.SetFloat("InputY",     inputY);
                animator.SetFloat("LastInputX", lastInputX);
                animator.SetFloat("LastInputY", lastInputY);
                return;
            }
        }
    }
}
