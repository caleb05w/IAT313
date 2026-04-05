using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private Dialogue dialogue;

    private bool playerInRange = false;
    private NPCWander wander;

    void Start()
    {
        wander = GetComponent<NPCWander>();
    }

    void Update()
    {
        if (DialogueManager.Instance == null) return;

        // Only opens dialogue — advancing/dismissing is handled by DialogueManager
        if (playerInRange && Keyboard.current.eKey.wasPressedThisFrame && !DialogueManager.Instance.IsOpen())
        {
            DialogueManager.Instance.StartDialogue(dialogue);
            wander?.SetTalking(true);
        }
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
