using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueTrigger : MonoBehaviour
{
    public Dialogue dialogue;

    private bool playerInRange = false;
    private DialogueManager dialogueManager;

    void Start()
    {
        dialogueManager = FindObjectOfType<DialogueManager>(true);
    }

 // constantly checks if player is still in frame or if E was pressed
    void Update()
    {
        if (playerInRange && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (dialogueManager.IsOpen())
                dialogueManager.DisplayNextLine();
            else
                dialogueManager.StartDialogue(dialogue);
        }
    }
// happens once someone walks into the NPC's range, checks if they're a player
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

// happens when someone walks out of the NPC's range
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            dialogueManager.EndDialogue();
        }
    }
}
