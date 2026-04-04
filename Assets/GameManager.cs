using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Central game state manager — controls what mode the game is currently in.
// Attach to an empty GameObject named "GameManager" in your first scene.
// DontDestroyOnLoad keeps it alive across scene loads.
// Other scripts subscribe to OnStateChanged to react when the state changes.
public class GameManager : MonoBehaviour
{
    // Global access point — only one GameManager exists at a time
    public static GameManager Instance { get; private set; }

    public enum GameState { Explore, Dialogue, Combat, Pause, Transition }

    // Visible in the Inspector during Play Mode so you can watch state change live
    [SerializeField] private GameState currentState = GameState.Explore;
    public GameState CurrentState => currentState;

    // Remembers what state we were in before pausing so Unpause() can restore it
    private GameState stateBeforePause;

    // Persistent player data — survives scene loads
    [HideInInspector] public List<ItemData> savedInventory = new List<ItemData>();

    // Which SpawnPoint to use in the next scene — set by TeleportPad before loading
    [HideInInspector] public string targetSpawnPointName = "";

    // Approach direction when crossing scenes — SpawnPoint offsets the player by this
    [HideInInspector] public Vector2 savedApproachDirection = Vector2.down;

    // Set by TeleportPad before a cross-scene load; tells SpawnPoint to apply offset + restore state
    [HideInInspector] public bool pendingTeleportSpawn = false;

    // Subscribe to this event to react to state changes from any other script:
    // GameManager.Instance.OnStateChanged += MyHandler;
    public event System.Action<GameState> OnStateChanged;

    void Awake()
    {
        // Singleton pattern — destroy any duplicate that loads in a new scene
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (currentState == GameState.Transition)
            SetState(GameState.Explore);
    }

    // Call this from any script to change the game state
    // e.g. GameManager.Instance.SetState(GameState.Dialogue);
    public void SetState(GameState newState)
    {
        // Skip if already in this state
        if (currentState == newState) return;

        currentState = newState;

        // Pause freezes physics and animations via timeScale; all other states run normally
        Time.timeScale = (newState == GameState.Pause) ? 0f : 1f;

        // Notify all subscribers (playerMovement, StateLabel, etc.)
        OnStateChanged?.Invoke(currentState);

    }

    // Pauses the game and remembers the previous state so Unpause() can restore it
    public void Pause()
    {
        if (currentState == GameState.Pause) return;
        stateBeforePause = currentState;
        SetState(GameState.Pause);
    }

    // Restores the state that was active before Pause() was called
    public void Unpause()
    {
        SetState(stateBeforePause);
    }

    // Convenience check — lets other scripts avoid importing the enum directly
    // e.g. if (GameManager.Instance.IsState(GameState.Explore))
    public bool IsState(GameState state) => currentState == state;

    void Update()
    {
        // Toggle pause when Escape is pressed
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (currentState == GameState.Pause)
                Unpause();
            else
                Pause();
        }
    }

    // Shows current state as an overlay in the Game view during Play Mode
    // Remove this before shipping
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 200, 20), "State: " + currentState);
    }
}
