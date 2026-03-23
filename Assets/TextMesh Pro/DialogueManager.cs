using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI nameText;
    [SerializeField] public TextMeshProUGUI dialogueText;
    [SerializeField] public GameObject dialogueBox;

    private string[] lines;
    private int currentIndex = 0;
    private bool isOpen = false;

// takes all the NPC lines and writes down the character's name and their lines 
    public void StartDialogue(Dialogue dialogue)
    {
        lines = dialogue.dialogueText;
        currentIndex = 0;
        isOpen = true;

        dialogueBox.SetActive(dialogue.showDialogue);
        nameText.text = dialogue.characterName;
        dialogueText.text = lines[currentIndex];
    }

// each time you press E, displays the next line if theres any
    public void DisplayNextLine()
    {
        currentIndex++;

        if (currentIndex >= lines.Length)
        {
            EndDialogue();
            return;
        }

        dialogueText.text = lines[currentIndex];
    }

// if you walk away or when dialogue is finished, changes isOpen to false and the box disappears
    public void EndDialogue()
    {
        isOpen = false;
        dialogueBox.SetActive(false);
    }

// called everytime you press E to make sure the dialogue is still showing
    public bool IsOpen()
    {
        return isOpen;
    }
}
