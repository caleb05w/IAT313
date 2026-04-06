using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private Dialogue dialogue;
    [SerializeField] private string flagToSet;
    [SerializeField] private string flagOnComplete;
    [SerializeField] private bool oneShot = false;

    [SerializeField] private NPCWander wander;

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

        // Only opens dialogue — advancing/dismissing is handled by DialogueManager
        if (playerInRange && Keyboard.current.eKey.wasPressedThisFrame && !DialogueManager.Instance.IsOpen())
        {
            StartDialogueWithFlag();
            wander?.SetTalking(true);
        }

        bool isOpen = DialogueManager.Instance.IsOpen();
        if (thisTriggered && wasOpen && !isOpen)
        {
            if (!string.IsNullOrEmpty(flagOnComplete))
                GameManager.Instance?.SetFlag(flagOnComplete);
            thisTriggered = false;
        }
        wasOpen = isOpen;
    }

    public void TriggerDialogue()
    {
        if (DialogueManager.Instance == null || DialogueManager.Instance.IsOpen()) return;
        StartDialogueWithFlag();
        wander?.SetTalking(true);
    }

    private void StartDialogueWithFlag()
    {
        if (oneShot && hasFired) return;
        hasFired = true;
        DialogueManager.Instance.StartDialogue(dialogue);
        thisTriggered = true;
        if (!string.IsNullOrEmpty(flagToSet))
            GameManager.Instance?.SetFlag(flagToSet);
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
            DialogueManager.Instance?.EndDialogue();
            wander?.SetTalking(false);
        }
    }
}
