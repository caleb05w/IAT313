using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueTrigger : MonoBehaviour
{
    public Dialogue dialogue;

    private bool playerInRange = false;
    private DialogueManager dialogueManager;

    private NPCWander wander;

    void Start()
    {
        dialogueManager = FindObjectOfType<DialogueManager>(true);
        wander = GetComponent<NPCWander>();
    }

    void Update()
    {
        if (playerInRange && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (dialogueManager.IsOpen())
                dialogueManager.DisplayNextLine();
            else
            {
                dialogueManager.StartDialogue(dialogue);
                wander?.SetTalking(true);
            }
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
            dialogueManager?.EndDialogue();
            wander?.SetTalking(false);
        }
    }
}
