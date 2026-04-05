using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Central game state manager.
// Holds persistent data and game state across scene loads.
// Auto-creates itself before any scene loads — place a prefab in Assets/Resources/GameManager.prefab,
// or it will create a bare instance automatically.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Explore, Dialogue, Combat, Pause, Transition }

    [SerializeField] private GameState currentState = GameState.Explore;
    public GameState CurrentState => currentState;

    // Fired whenever the game state changes
    public event System.Action<GameState> OnStateChanged;

    // --- Persistent Data ---
    [HideInInspector] public List<ItemData> savedInventory = new List<ItemData>();
    [HideInInspector] public string targetSpawnPointName = "";
    [HideInInspector] public Vector2 savedApproachDirection = Vector2.down;
    [HideInInspector] public bool pendingTeleportSpawn = false;

    // --- Story Flags ---
    private readonly HashSet<string> flags = new HashSet<string>();

    // Fired whenever a flag is set — SceneDirector and others can subscribe
    public event System.Action<string> OnFlagSet;

    public void SetFlag(string flag) { flags.Add(flag); OnFlagSet?.Invoke(flag); }
    public void ClearFlag(string flag) => flags.Remove(flag);
    public bool HasFlag(string flag) => flags.Contains(flag);

    // --- Pause ---
    private GameState stateBeforePause;

    public void Pause()
    {
        if (currentState == GameState.Pause) return;
        stateBeforePause = currentState;
        SetState(GameState.Pause);
    }

    public void Unpause() => SetState(stateBeforePause);

    // --- State ---
    public void SetState(GameState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        Time.timeScale = (newState == GameState.Pause) ? 0f : 1f;
        OnStateChanged?.Invoke(currentState);
    }

    public bool IsState(GameState state) => currentState == state;

    // --- Lifecycle ---
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        var prefab = Resources.Load<GameManager>("GameManager");
        if (prefab != null) Instantiate(prefab);
        else new GameObject("GameManager").AddComponent<GameManager>();
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (currentState == GameState.Pause) Unpause();
            else Pause();
        }

        if (Keyboard.current.oKey.wasPressedThisFrame)
            Debug.Log("[Flags] " + (flags.Count == 0 ? "(none)" : "[" + string.Join(", ", flags) + "]"));
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (currentState == GameState.Transition)
            SetState(GameState.Explore);
    }
}
