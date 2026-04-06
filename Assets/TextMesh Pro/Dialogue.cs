using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public string characterName;
    [TextArea] public string text;
}

[System.Serializable]
public class Dialogue
{
    [SerializeField] public bool showDialogue;
    [SerializeField] public DialogueLine[] lines;

}
