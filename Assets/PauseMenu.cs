using UnityEngine;
using UnityEngine.SceneManagement;

// Controls the pause menu panel.
// Reacts to GameManager state changes so the panel stays in sync regardless of
// whether pause was triggered by Escape (GameManager) or a UI button.
public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject pauseMenu;

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += OnStateChanged;
            pauseMenu.SetActive(GameManager.Instance.IsState(GameManager.GameState.Pause));
        }
    }

    private void OnStateChanged(GameManager.GameState state)
    {
        pauseMenu.SetActive(state == GameManager.GameState.Pause);
    }

    // Wire to an optional "Pause" button in the UI
    public void Pause()
    {
        GameManager.Instance?.Pause();
    }

    public void Resume()
    {
        GameManager.Instance?.Unpause();
    }

    public void Home()
    {
        GameManager.Instance?.SetState(GameManager.GameState.Explore);
        SceneManager.LoadScene("MENU");
    }

    public void Restart()
    {
        GameManager.Instance?.SetState(GameManager.GameState.Explore);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
