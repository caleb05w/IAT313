using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Place one SceneDirector per scene to own that scene's startup sequence and event hooks.
// Wire scene-specific objects to the UnityEvents in the Inspector — no code changes needed per scene.
public class SceneDirector : MonoBehaviour
{
    [System.Serializable]
    public class FlagResponse
    {
        [Tooltip("Flag name that triggers this response (must match exactly).")]
        public string flag;
        [Tooltip("Events to fire when this flag is set. Drag objects in and call methods or SetActive etc.")]
        public UnityEvent onFlagSet;
    }

    [Header("Intro Dialogue")]
    [Tooltip("Play a dialogue automatically when the scene starts.")]
    [SerializeField] private bool playIntroOnStart = false;
    [SerializeField] private Dialogue introDialogue;

    [Header("Scene Lifecycle")]
    [Tooltip("Fires immediately when the scene starts.")]
    [SerializeField] private UnityEvent onSceneStart;
    [Tooltip("Fires after intro dialogue and player setup are complete.")]
    [SerializeField] private UnityEvent onSceneReady;

    [Header("Flag Responses")]
    [Tooltip("Add an entry per flag. When TriggerFlag is called with a matching name, its events fire.")]
    [SerializeField] private List<FlagResponse> flagResponses = new List<FlagResponse>();

    [Header("Game State Hooks")]
    [Tooltip("Fires whenever dialogue opens from any source.")]
    [SerializeField] private UnityEvent onDialogueOpen;
    [Tooltip("Fires whenever dialogue closes.")]
    [SerializeField] private UnityEvent onDialogueClose;
    [Tooltip("Fires when the player enters combat.")]
    [SerializeField] private UnityEvent onCombatStart;
    [Tooltip("Fires when combat ends and the game returns to Explore.")]
    [SerializeField] private UnityEvent onCombatEnd;
    [Tooltip("Fires when the game is paused.")]
    [SerializeField] private UnityEvent onPause;
    [Tooltip("Fires when the game is unpaused.")]
    [SerializeField] private UnityEvent onUnpause;

    private GameManager.GameState previousState;

    void Start()
    {
        onSceneStart.Invoke();

        if (GameManager.Instance != null)
        {
            previousState = GameManager.Instance.CurrentState;
            GameManager.Instance.OnStateChanged += OnStateChanged;
            GameManager.Instance.OnFlagSet += OnFlagSet;
        }

        if (playIntroOnStart && introDialogue != null)
            DialogueManager.Instance?.StartDialogue(introDialogue);

        onSceneReady.Invoke();
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= OnStateChanged;
            GameManager.Instance.OnFlagSet -= OnFlagSet;
        }
    }

    private void OnFlagSet(string flag)
    {
        Debug.Log($"[SceneDirector] Flag set: '{flag}'");
        foreach (var entry in flagResponses)
        {
            if (entry.flag != flag) continue;
            Debug.Log($"[SceneDirector] Matched flag '{flag}' — invoking response");
            entry.onFlagSet.Invoke();
        }
    }

    private void OnStateChanged(GameManager.GameState state)
    {
        switch (state)
        {
            case GameManager.GameState.Dialogue:
                onDialogueOpen.Invoke();
                break;

            case GameManager.GameState.Combat:
                onCombatStart.Invoke();
                break;

            case GameManager.GameState.Pause:
                onPause.Invoke();
                break;

            case GameManager.GameState.Explore:
                if (previousState == GameManager.GameState.Dialogue) onDialogueClose.Invoke();
                if (previousState == GameManager.GameState.Combat)   onCombatEnd.Invoke();
                if (previousState == GameManager.GameState.Pause)    onUnpause.Invoke();
                break;
        }

        previousState = state;
    }
}
