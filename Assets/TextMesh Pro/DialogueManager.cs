using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    private static DialogueManager _instance;
    public static DialogueManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
            return _instance;
        }
    }

    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject dialogueBox;

    private string[] lines;
    private int currentIndex = 0;
    private bool isOpen = false;

    void Awake()
    {
        _instance = this;
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    void Update()
    {
        if (isOpen && Keyboard.current.eKey.wasPressedThisFrame)
            DisplayNextLine();
    }

// takes all the NPC lines and writes down the character's name and their lines
    public void StartDialogue(Dialogue dialogue)
    {
        lines = dialogue.dialogueText;
        currentIndex = 0;
        isOpen = true;

        dialogueBox.SetActive(dialogue.showDialogue);
        nameText.text = dialogue.characterName;
        dialogueText.text = lines[currentIndex];
        GameManager.Instance?.SetState(GameManager.GameState.Dialogue);
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
        GameManager.Instance?.SetState(GameManager.GameState.Explore);
    }

// called everytime you press E to make sure the dialogue is still showing
    public bool IsOpen()
    {
        return isOpen;
    }
}
