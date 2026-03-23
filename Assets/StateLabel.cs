using UnityEngine;
using TMPro;

// Displays the current GameManager state above the player.
// Attach to the empty label GameObject that is a child of the Player.
// Assign the TextMeshPro component in the Inspector.
public class StateLabel : MonoBehaviour
{
    [SerializeField] private TextMeshPro label;

    void OnDisable() { if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= OnStateChanged; }

    void Start()
    {
        // Subscribe here instead of OnEnable so GameManager is guaranteed to exist
        GameManager.Instance.OnStateChanged += OnStateChanged;
        // Show the initial state on startup
        label.text = GameManager.Instance.CurrentState.ToString();
    }

    void OnStateChanged(GameManager.GameState state)
    {
        // Update the label whenever the state changes
        label.text = state.ToString();
    }
}
