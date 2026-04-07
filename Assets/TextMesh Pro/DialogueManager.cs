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

    private DialogueLine[] lines;
    private int currentIndex = 0;
    private bool isOpen = false;
    private int frameOpened = -1;
    public int FrameOpened => frameOpened;

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
        if (isOpen && Keyboard.current.eKey.wasPressedThisFrame && Time.frameCount > frameOpened)
            DisplayNextLine();
    }

// takes all the NPC lines and writes down the character's name and their lines
    public void StartDialogue(Dialogue dialogue)
    {
        Debug.Log($"[DialogueManager] StartDialogue called. dialogue={dialogue != null}, lines={dialogue?.lines?.Length ?? -1}, showDialogue={dialogue?.showDialogue}, dialogueBox={dialogueBox != null}");
        if (dialogue == null || dialogue.lines == null || dialogue.lines.Length == 0)
        {
            Debug.LogWarning("[DialogueManager] Dialogue has no lines — did you forget to fill the Lines array?");
            return;
        }

        lines = dialogue.lines;
        currentIndex = 0;
        isOpen = true;
        frameOpened = Time.frameCount;

        dialogueBox.SetActive(dialogue.showDialogue);
        ShowLine(lines[currentIndex]);
        Debug.Log($"[DialogueManager] Showing line 0: name={lines[0].characterName}, text={lines[0].text}");
        GameManager.Instance?.SetState(GameManager.GameState.Dialogue);
    }

    private void ShowLine(DialogueLine line)
    {
        nameText.text     = line.characterName;
        dialogueText.text = line.text;
    }

// each time you press E, displays the next line if theres any
    public void DisplayNextLine()
    {
        string lineFlag = lines[currentIndex].flagOnComplete;
        currentIndex++;

        if (currentIndex >= lines.Length)
        {
            // End dialogue first so any TriggerDialogue calls from the flag can open a new dialogue.
            EndDialogue();
            if (!string.IsNullOrEmpty(lineFlag))
                GameManager.Instance?.SetFlag(lineFlag);
            return;
        }

        if (!string.IsNullOrEmpty(lineFlag))
            GameManager.Instance?.SetFlag(lineFlag);

        ShowLine(lines[currentIndex]);
    }

// if you walk away or when dialogue is finished, changes isOpen to false and the box disappears
    public void EndDialogue()
    {
        Debug.Log($"[DialogueManager] EndDialogue called. isOpen was={isOpen}\n{System.Environment.StackTrace}");
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
