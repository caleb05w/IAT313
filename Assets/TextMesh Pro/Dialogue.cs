using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public string characterName;
    [TextArea] public string text;
    [Tooltip("Flag to set when the player advances past this line.")]
    public string flagOnComplete;
}

[System.Serializable]
public class Dialogue
{
    [SerializeField] public bool showDialogue;
    [SerializeField] public DialogueLine[] lines;

}
