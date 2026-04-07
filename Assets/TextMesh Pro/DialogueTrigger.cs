using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DialogueTrigger : MonoBehaviour
{
    [System.Serializable]
    public class FlagDialogue
    {
        [Tooltip("If this flag is set, use this dialogue instead of the default.")]
        public string flag;
        public Dialogue dialogue;
        [Tooltip("If true, this flag dialogue can only trigger once.")]
        public bool oneShot;
        [HideInInspector] public bool hasFired;
    }

    [SerializeField] private Dialogue dialogue;
    [Tooltip("Override dialogue based on flags. First matching flag wins.")]
    [SerializeField] private List<FlagDialogue> flagDialogues = new List<FlagDialogue>();
    [SerializeField] private string flagToSet;
    [SerializeField] private string flagOnComplete;
    [SerializeField] private bool oneShot = false;
    [SerializeField] private bool endDialogueOnExit = true;
    [SerializeField] private NPCWander wander;

    [Header("On Complete")]
    [SerializeField] private UnityEvent onComplete;

    private bool playerInRange = false;
    private bool wasOpen = false;
    private bool thisTriggered = false;
    private bool hasFired = false;

    void Start()
    {
        if (wander == null) wander = GetComponent<NPCWander>();
    }

    void Update()
    {
        if (DialogueManager.Instance == null) return;

        if (playerInRange && Keyboard.current.eKey.wasPressedThisFrame && !DialogueManager.Instance.IsOpen() && Time.frameCount > DialogueManager.Instance.FrameOpened)
        {
            StartDialogueWithFlag();
            wander?.SetTalking(true);
        }

        bool isOpen = DialogueManager.Instance.IsOpen();
        if (thisTriggered && wasOpen && !isOpen)
        {
            if (!string.IsNullOrEmpty(flagOnComplete))
                GameManager.Instance?.SetFlag(flagOnComplete);
            onComplete?.Invoke();
            thisTriggered = false;
            wander?.SetTalking(false);
        }
        wasOpen = isOpen;
    }

    public void TriggerDialogue()
    {
        Debug.Log($"[DialogueTrigger:{name}] TriggerDialogue called. IsOpen={DialogueManager.Instance?.IsOpen()}");
        if (DialogueManager.Instance == null || DialogueManager.Instance.IsOpen()) return;
        StartDialogueWithFlag();
        wander?.SetTalking(true);
    }

    private void StartDialogueWithFlag()
    {
        var (resolved, entry) = ResolveDialogue();
        bool isFlagOverride = resolved != dialogue;
        Debug.Log($"[DialogueTrigger:{name}] StartDialogueWithFlag. resolved={resolved?.GetType().Name ?? "NULL"}, isFlagOverride={isFlagOverride}, oneShot={oneShot}, hasFired={hasFired}, entry.oneShot={entry?.oneShot}, entry.hasFired={entry?.hasFired}");
        if (oneShot && hasFired && !isFlagOverride) { Debug.Log($"[DialogueTrigger:{name}] Blocked by oneShot+hasFired"); return; }
        if (entry != null && entry.oneShot && entry.hasFired) { Debug.Log($"[DialogueTrigger:{name}] Blocked by entry.oneShot+hasFired"); return; }
        if (resolved == null) { Debug.LogWarning($"[DialogueTrigger:{name}] resolved dialogue is null!"); return; }
        hasFired = true;
        if (entry != null) entry.hasFired = true;
        DialogueManager.Instance.StartDialogue(resolved);
        thisTriggered = true;
        if (!string.IsNullOrEmpty(flagToSet))
            GameManager.Instance?.SetFlag(flagToSet);
    }

    private (Dialogue dialogue, FlagDialogue entry) ResolveDialogue()
    {
        // Iterate in reverse so later entries take priority over earlier ones.
        for (int i = flagDialogues.Count - 1; i >= 0; i--)
        {
            var entry = flagDialogues[i];
            if (!string.IsNullOrEmpty(entry.flag) && GameManager.Instance != null && GameManager.Instance.HasFlag(entry.flag))
                return (entry.dialogue, entry);
        }
        return (dialogue, null);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (endDialogueOnExit && thisTriggered)
                DialogueManager.Instance?.EndDialogue();
            wander?.SetTalking(false);
        }
    }
}
